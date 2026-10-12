using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    // Reception enters an item code and a borrower code to place a hold.
    public class PlaceHoldViewModel
    {
        [Required(ErrorMessage = "The item library code is required.")]
        [StringLength(50)]
        [Display(Name = "Item Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "The borrower code is required.")]
        [StringLength(20)]
        [Display(Name = "Borrower Code")]
        public string BorrowerCode { get; set; } = string.Empty;
    }
}
