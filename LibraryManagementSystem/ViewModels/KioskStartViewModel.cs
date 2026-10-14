using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    public class KioskStartViewModel
    {
        [Required(ErrorMessage = "Scan or enter your borrower code.")]
        [StringLength(20)]
        [Display(Name = "Borrower code")]
        public string BorrowerCode { get; set; } = string.Empty;
    }
}