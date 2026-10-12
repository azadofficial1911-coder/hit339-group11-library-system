using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    public class NotificationsIndexViewModel
    {
        public List<NotificationLog> Notifications { get; set; } = new();

        public string SearchTerm { get; set; } = string.Empty;

        public string SelectedChannel { get; set; } = string.Empty;

        public string SelectedEvent { get; set; } = string.Empty;

        // Total matching rows; the page shows the latest 200.
        public int TotalMatching { get; set; }
    }
}
