using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public enum ItemStatus
    {
        [Display(Name = "Available")]
        Available,

        [Display(Name = "Borrowed")]
        Borrowed,

        [Display(Name = "Damaged")]
        Damaged,

        [Display(Name = "Destroyed")]
        Destroyed
    }
}