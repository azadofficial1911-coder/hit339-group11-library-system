using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Toy : Item
    {
        [Required(ErrorMessage = "The toy type is required.")]
        [StringLength(50)]
        [Display(Name = "Toy Type")]
        public string ToyType { get; set; } = string.Empty;

        [Range(0, 18)]
        [Display(Name = "Recommended Age")]
        public int RecommendedAge { get; set; }

        [Required(ErrorMessage = "The manufacturer is required.")]
        [StringLength(100)]
        public string Manufacturer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Safety information is required.")]
        [StringLength(250)]
        [Display(Name = "Safety Information")]
        public string SafetyInformation { get; set; } = string.Empty;
    }
}