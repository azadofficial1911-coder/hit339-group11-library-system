using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReportViewModel> GenerateAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string? itemType,
            CancellationToken cancellationToken = default)
        {
            var report = new ReportViewModel
            {
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date,
                ItemType = itemType?.Trim()
            };

            if (report.FromDate.HasValue &&
                report.ToDate.HasValue &&
                report.FromDate.Value > report.ToDate.Value)
            {
                report.Errors.Add(
                    "The start date must be on or before the end date.");
            }

            string[] types = { "Book", "Music", "Toy" };

            if (!string.IsNullOrEmpty(report.ItemType))
            {
                var matchedType = types.FirstOrDefault(type =>
                    type.Equals(
                        report.ItemType,
                        StringComparison.OrdinalIgnoreCase));

                if (matchedType == null)
                {
                    report.Errors.Add("Choose Book, Music or Toy.");
                }
                else
                {
                    report.ItemType = matchedType;
                }
            }

            if (report.Errors.Count > 0)
            {
                return report;
            }

            var itemsQuery = _context.Items.AsNoTracking();
            var loansQuery = _context.Loans.AsNoTracking();

            switch (report.ItemType)
            {
                case "Book":
                    itemsQuery = itemsQuery.Where(item => item is Book);
                    loansQuery = loansQuery.Where(loan => loan.Item is Book);
                    break;

                case "Music":
                    itemsQuery = itemsQuery.Where(item => item is Music);
                    loansQuery = loansQuery.Where(loan => loan.Item is Music);
                    break;

                case "Toy":
                    itemsQuery = itemsQuery.Where(item => item is Toy);
                    loansQuery = loansQuery.Where(loan => loan.Item is Toy);
                    break;
            }

            // Inventory shows current stock.
            // Dates filter loans by their borrowing date.
            if (report.FromDate.HasValue)
            {
                var start = report.FromDate.Value;
                loansQuery = loansQuery.Where(
                    loan => loan.BorrowDate >= start);
            }

            if (report.ToDate.HasValue)
            {
                var end = report.ToDate.Value;
                loansQuery = loansQuery.Where(
                    loan => loan.BorrowDate.Date <= end);
            }

            var inventory = await itemsQuery
                .Select(item => new
                {
                    ItemType = item is Book ? "Book" :
                        item is Music ? "Music" : "Toy",
                    item.Status
                })
                .GroupBy(item => new { item.ItemType, item.Status })
                .Select(group => new
                {
                    group.Key.ItemType,
                    group.Key.Status,
                    Count = group.Count()
                })
                .ToListAsync(cancellationToken);

            report.Inventory = inventory
                .Select(row => new InventoryReportRow
                {
                    ItemType = row.ItemType,
                    Status = row.Status.ToString(),
                    Count = row.Count
                })
                .OrderBy(row => row.ItemType)
                .ThenBy(row => row.Status)
                .ToList();

            report.TotalItems = inventory.Sum(row => row.Count);

            report.AvailableItems = inventory
                .Where(row => row.Status == ItemStatus.Available)
                .Sum(row => row.Count);

            report.BorrowedItems = inventory
                .Where(row => row.Status == ItemStatus.Borrowed)
                .Sum(row => row.Count);

            report.Loans = await loansQuery
                .OrderByDescending(loan => loan.BorrowDate)
                .ThenByDescending(loan => loan.Id)
                .Select(loan => new LoanReportRow
                {
                    LoanId = loan.Id,
                    LibraryCode = loan.Item.LibraryCode,
                    ItemName = loan.Item.Name,
                    ItemType = loan.Item is Book ? "Book" :
                        loan.Item is Music ? "Music" : "Toy",
                    BorrowDate = loan.BorrowDate,
                    DueDate = loan.DueDate,
                    ReturnDate = loan.ReturnDate,
                    FineAmount = loan.FineAmount,
                    FinePaid = loan.FinePaid
                })
                .ToListAsync(cancellationToken);

            var today = DateTime.Today;

            foreach (var loan in report.Loans)
            {
                loan.Status = loan.ReturnDate.HasValue
                    ? "Returned"
                    : loan.DueDate.Date < today
                        ? "Overdue"
                        : "Active";
            }

            report.TotalLoans = report.Loans.Count;

            report.ActiveLoans = report.Loans.Count(
                loan => !loan.ReturnDate.HasValue);

            report.OverdueLoans = report.Loans.Count(
                loan => loan.Status == "Overdue");

            report.ReturnedLoans = report.Loans.Count(
                loan => loan.ReturnDate.HasValue);

            report.AssessedFines = report.Loans.Sum(
                loan => loan.FineAmount);

            report.PaidFines = report.Loans
                .Where(loan => loan.FinePaid)
                .Sum(loan => loan.FineAmount);

            report.OutstandingFines = report.Loans
                .Where(loan => !loan.FinePaid)
                .Sum(loan => loan.FineAmount);

            return report;
        }
    }
}