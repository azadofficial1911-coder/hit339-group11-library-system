using System;
using System.Collections.Generic;

namespace LibraryManagementSystem.ViewModels
{
    public class KioskAccountViewModel
    {
        public string FirstName { get; set; } = string.Empty;

        public List<KioskLoanViewModel> ActiveLoans { get; set; } = new();

        public decimal OutstandingFines { get; set; }
    }

    public class KioskLoanViewModel
    {
        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public bool IsOverdue => DueDate.Date < DateTime.Today;
    }
}