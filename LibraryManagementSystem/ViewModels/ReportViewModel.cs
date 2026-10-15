namespace LibraryManagementSystem.ViewModels
{
    public class ReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ItemType { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        public List<string> Errors { get; set; } = new();

        public int TotalItems { get; set; }
        public int AvailableItems { get; set; }
        public int BorrowedItems { get; set; }

        public int TotalLoans { get; set; }
        public int ActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public int ReturnedLoans { get; set; }

        public decimal AssessedFines { get; set; }
        public decimal PaidFines { get; set; }
        public decimal OutstandingFines { get; set; }

        public List<InventoryReportRow> Inventory { get; set; } = new();
        public List<LoanReportRow> Loans { get; set; } = new();
    }

    public class InventoryReportRow
    {
        public string ItemType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class LoanReportRow
    {
        public int LoanId { get; set; }
        public string LibraryCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;

        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public string Status { get; set; } = string.Empty;
        public decimal FineAmount { get; set; }
        public bool FinePaid { get; set; }
    }
}