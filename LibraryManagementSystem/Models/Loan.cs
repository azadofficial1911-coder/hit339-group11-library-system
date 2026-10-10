using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LibraryManagementSystem.Models
{
    // Represents one borrowing transaction between a borrower and an item.
    public class Loan
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Library Item")]
        public int ItemId { get; set; }

        // Navigation property used by Entity Framework.
        [ValidateNever]
        public Item Item { get; set; } = null!;

        [Required]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        // Navigation property used by Entity Framework.
        [ValidateNever]
        public Borrower Borrower { get; set; } = null!;

        [DataType(DataType.Date)]
        [Display(Name = "Borrow Date")]
        public DateTime BorrowDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

        [DataType(DataType.Date)]
        [Display(Name = "Return Date")]
        public DateTime? ReturnDate { get; set; }

        [Range(typeof(decimal), "0", "10000")]
        [Display(Name = "Fine Amount")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal FineAmount { get; set; }

        [Display(Name = "Fine Paid")]
        public bool FinePaid { get; set; }

        [StringLength(256)]
        [Display(Name = "Issued By")]
        public string IssuedBy { get; set; } = string.Empty;

        [StringLength(256)]
        [Display(Name = "Returned By")]
        public string? ReturnedBy { get; set; }

        // Calculated property—this does not require a database column.
        [NotMapped]
        public bool IsReturned => ReturnDate.HasValue;
    }
}