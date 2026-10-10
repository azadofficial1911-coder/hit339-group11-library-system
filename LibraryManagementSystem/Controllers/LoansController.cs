using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Reception)]
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFineCalculator _fineCalculator;

        public LoansController(
            ApplicationDbContext context,
            IFineCalculator fineCalculator)
        {
            _context = context;
            _fineCalculator = fineCalculator;
        }

        // Displays active, overdue and returned loans.
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? loanStatus)
        {
            IQueryable<Loan> query = _context.Loans
                .AsNoTracking()
                .Include(loan => loan.Item)
                .Include(loan => loan.Borrower);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(loan =>
                    loan.Item.LibraryCode.Contains(searchTerm) ||
                    loan.Item.Name.Contains(searchTerm) ||
                    loan.Borrower.BorrowerCode.Contains(searchTerm) ||
                    loan.Borrower.FullName.Contains(searchTerm));
            }

            switch (loanStatus)
            {
                case "Active":
                    query = query.Where(loan =>
                        loan.ReturnDate == null);
                    break;

                case "Overdue":
                    DateTime today = DateTime.Today;

                    query = query.Where(loan =>
                        loan.ReturnDate == null &&
                        loan.DueDate < today);
                    break;

                case "Returned":
                    query = query.Where(loan =>
                        loan.ReturnDate != null);
                    break;

                case "UnpaidFine":
                    query = query.Where(loan =>
                        loan.ReturnDate != null &&
                        loan.FineAmount > 0 &&
                        !loan.FinePaid);
                    break;
            }

            ViewData["SearchTerm"] = searchTerm;
            ViewData["SelectedLoanStatus"] = loanStatus;

            List<Loan> loans = await query
                .OrderBy(loan => loan.ReturnDate.HasValue)
                .ThenBy(loan => loan.DueDate)
                .ThenByDescending(loan => loan.BorrowDate)
                .ToListAsync();

            return View(loans);
        }

        // Displays one complete loan record.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Loan? loan = await _context.Loans
                .AsNoTracking()
                .Include(record => record.Item)
                .Include(record => record.Borrower)
                .FirstOrDefaultAsync(record => record.Id == id);

            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // Displays the borrowing form.
        [HttpGet]
        public IActionResult Borrow()
        {
            return View(new BorrowItemViewModel());
        }

        // Creates a loan and changes the item status to Borrowed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrow(
            BorrowItemViewModel model)
        {
            model.LibraryCode =
                model.LibraryCode.Trim().ToUpperInvariant();

            model.BorrowerCode =
                model.BorrowerCode.Trim().ToUpperInvariant();

            if (model.DueDate.Date <= DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "The due date must be after today.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Item? item = await _context.Items
                .FirstOrDefaultAsync(record =>
                    record.LibraryCode.ToUpper() ==
                    model.LibraryCode);

            if (item == null)
            {
                ModelState.AddModelError(
                    nameof(model.LibraryCode),
                    "No library item was found with this code.");

                return View(model);
            }

            if (item.Status != ItemStatus.Available)
            {
                ModelState.AddModelError(
                    nameof(model.LibraryCode),
                    $"This item cannot be borrowed because its status is {item.Status}.");

                return View(model);
            }

            Borrower? borrower = await _context.Borrowers
                .FirstOrDefaultAsync(record =>
                    record.BorrowerCode.ToUpper() ==
                    model.BorrowerCode);

            if (borrower == null)
            {
                ModelState.AddModelError(
                    nameof(model.BorrowerCode),
                    "No borrower was found with this code.");

                return View(model);
            }

            if (!borrower.IsActive)
            {
                ModelState.AddModelError(
                    nameof(model.BorrowerCode),
                    "This borrower account is inactive.");

                return View(model);
            }

            bool alreadyBorrowed = await _context.Loans
                .AnyAsync(loan =>
                    loan.ItemId == item.Id &&
                    loan.ReturnDate == null);

            if (alreadyBorrowed)
            {
                ModelState.AddModelError(
                    nameof(model.LibraryCode),
                    "This item already has an active loan.");

                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                Loan loan = new()
                {
                    ItemId = item.Id,
                    BorrowerId = borrower.Id,
                    BorrowDate = DateTime.Today,
                    DueDate = model.DueDate.Date,
                    FineAmount = 0,
                    FinePaid = false,
                    IssuedBy =
                        User.Identity?.Name ?? "Unknown Reception User"
                };

                item.Status = ItemStatus.Borrowed;

                _context.Loans.Add(loan);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"\"{item.Name}\" was issued to {borrower.FullName} successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "The borrowing transaction could not be completed. " +
                    "The item may have been borrowed by another user.");

                return View(model);
            }
        }

        // Displays the return form.
        [HttpGet]
        public IActionResult Return()
        {
            return View(new ReturnItemViewModel());
        }

        // Returns an item and automatically calculates its late fine.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(
            ReturnItemViewModel model)
        {
            model.LibraryCode =
                model.LibraryCode.Trim().ToUpperInvariant();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Loan? activeLoan = await _context.Loans
                .Include(loan => loan.Item)
                .Include(loan => loan.Borrower)
                .FirstOrDefaultAsync(loan =>
                    loan.Item.LibraryCode.ToUpper() ==
                    model.LibraryCode &&
                    loan.ReturnDate == null);

            if (activeLoan == null)
            {
                ModelState.AddModelError(
                    nameof(model.LibraryCode),
                    "No active loan was found for this library code.");

                return View(model);
            }

            DateTime returnDate = DateTime.Today;

            decimal fine = _fineCalculator.CalculateFine(
                activeLoan.DueDate,
                returnDate);

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                activeLoan.ReturnDate = returnDate;
                activeLoan.FineAmount = fine;
                activeLoan.FinePaid = fine == 0;
                activeLoan.ReturnedBy =
                    User.Identity?.Name ?? "Unknown Reception User";

                activeLoan.Item.Status = ItemStatus.Available;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (fine > 0)
                {
                    TempData["SuccessMessage"] =
                        $"\"{activeLoan.Item.Name}\" was returned. " +
                        $"Late fine: {fine:C}.";
                }
                else
                {
                    TempData["SuccessMessage"] =
                        $"\"{activeLoan.Item.Name}\" was returned successfully. " +
                        "No late fine was charged.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "The return transaction could not be completed.");

                return View(model);
            }
        }

        // Records payment of an outstanding fine.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFinePaid(int id)
        {
            Loan? loan = await _context.Loans
                .Include(record => record.Item)
                .Include(record => record.Borrower)
                .FirstOrDefaultAsync(record => record.Id == id);

            if (loan == null)
            {
                return NotFound();
            }

            if (loan.ReturnDate == null)
            {
                TempData["ErrorMessage"] =
                    "An active loan cannot have a completed fine payment.";

                return RedirectToAction(nameof(Index));
            }

            if (loan.FineAmount <= 0)
            {
                TempData["ErrorMessage"] =
                    "This loan does not have a fine.";

                return RedirectToAction(nameof(Index));
            }

            if (loan.FinePaid)
            {
                TempData["ErrorMessage"] =
                    "This fine has already been paid.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                loan.FinePaid = true;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"{loan.Borrower.FullName}'s fine of " +
                    $"{loan.FineAmount:C} was marked as paid.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "The fine payment could not be recorded.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}