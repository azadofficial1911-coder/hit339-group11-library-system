namespace LibraryManagementSystem.Services
{
    // Simulates email and SMS by writing each message to the NotificationLogs table.
    public interface INotificationService
    {
        // Email when an item is borrowed.
        Task NotifyBorrowedAsync(int loanId, CancellationToken ct = default);

        // Email and SMS when a returned loan has a late fine.
        Task NotifyFineAssessedAsync(int loanId, CancellationToken ct = default);

        // Email and SMS when a hold becomes Ready for collection.
        Task NotifyHoldReadyAsync(int reservationId, CancellationToken ct = default);

        // Creates due-soon and overdue reminders. Safe to run repeatedly:
        // each message has a unique key, so duplicates are skipped.
        // Returns the number of new messages logged.
        Task<int> RunScheduledChecksAsync(CancellationToken ct = default);
    }
}
