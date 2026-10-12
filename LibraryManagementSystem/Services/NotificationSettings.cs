namespace LibraryManagementSystem.Services
{
    // Notification rules loaded from appsettings.json.
    public class NotificationSettings
    {
        public const string SectionName = "NotificationSettings";

        // A "due soon" reminder is created when a loan is due within this many days.
        public int DueSoonDays { get; set; } = 2;

        // How often the background job checks for reminders while the app runs.
        public int CheckIntervalMinutes { get; set; } = 60;
    }
}
