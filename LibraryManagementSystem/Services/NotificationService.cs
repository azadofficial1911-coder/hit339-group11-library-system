using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFineCalculator _fineCalculator;
        private readonly NotificationSettings _settings;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IFineCalculator fineCalculator,
            IOptions<NotificationSettings> options,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _fineCalculator = fineCalculator;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task NotifyBorrowedAsync(int loanId, CancellationToken ct = default)
        {
            try
            {
                Loan? loan = await LoadLoanAsync(loanId, ct);

                if (loan == null)
                {
                    return;
                }

                await LogAsync(
                    loan.Borrower,
                    NotificationChannel.Email,
                    NotificationEventType.Borrowed,
                    $"You borrowed \"{loan.Item.Name}\"",
                    $"Hi {loan.Borrower.FullName}, you borrowed \"{loan.Item.Name}\" " +
                    $"({loan.Item.LibraryCode}). Please return it by {loan.DueDate:d MMM yyyy}.",
                    $"BORROWED:{loan.Id}",
                    loan.Id,
                    null,
                    ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed message must never undo a completed borrowing.
                _logger.LogWarning(ex, "Could not log the borrowed notification for loan {LoanId}.", loanId);
            }
        }

        public async Task NotifyFineAssessedAsync(int loanId, CancellationToken ct = default)
        {
            try
            {
                Loan? loan = await LoadLoanAsync(loanId, ct);

                if (loan == null || loan.FineAmount <= 0)
                {
                    return;
                }

                await LogBothAsync(
                    loan.Borrower,
                    NotificationEventType.FineAssessed,
                    $"Late fine for \"{loan.Item.Name}\"",
                    $"Hi {loan.Borrower.FullName}, \"{loan.Item.Name}\" was returned late. " +
                    $"A fine of {loan.FineAmount:C} has been assessed and is unpaid.",
                    $"Fine of {loan.FineAmount:C} assessed for late return of \"{loan.Item.Name}\". Payment due at the library.",
                    $"FINE:{loan.Id}",
                    loan.Id,
                    null,
                    ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not log the fine notification for loan {LoanId}.", loanId);
            }
        }

        public async Task NotifyHoldReadyAsync(int reservationId, CancellationToken ct = default)
        {
            try
            {
                Reservation? hold = await _context.Reservations
                    .AsNoTracking()
                    .Include(record => record.Item)
                    .Include(record => record.Borrower)
                    .FirstOrDefaultAsync(record => record.Id == reservationId, ct);

                if (hold == null || hold.Status != ReservationStatus.Ready)
                {
                    return;
                }

                string collectBy = hold.ExpiresAt?.ToLocalTime().ToString("d MMM yyyy") ?? "soon";

                await LogBothAsync(
                    hold.Borrower,
                    NotificationEventType.HoldReady,
                    $"\"{hold.Item.Name}\" is ready to collect",
                    $"Hi {hold.Borrower.FullName}, \"{hold.Item.Name}\" ({hold.Item.LibraryCode}) " +
                    $"is now ready for you. Please collect it by {collectBy}.",
                    $"\"{hold.Item.Name}\" is ready for you. Collect by {collectBy}.",
                    $"HOLDREADY:{hold.Id}",
                    null,
                    hold.Id,
                    ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not log the hold-ready notification for hold {HoldId}.", reservationId);
            }
        }

        public async Task<int> RunScheduledChecksAsync(CancellationToken ct = default)
        {
            DateTime today = DateTime.Today;
            DateTime soonLimit = today.AddDays(_settings.DueSoonDays);

            List<Loan> loans = await _context.Loans
                .AsNoTracking()
                .Include(loan => loan.Item)
                .Include(loan => loan.Borrower)
                .Where(loan =>
                    loan.ReturnDate == null &&
                    loan.DueDate <= soonLimit)
                .ToListAsync(ct);

            int created = 0;

            foreach (Loan loan in loans)
            {
                DateTime due = loan.DueDate.Date;

                if (due >= today)
                {
                    // Due soon: one reminder per loan.
                    int daysLeft = (due - today).Days;
                    string when = daysLeft == 0 ? "today" : $"in {daysLeft} day(s)";

                    created += await LogBothAsync(
                        loan.Borrower,
                        NotificationEventType.DueSoon,
                        $"\"{loan.Item.Name}\" is due {when}",
                        $"Hi {loan.Borrower.FullName}, \"{loan.Item.Name}\" is due {when} " +
                        $"({due:d MMM yyyy}). Please return it on time to avoid a fine.",
                        $"\"{loan.Item.Name}\" is due {when} ({due:d MMM}). Return on time to avoid a fine.",
                        $"DUESOON:{loan.Id}:{due:yyyyMMdd}",
                        loan.Id,
                        null,
                        ct);
                }
                else
                {
                    // Overdue: the fine is still accruing. One reminder per loan per week.
                    int overdueDays = (today - due).Days;
                    int week = (overdueDays - 1) / 7;
                    decimal fine = _fineCalculator.CalculateFine(due, today);

                    created += await LogBothAsync(
                        loan.Borrower,
                        NotificationEventType.Overdue,
                        $"\"{loan.Item.Name}\" is overdue",
                        $"Hi {loan.Borrower.FullName}, \"{loan.Item.Name}\" is {overdueDays} day(s) overdue. " +
                        $"The fine so far is {fine:C}. Return the item to stop further fines.",
                        $"\"{loan.Item.Name}\" is {overdueDays} day(s) overdue. Fine so far {fine:C}.",
                        $"OVERDUE:{loan.Id}:{week}",
                        loan.Id,
                        null,
                        ct);
                }
            }

            if (created > 0)
            {
                _logger.LogInformation("Scheduled check logged {Count} new notification(s).", created);
            }

            return created;
        }

        private Task<Loan?> LoadLoanAsync(int loanId, CancellationToken ct)
        {
            return _context.Loans
                .AsNoTracking()
                .Include(loan => loan.Item)
                .Include(loan => loan.Borrower)
                .FirstOrDefaultAsync(loan => loan.Id == loanId, ct);
        }

        // Logs the same event as an email (long text) and an SMS (short text).
        private async Task<int> LogBothAsync(
            Borrower borrower,
            NotificationEventType eventType,
            string subject,
            string emailBody,
            string smsBody,
            string baseKey,
            int? loanId,
            int? reservationId,
            CancellationToken ct)
        {
            int count = 0;

            if (await LogAsync(borrower, NotificationChannel.Email, eventType, subject,
                    emailBody, baseKey, loanId, reservationId, ct))
            {
                count++;
            }

            if (await LogAsync(borrower, NotificationChannel.Sms, eventType, subject,
                    smsBody, baseKey, loanId, reservationId, ct))
            {
                count++;
            }

            return count;
        }

        // Returns false (and logs nothing) when the same message already exists.
        private async Task<bool> LogAsync(
            Borrower borrower,
            NotificationChannel channel,
            NotificationEventType eventType,
            string subject,
            string body,
            string baseKey,
            int? loanId,
            int? reservationId,
            CancellationToken ct)
        {
            string key = $"{baseKey}:{channel}";

            if (await _context.NotificationLogs.AnyAsync(n => n.DedupKey == key, ct))
            {
                return false;
            }

            NotificationLog row = new()
            {
                BorrowerId = borrower.Id,
                Channel = channel,
                EventType = eventType,
                Recipient = channel == NotificationChannel.Email
                    ? borrower.Email
                    : borrower.PhoneNumber,
                Subject = Truncate(subject, 200),
                Body = Truncate(body, 1000),
                LoanId = loanId,
                ReservationId = reservationId,
                DedupKey = key,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(row);

            try
            {
                await _context.SaveChangesAsync(ct);
                return true;
            }
            catch (DbUpdateException)
            {
                // Another request logged the same message first; the unique index stopped ours.
                _context.Entry(row).State = EntityState.Detached;
                return false;
            }
        }

        private static string Truncate(string text, int maxLength)
            => text.Length <= maxLength ? text : text[..maxLength];
    }
}
