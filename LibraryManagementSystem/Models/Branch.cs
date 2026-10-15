using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Branch code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Branch name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [StringLength(250)]
        [Display(Name = "Opening hours")]
        public string? OpeningHours { get; set; }

        [StringLength(500)]
        [Display(Name = "Accessibility information")]
        public string? AccessibilityInformation { get; set; }

        [Display(Name = "Active branch")]
        public bool IsActive { get; set; } = true;
    }
}