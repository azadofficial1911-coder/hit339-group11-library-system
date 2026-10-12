using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LibraryManagementSystem.Models
{
    // Hold lifecycle: Waiting -> Ready -> Collected, or Expired / Cancelled.
    public enum ReservationStatus
    {
        Waiting,
        Ready,
        Collected,
        Expired,
        Cancelled
    }

    // Represents one borrower's place in the queue for an item (R5).
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Library Item")]
        public int ItemId { get; set; }

        [ValidateNever]
        public Item Item { get; set; } = null!;

        [Required]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        [ValidateNever]
        public Borrower Borrower { get; set; } = null!;

        // Sets the queue order (first in, first out).
        [Display(Name = "Placed At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

        // Set when the hold becomes Ready.
        public DateTime? ReadyAt { get; set; }

        // Last moment the borrower can collect a Ready item.
        public DateTime? ExpiresAt { get; set; }

        [StringLength(256)]
        [Display(Name = "Placed By")]
        public string PlacedBy { get; set; } = string.Empty;
    }
}
