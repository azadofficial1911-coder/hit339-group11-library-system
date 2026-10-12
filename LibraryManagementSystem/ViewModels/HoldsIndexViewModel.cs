using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class HoldRowViewModel
    {
        public int ReservationId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public string BorrowerName { get; set; } = string.Empty;

        public string BorrowerCode { get; set; } = string.Empty;

        public ReservationStatus Status { get; set; }

        // 1 = front of the queue. Null for closed holds.
        public int? Position { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive =>
            Status == ReservationStatus.Waiting ||
            Status == ReservationStatus.Ready;
    }

    public class HoldsIndexViewModel
    {
        public List<HoldRowViewModel> Holds { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;

        // "Active" (default), "All", or a ReservationStatus name.
        public string SelectedStatus { get; set; } = "Active";
    }
}
