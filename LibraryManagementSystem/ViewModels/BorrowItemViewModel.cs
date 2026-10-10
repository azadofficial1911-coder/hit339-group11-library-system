using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    // Contains only the information Reception is allowed to submit.
    public class BorrowItemViewModel
    {
        [Required(ErrorMessage = "The item library code is required.")]
        [StringLength(50)]
        [Display(Name = "Item Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "The borrower code is required.")]
        [StringLength(20)]
        [Display(Name = "Borrower Code")]
        public string BorrowerCode { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
    }
}