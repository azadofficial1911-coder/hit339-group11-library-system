using Microsoft.Extensions.Options;

namespace LibraryManagementSystem.Services
{
    public class FineCalculator : IFineCalculator
    {
        private readonly FineSettings _settings;

        public FineCalculator(IOptions<FineSettings> options)
        {
            _settings = options.Value;
        }

        public int CalculateLateDays(
            DateTime dueDate,
            DateTime returnDate)
        {
            if (returnDate.Date <= dueDate.Date)
            {
                return 0;
            }

            return (returnDate.Date - dueDate.Date).Days;
        }

        public decimal CalculateFine(
            DateTime dueDate,
            DateTime returnDate)
        {
            int lateDays = CalculateLateDays(dueDate, returnDate);

            if (lateDays == 0)
            {
                return 0;
            }

            decimal calculatedFine =
                lateDays * _settings.DailyLateFee;

            return Math.Min(
                calculatedFine,
                _settings.MaximumFine);
        }
    }
}