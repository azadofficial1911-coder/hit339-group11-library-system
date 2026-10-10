using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public abstract class Item
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "The item name is required.")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A description is required.")]
        [StringLength(500, MinimumLength = 5)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "The library code is required.")]
        [StringLength(20, MinimumLength = 4)]
        [RegularExpression(
            @"^[A-Za-z0-9-]+$",
            ErrorMessage = "Use only letters, numbers and hyphens."
        )]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Item Status")]
        public ItemStatus Status { get; set; } = ItemStatus.Available;

        [DataType(DataType.Date)]
        [Display(Name = "Date Added")]
        public DateTime DateAdded { get; set; } = DateTime.Today;
    }
}