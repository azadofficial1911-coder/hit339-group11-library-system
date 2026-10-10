namespace LibraryManagementSystem.Services
{
    // Fine values will be loaded from appsettings.json.
    public class FineSettings
    {
        public const string SectionName = "FineSettings";

        public decimal DailyLateFee { get; set; } = 2.00m;

        public decimal MaximumFine { get; set; } = 100.00m;
    }
}