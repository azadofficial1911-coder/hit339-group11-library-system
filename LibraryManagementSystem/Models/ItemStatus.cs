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
        Destroyed,

        // Held at the front of a reservation queue for one borrower (R5).
        [Display(Name = "Reserved")]
        Reserved,

        [Display(Name = "In transit")]
        InTransit
    }
}
