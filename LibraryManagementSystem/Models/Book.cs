using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Book : Item
    {
        [Required(ErrorMessage = "The author's name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "The book genre is required.")]
        [StringLength(50)]
        public string Genre { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression(
            @"^[0-9Xx-]*$",
            ErrorMessage = "Enter a valid ISBN."
        )]
        [Display(Name = "ISBN")]
        public string? Isbn { get; set; }

        [Range(1000, 2100)]
        [Display(Name = "Publication Year")]
        public int PublicationYear { get; set; }
    }
}