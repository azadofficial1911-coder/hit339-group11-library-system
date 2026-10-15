using System.Data;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Services
{
    public class ReservationService : IReservationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;
        private readonly ReservationSettings _settings;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            ApplicationDbContext context,
            INotificationService notifications,
            IOptions<ReservationSettings> options,
            ILogger<ReservationService> logger)
        {
            _context = context;
            _notifications = notifications;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<HoldResult> PlaceHoldAsync(
            string libraryCode,
            string borrowerCode,
            string placedBy,
            CancellationToken ct = default)
        {
            libraryCode = libraryCode.Trim().ToUpperInvariant();
            borrowerCode = borrowerCode.Trim().ToUpperInvariant();

            Item? item = await _context.Items
                .FirstOrDefaultAsync(record =>
                    record.LibraryCode.ToUpper() == libraryCode, ct);

            if (item == null)
            {
                return HoldResult.Fail("No library item was found with this code.");
            }

            Borrower? borrower = await _context.Borrowers
                .FirstOrDefaultAsync(record =>
                    record.BorrowerCode.ToUpper() == borrowerCode, ct);

            if (borrower == null)
            {
                return HoldResult.Fail("No borrower was found with this code.");
            }

            if (!borrower.IsActive)
            {
                return HoldResult.Fail("This borrower account is inactive.");
            }

            if (item.Status == ItemStatus.Available)
            {
                return HoldResult.Fail(
                    "This item is available now, so it can be borrowed instead of reserved.");
            }

            if (item.Status == ItemStatus.Destroyed)
            {
                return HoldResult.Fail("This item has been destroyed and cannot be reserved.");
            }

            bool borrowerHasItem = await _context.Loans
                .AnyAsync(loan =>
                    loan.ItemId == item.Id &&
                    loan.BorrowerId == borrower.Id &&
                    loan.ReturnDate == null, ct);

            if (borrowerHasItem)
            {
                return HoldResult.Fail("This borrower already has the item on loan.");
            }

            bool duplicate = await _context.Reservations
                .AnyAsync(hold =>
                    hold.ItemId == item.Id &&
                    hold.BorrowerId == borrower.Id &&
                    (hold.Status == ReservationStatus.Waiting ||
                     hold.Status == ReservationStatus.Ready), ct);

            if (duplicate)
            {
                return HoldResult.Fail("This borrower already has an active hold on the item.");
            }

            Reservation reservation = new()
            {
                ItemId = item.Id,
                BorrowerId = borrower.Id,
                Status = ReservationStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                PlacedBy = placedBy
            };

            _context.Reservations.Add(reservation);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // The unique index caught a duplicate placed at the same moment.
                _context.Entry(reservation).State = EntityState.Detached;

                return HoldResult.Fail("This borrower already has an active hold on the item.");
            }

            int position = await GetPositionAsync(reservation, ct);

            return HoldResult.Ok(
                $"Hold placed for {borrower.FullName} on \"{item.Name}\". " +
                $"Queue position: {position}.",
                reservation);
        }

        public async Task<HoldResult> CancelHoldAsync(
            int reservationId,
            CancellationToken ct = default)
        {
            Reservation? reservation = await _context.Reservations
                .FirstOrDefaultAsync(hold => hold.Id == reservationId, ct);

            if (reservation == null)
            {
                return HoldResult.Fail("That hold could not be found.");
            }

            if (reservation.Status != ReservationStatus.Waiting &&
                reservation.Status != ReservationStatus.Ready)
            {
                return HoldResult.Fail("That hold is already closed.");
            }

            bool wasReady = reservation.Status == ReservationStatus.Ready;

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync(ct);

            // A cancelled Ready hold frees the item for the next borrower.
            if (wasReady)
            {
                await OnItemAvailableAsync(reservation.ItemId, ct);
            }

            return HoldResult.Ok("The hold was cancelled.", reservation);
        }

        public async Task<List<HoldSummary>> GetHoldsForBorrowerAsync(
            int borrowerId,
            CancellationToken ct = default)
        {
            List<Reservation> holds = await _context.Reservations
                .AsNoTracking()
                .Include(hold => hold.Item)
                .Where(hold =>
                    hold.BorrowerId == borrowerId &&
                    (hold.Status == ReservationStatus.Waiting ||
                     hold.Status == ReservationStatus.Ready))
                .OrderBy(hold => hold.CreatedAt)
                .ToListAsync(ct);

            List<HoldSummary> summaries = new();

            foreach (Reservation hold in holds)
            {
                summaries.Add(new HoldSummary(
                    hold.Id,
                    hold.Item.Name,
                    hold.Item.LibraryCode,
                    hold.Status,
                    await GetPositionAsync(hold, ct),
                    hold.ExpiresAt));
            }

            return summaries;
        }

        public async Task<Reservation?> OnItemAvailableAsync(
            int itemId,
            CancellationToken ct = default)
        {
            // Use the caller's transaction if there is one; otherwise start our own.
            // Serializable stops two requests giving the same copy to two borrowers.
            IDbContextTransaction? ownTransaction =
                _context.Database.CurrentTransaction == null
                    ? await _context.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable, ct)
                    : null;

            Reservation? promoted = null;

            try
            {
                Item? item = await _context.Items
                    .FirstOrDefaultAsync(record => record.Id == itemId, ct);

                if (item != null && (item.Status == ItemStatus.Available || item.Status == ItemStatus.Reserved))
                {
                    bool alreadyHeld = await _context.Reservations
                        .AnyAsync(hold =>
                            hold.ItemId == itemId &&
                            hold.Status == ReservationStatus.Ready, ct);

                    if (alreadyHeld)
                    {
                        // A Ready hold exists, so the item must not look available.
                        item.Status = ItemStatus.Reserved;
                    }
                    else
                    {
                        Reservation? next = await _context.Reservations
                            .Where(hold =>
                                hold.ItemId == itemId &&
                                hold.Status == ReservationStatus.Waiting)
                            .OrderBy(hold => hold.CreatedAt)
                            .ThenBy(hold => hold.Id)
                            .FirstOrDefaultAsync(ct);

                        if (next != null)
                        {
                            DateTime now = DateTime.UtcNow;

                            next.Status = ReservationStatus.Ready;
                            next.ReadyAt = now;
                            next.ExpiresAt = now.AddDays(_settings.PickupDays);
                            item.Status = ItemStatus.Reserved;

                            promoted = next;
                        }
                        else if (item.Status == ItemStatus.Reserved)
                        {
                            // Nobody is waiting any more, so release the item.
                            item.Status = ItemStatus.Available;
                        }
                    }

                    await _context.SaveChangesAsync(ct);
                }

                if (ownTransaction != null)
                {
                    await ownTransaction.CommitAsync(ct);
                }
            }
            catch
            {
                if (ownTransaction != null)
                {
                    await ownTransaction.RollbackAsync(ct);
                }

                throw;
            }
            finally
            {
                if (ownTransaction != null)
                {
                    await ownTransaction.DisposeAsync();
                }
            }

            if (promoted != null)
            {
                // Notify after the allocation is saved.
                await _notifications.NotifyHoldReadyAsync(promoted.Id, ct);
            }

            return promoted;
        }

        public async Task OnItemStatusChangedAsync(
            int itemId,
            ItemStatus oldStatus,
            ItemStatus newStatus,
            CancellationToken ct = default)
        {
            if (newStatus == ItemStatus.Available && oldStatus != ItemStatus.Available)
            {
                // For example a Damaged item that has been repaired.
                await OnItemAvailableAsync(itemId, ct);
            }
            else if (newStatus == ItemStatus.Destroyed)
            {
                List<Reservation> activeHolds = await _context.Reservations
                    .Where(hold =>
                        hold.ItemId == itemId &&
                        (hold.Status == ReservationStatus.Waiting ||
                         hold.Status == ReservationStatus.Ready))
                    .ToListAsync(ct);

                foreach (Reservation hold in activeHolds)
                {
                    hold.Status = ReservationStatus.Cancelled;
                }

                if (activeHolds.Count > 0)
                {
                    await _context.SaveChangesAsync(ct);
                }
            }
        }

        public async Task<BorrowCheck> CheckBorrowAllowedAsync(
            int itemId,
            int borrowerId,
            CancellationToken ct = default)
        {
            Reservation? ready = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(hold =>
                    hold.ItemId == itemId &&
                    hold.Status == ReservationStatus.Ready, ct);

            if (ready == null)
            {
                return new BorrowCheck(true);
            }

            return ready.BorrowerId == borrowerId
                ? new BorrowCheck(true)
                : new BorrowCheck(false, "This item is being held for another borrower.");
        }

        public async Task MarkCollectedAsync(
            int itemId,
            int borrowerId,
            CancellationToken ct = default)
        {
            Reservation? ready = await _context.Reservations
                .FirstOrDefaultAsync(hold =>
                    hold.ItemId == itemId &&
                    hold.BorrowerId == borrowerId &&
                    hold.Status == ReservationStatus.Ready, ct);

            if (ready != null)
            {
                ready.Status = ReservationStatus.Collected;
            }
        }

        public async Task<int> ExpireOverdueHoldsAsync(CancellationToken ct = default)
        {
            DateTime now = DateTime.UtcNow;

            List<Reservation> expired = await _context.Reservations
                .Where(hold =>
                    hold.Status == ReservationStatus.Ready &&
                    hold.ExpiresAt != null &&
                    hold.ExpiresAt < now)
                .ToListAsync(ct);

            foreach (Reservation hold in expired)
            {
                hold.Status = ReservationStatus.Expired;
                await _context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Hold {HoldId} expired; offering item {ItemId} to the next borrower.",
                    hold.Id,
                    hold.ItemId);

                await OnItemAvailableAsync(hold.ItemId, ct);
            }

            // Safety net: an Available item should never have people waiting for it
            // (for example if a return was saved but the queue update failed).
            List<int> stranded = await _context.Reservations
                .Where(hold =>
                    hold.Status == ReservationStatus.Waiting &&
                    hold.Item.Status == ItemStatus.Available)
                .Select(hold => hold.ItemId)
                .Distinct()
                .ToListAsync(ct);

            foreach (int itemId in stranded)
            {
                await OnItemAvailableAsync(itemId, ct);
            }

            return expired.Count;
        }

        // 1 = front of the queue. Counts active holds placed before this one.
        private async Task<int> GetPositionAsync(
            Reservation reservation,
            CancellationToken ct)
        {
            int ahead = await _context.Reservations
                .AsNoTracking()
                .CountAsync(hold =>
                    hold.ItemId == reservation.ItemId &&
                    (hold.Status == ReservationStatus.Waiting ||
                     hold.Status == ReservationStatus.Ready) &&
                    (hold.CreatedAt < reservation.CreatedAt ||
                     (hold.CreatedAt == reservation.CreatedAt &&
                      hold.Id < reservation.Id)), ct);

            return ahead + 1;
        }
    }
}
