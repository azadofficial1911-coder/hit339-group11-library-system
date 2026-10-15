using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class ReceptionDesk
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Desk code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Desk name")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Select a branch.")]
        [Display(Name = "Branch")]
        public int BranchId { get; set; }

        public Branch? Branch { get; set; }

        [Display(Name = "Active desk")]
        public bool IsActive { get; set; } = true;
    }
}