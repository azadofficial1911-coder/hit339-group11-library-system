using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.ViewModels
{
    // Reception enters an item's code to process its return.
    public class ReturnItemViewModel
    {
        [Required(ErrorMessage = "The item library code is required.")]
        [StringLength(50)]
        [Display(Name = "Item Library Code")]
        public string LibraryCode { get; set; } = string.Empty;
    }
}