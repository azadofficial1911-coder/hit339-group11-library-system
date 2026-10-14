using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class KioskBorrowViewModel
    {
        [Required(ErrorMessage = "Scan or enter the item code.")]
        [StringLength(20)]
        [Display(Name = "Item library code")]
        public string LibraryCode { get; set; } = string.Empty;

        // Filled by the controller after checking the database.
        public string? ItemName { get; set; }

        public DateTime? DueDate { get; set; }

        // One-time confirmation token checked against the session.
        public string? ConfirmationToken { get; set; }

        public bool ReadyToConfirm =>
            !string.IsNullOrEmpty(ItemName) &&
            DueDate.HasValue &&
            !string.IsNullOrEmpty(ConfirmationToken);
    }
}