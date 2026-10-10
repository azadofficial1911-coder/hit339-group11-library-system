namespace LibraryManagementSystem.ViewModels
{
    // Contains the statistical information displayed to managers.
    public class ManagerDashboardViewModel
    {
        // Item-type statistics
        public int TotalItems { get; set; }

        public int TotalBooks { get; set; }

        public int TotalMusicItems { get; set; }

        public int TotalToys { get; set; }

        // Item-status statistics
        public int AvailableItems { get; set; }

        public int BorrowedItems { get; set; }

        public int DamagedItems { get; set; }

        public int DestroyedItems { get; set; }

        // Borrowing statistics
        public int TotalLoans { get; set; }

        public int ActiveLoans { get; set; }

        public int OverdueLoans { get; set; }

        public int ReturnedLoans { get; set; }

        // Borrower statistics
        public int TotalBorrowers { get; set; }

        public int ActiveBorrowers { get; set; }

        // Fine statistics
        public decimal TotalFinesCharged { get; set; }

        public decimal PaidFines { get; set; }

        public decimal OutstandingFines { get; set; }

        public int UnpaidFineCount { get; set; }

        // Calculated statistics do not require database columns.
        public double AvailabilityPercentage =>
            TotalItems == 0
                ? 0
                : (double)AvailableItems / TotalItems * 100;

        public double ReturnPercentage =>
            TotalLoans == 0
                ? 0
                : (double)ReturnedLoans / TotalLoans * 100;
    }
}