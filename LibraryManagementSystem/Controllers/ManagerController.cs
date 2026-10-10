using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Manager)]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            DateTime today = DateTime.Today;

            // One grouped query calculates all item-status statistics.
            var itemStatistics = await _context.Items
                .AsNoTracking()
                .GroupBy(item => 1)
                .Select(group => new
                {
                    Total = group.Count(),

                    Available = group.Count(item =>
                        item.Status == ItemStatus.Available),

                    Borrowed = group.Count(item =>
                        item.Status == ItemStatus.Borrowed),

                    Damaged = group.Count(item =>
                        item.Status == ItemStatus.Damaged),

                    Destroyed = group.Count(item =>
                        item.Status == ItemStatus.Destroyed)
                })
                .FirstOrDefaultAsync();

            // TPH inheritance allows each inherited item type to be counted.
            int totalBooks = await _context.Books
                .AsNoTracking()
                .CountAsync();

            int totalMusicItems = await _context.Music
                .AsNoTracking()
                .CountAsync();

            int totalToys = await _context.Toys
                .AsNoTracking()
                .CountAsync();

            // One grouped query calculates borrowing and fine statistics.
            var loanStatistics = await _context.Loans
                .AsNoTracking()
                .GroupBy(loan => 1)
                .Select(group => new
                {
                    Total = group.Count(),

                    Active = group.Count(loan =>
                        loan.ReturnDate == null),

                    Overdue = group.Count(loan =>
                        loan.ReturnDate == null &&
                        loan.DueDate < today),

                    Returned = group.Count(loan =>
                        loan.ReturnDate != null),

                    TotalFines = group.Sum(loan =>
                        loan.FineAmount),

                    PaidFines = group.Sum(loan =>
                        loan.FinePaid
                            ? loan.FineAmount
                            : 0m),

                    OutstandingFines = group.Sum(loan =>
                        !loan.FinePaid
                            ? loan.FineAmount
                            : 0m),

                    UnpaidFineCount = group.Count(loan =>
                        loan.FineAmount > 0 &&
                        !loan.FinePaid)
                })
                .FirstOrDefaultAsync();

            // One grouped query calculates borrower statistics.
            var borrowerStatistics = await _context.Borrowers
                .AsNoTracking()
                .GroupBy(borrower => 1)
                .Select(group => new
                {
                    Total = group.Count(),

                    Active = group.Count(borrower =>
                        borrower.IsActive)
                })
                .FirstOrDefaultAsync();

            ManagerDashboardViewModel model = new()
            {
                TotalItems = itemStatistics?.Total ?? 0,
                AvailableItems = itemStatistics?.Available ?? 0,
                BorrowedItems = itemStatistics?.Borrowed ?? 0,
                DamagedItems = itemStatistics?.Damaged ?? 0,
                DestroyedItems = itemStatistics?.Destroyed ?? 0,

                TotalBooks = totalBooks,
                TotalMusicItems = totalMusicItems,
                TotalToys = totalToys,

                TotalLoans = loanStatistics?.Total ?? 0,
                ActiveLoans = loanStatistics?.Active ?? 0,
                OverdueLoans = loanStatistics?.Overdue ?? 0,
                ReturnedLoans = loanStatistics?.Returned ?? 0,

                TotalBorrowers = borrowerStatistics?.Total ?? 0,
                ActiveBorrowers = borrowerStatistics?.Active ?? 0,

                TotalFinesCharged =
                    loanStatistics?.TotalFines ?? 0,

                PaidFines =
                    loanStatistics?.PaidFines ?? 0,

                OutstandingFines =
                    loanStatistics?.OutstandingFines ?? 0,

                UnpaidFineCount =
                    loanStatistics?.UnpaidFineCount ?? 0
            };

            return View(model);
        }
    }
}