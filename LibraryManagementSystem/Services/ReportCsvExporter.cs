using System.Globalization;
using System.Text;
using LibraryManagementSystem.ViewModels;

namespace LibraryManagementSystem.Services
{
    public static class ReportCsvExporter
    {
        public static byte[] Export(ReportViewModel report)
        {
            var csv = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;

            void Row(params string[] values)
            {
                csv.AppendLine(string.Join(",", values.Select(Escape)));
            }

            void Metric(string name, string value)
            {
                Row("Summary", name, value);
            }

            Row("Section", "Metric", "Value");
            Row("Filters", "Borrowed from",
                report.FromDate?.ToString("yyyy-MM-dd") ?? "Any");
            Row("Filters", "Borrowed to",
                report.ToDate?.ToString("yyyy-MM-dd") ?? "Any");
            Row("Filters", "Item type", report.ItemType ?? "All");
            Row("Filters", "Generated",
                report.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"));

            Metric("Total items", report.TotalItems.ToString(culture));
            Metric("Available items", report.AvailableItems.ToString(culture));
            Metric("Borrowed items", report.BorrowedItems.ToString(culture));
            Metric("Total loans", report.TotalLoans.ToString(culture));
            Metric("Active loans (includes overdue)",
                report.ActiveLoans.ToString(culture));
            Metric("Overdue loans", report.OverdueLoans.ToString(culture));
            Metric("Returned loans", report.ReturnedLoans.ToString(culture));
            Metric("Assessed fines AUD",
                report.AssessedFines.ToString("F2", culture));
            Metric("Marked paid fines AUD",
                report.PaidFines.ToString("F2", culture));
            Metric("Outstanding fines AUD",
                report.OutstandingFines.ToString("F2", culture));

            Row("Notes", "Inventory",
                "Current stock; loan date filters do not apply.");
            Row("Notes", "Fines",
                "Recorded fines for selected loans; not payment-date totals.");

            Row();
            Row("Item type", "Current status", "Item count");

            foreach (var item in report.Inventory)
            {
                Row(item.ItemType, item.Status, item.Count.ToString(culture));
            }

            Row();
            Row("Loan ID", "Library code", "Item name", "Item type",
                "Borrowed", "Due", "Returned", "Current loan status",
                "Recorded fine AUD", "Marked paid");

            foreach (var loan in report.Loans)
            {
                Row(
                    loan.LoanId.ToString(culture),
                    loan.LibraryCode,
                    loan.ItemName,
                    loan.ItemType,
                    loan.BorrowDate.ToString("yyyy-MM-dd"),
                    loan.DueDate.ToString("yyyy-MM-dd"),
                    loan.ReturnDate?.ToString("yyyy-MM-dd") ?? "",
                    loan.Status,
                    loan.FineAmount.ToString("F2", culture),
                    loan.FinePaid ? "Yes" : "No");
            }

            // Include a UTF-8 BOM for spreadsheet compatibility.
            var encoding = new UTF8Encoding(true);

            return encoding.GetPreamble()
                .Concat(encoding.GetBytes(csv.ToString()))
                .ToArray();
        }

        private static string Escape(string value)
        {
            // Prevent spreadsheet formulas in exported text fields.
            var trimmed = value.TrimStart();

            if (trimmed.Length > 0 &&
                "=+-@".Contains(trimmed[0]))
            {
                value = "'" + value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}