namespace LibraryManagementSystem.Services
{
    // Reservation rules loaded from appsettings.json.
    public class ReservationSettings
    {
        public const string SectionName = "ReservationSettings";

        // Days a borrower has to collect an item once their hold is Ready.
        public int PickupDays { get; set; } = 3;
    }
}
