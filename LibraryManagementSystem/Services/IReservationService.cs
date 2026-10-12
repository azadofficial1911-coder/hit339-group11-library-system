using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Services
{
    public sealed record HoldResult(
        bool Succeeded,
        string Message,
        Reservation? Reservation = null)
    {
        public static HoldResult Ok(string message, Reservation? reservation = null)
            => new(true, message, reservation);

        public static HoldResult Fail(string message)
            => new(false, message);
    }

    public sealed record BorrowCheck(bool Allowed, string? Reason = null);

    // One active hold, shaped for the kiosk account summary and staff screens.
    public sealed record HoldSummary(
        int ReservationId,
        string ItemName,
        string LibraryCode,
        ReservationStatus Status,
        int Position,
        DateTime? ExpiresAt);

    public interface IReservationService
    {
        // Places a hold on a Borrowed, Damaged or Reserved item.
        Task<HoldResult> PlaceHoldAsync(
            string libraryCode,
            string borrowerCode,
            string placedBy,
            CancellationToken ct = default);

        // Cancels a Waiting or Ready hold. Everyone behind moves up.
        Task<HoldResult> CancelHoldAsync(int reservationId, CancellationToken ct = default);

        // The borrower's active holds with queue positions (1 = front of the queue).
        Task<List<HoldSummary>> GetHoldsForBorrowerAsync(int borrowerId, CancellationToken ct = default);

        // Call after an item becomes Available (return or repair).
        // Gives it to the first Waiting hold, or leaves it Available.
        Task<Reservation?> OnItemAvailableAsync(int itemId, CancellationToken ct = default);

        // Call after an admin edits an item's status.
        Task OnItemStatusChangedAsync(
            int itemId,
            ItemStatus oldStatus,
            ItemStatus newStatus,
            CancellationToken ct = default);

        // Call before issuing a loan: a Ready item can only go to the holder.
        Task<BorrowCheck> CheckBorrowAllowedAsync(
            int itemId,
            int borrowerId,
            CancellationToken ct = default);

        // Marks the borrower's Ready hold Collected. Does NOT save;
        // the caller saves it together with the new loan.
        Task MarkCollectedAsync(int itemId, int borrowerId, CancellationToken ct = default);

        // Expires Ready holds past their pickup window and offers the item to the next borrower.
        Task<int> ExpireOverdueHoldsAsync(CancellationToken ct = default);
    }
}
