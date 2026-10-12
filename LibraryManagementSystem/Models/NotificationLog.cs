using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LibraryManagementSystem.Models
{
    public enum NotificationChannel
    {
        Email,
        Sms
    }

    public enum NotificationEventType
    {
        Borrowed,
        DueSoon,
        Overdue,
        FineAssessed,
        HoldReady
    }

    // One simulated email or SMS message (R3). Nothing is sent outside the application.
    public class NotificationLog
    {
        public int Id { get; set; }

        public int BorrowerId { get; set; }

        [ValidateNever]
        public Borrower Borrower { get; set; } = null!;

        public NotificationChannel Channel { get; set; }

        [Display(Name = "Event")]
        public NotificationEventType EventType { get; set; }

        // The email address or phone number the message would be sent to.
        [Required]
        [StringLength(150)]
        public string Recipient { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Body { get; set; } = string.Empty;

        public int? LoanId { get; set; }

        public int? ReservationId { get; set; }

        // Unique per event and channel. The unique index on this column
        // stops repeated checks from creating duplicate messages.
        [Required]
        [StringLength(200)]
        public string DedupKey { get; set; } = string.Empty;

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(40)]
        public string Status { get; set; } = "Simulated sent";
    }
}
