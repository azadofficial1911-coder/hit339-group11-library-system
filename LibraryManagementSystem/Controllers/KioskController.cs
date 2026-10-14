using System.Data;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public class KioskController : Controller
    {
        private const string BorrowerKey = "Kiosk.BorrowerId";
        private const string ItemKey = "Kiosk.PendingItem";
        private const string TokenKey = "Kiosk.ConfirmationToken";

        private readonly ApplicationDbContext _context;
        private readonly IReservationService _reservations;
        private readonly INotificationService _notifications;
        private readonly ILogger<KioskController> _logger;

        public KioskController(
            ApplicationDbContext context,
            IReservationService reservations,
            INotificationService notifications,
            ILogger<KioskController> logger)
        {
            _context = context;
            _reservations = reservations;
            _notifications = notifications;
            _logger = logger;
        }

        private void ClearPendingItem()
        {
            HttpContext.Session.Remove(ItemKey);
            HttpContext.Session.Remove(TokenKey);
        }

        private void ClearKiosk()
        {
            HttpContext.Session.Remove(BorrowerKey);
            ClearPendingItem();
        }

        private async Task<Borrower?> CurrentBorrowerAsync()
        {
            int? id = HttpContext.Session.GetInt32(BorrowerKey);

            if (!id.HasValue)
                return null;

            return await _context.Borrowers
                .AsNoTracking()
                .SingleOrDefaultAsync(b =>
                    b.Id == id.Value && b.IsActive);
        }

        private IActionResult SessionEnded()
        {
            ClearKiosk();

            TempData["KioskMessage"] =
                "Your session has ended. Please scan your card again.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Index()
        {
            ClearKiosk();
            return View(new KioskStartViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(
            KioskStartViewModel model)
        {
            ClearKiosk();

            model.BorrowerCode =
                (model.BorrowerCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            if (!ModelState.IsValid)
                return View("Index", model);

            var borrower = await _context.Borrowers
                .AsNoTracking()
                .Where(b =>
                    b.BorrowerCode == model.BorrowerCode &&
                    b.IsActive)
                .Select(b => new { b.Id })
                .SingleOrDefaultAsync();

            if (borrower == null)
            {
                ModelState.AddModelError(
                    nameof(model.BorrowerCode),
                    "We could not find an active account with this code. " +
                    "Check your card or ask the service desk.");

                return View("Index", model);
            }

            HttpContext.Session.SetInt32(BorrowerKey, borrower.Id);

            return RedirectToAction(nameof(Account));
        }

        [HttpGet]
        public async Task<IActionResult> Account()
        {
            var borrower = await CurrentBorrowerAsync();

            if (borrower == null)
                return SessionEnded();

            ClearPendingItem();

            var loans = await _context.Loans
                .AsNoTracking()
                .Where(l =>
                    l.BorrowerId == borrower.Id &&
                    l.ReturnDate == null)
                .OrderBy(l => l.DueDate)
                .Select(l => new KioskLoanViewModel
                {
                    ItemName = l.Item.Name,
                    LibraryCode = l.Item.LibraryCode,
                    DueDate = l.DueDate
                })
                .ToListAsync();

            decimal fines = await _context.Loans
                .AsNoTracking()
                .Where(l =>
                    l.BorrowerId == borrower.Id &&
                    l.ReturnDate != null &&
                    !l.FinePaid)
                .SumAsync(l => (decimal?)l.FineAmount) ?? 0m;

            return View(new KioskAccountViewModel
            {
                FirstName = borrower.FullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? "Reader",
                ActiveLoans = loans,
                OutstandingFines = fines
            });
        }

        [HttpGet]
        public async Task<IActionResult> Borrow()
        {
            if (await CurrentBorrowerAsync() == null)
                return SessionEnded();

            ClearPendingItem();

            return View(new KioskBorrowViewModel());
        }

        // Look up an item before showing the confirmation screen.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(
            KioskBorrowViewModel model)
        {
            var borrower = await CurrentBorrowerAsync();

            if (borrower == null)
                return SessionEnded();

            ClearPendingItem();

            // Only accept the entered code, never posted display data.
            string code = (model.LibraryCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            ModelState.Clear();
            model = new KioskBorrowViewModel { LibraryCode = code };

            if (!TryValidateModel(model))
                return View("Borrow", model);

            var item = await _context.Items
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.LibraryCode == code);

            if (item == null)
            {
                ModelState.AddModelError(
                    nameof(model.LibraryCode),
                    "No item was found with this code.");

                return View("Borrow", model);
            }

            string? error = await BorrowErrorAsync(item, borrower.Id);

            if (error != null)
            {
                ModelState.AddModelError(
                    nameof(model.LibraryCode), error);

                return View("Borrow", model);
            }

            string token = Guid.NewGuid().ToString("N");

            HttpContext.Session.SetInt32(ItemKey, item.Id);
            HttpContext.Session.SetString(TokenKey, token);

            model.ItemName = item.Name;
            model.DueDate = DateTime.Today.AddDays(14);
            model.ConfirmationToken = token;

            return View("Borrow", model);
        }

        private async Task<string?> BorrowErrorAsync(
            Item item,
            int borrowerId)
        {
            if (item.Status != ItemStatus.Available &&
                item.Status != ItemStatus.Reserved)
            {
                return $"This item cannot be borrowed. " +
                       $"Its status is {item.Status}.";
            }

            var check = await _reservations.CheckBorrowAllowedAsync(
                item.Id, borrowerId);

            if (!check.Allowed)
            {
                return check.Reason ??
                    "This item is being held for another borrower.";
            }

            if (await _context.Loans.AnyAsync(l =>
                l.ItemId == item.Id && l.ReturnDate == null))
            {
                return "This item already has an active loan.";
            }

            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            string? confirmationToken)
        {
            var borrower = await CurrentBorrowerAsync();

            if (borrower == null)
                return SessionEnded();

            int? itemId = HttpContext.Session.GetInt32(ItemKey);
            string? expectedToken =
                HttpContext.Session.GetString(TokenKey);

            if (!itemId.HasValue ||
                string.IsNullOrEmpty(expectedToken) ||
                expectedToken != confirmationToken)
            {
                TempData["KioskMessage"] =
                    "Please scan the item again before confirming.";

                return RedirectToAction(nameof(Borrow));
            }

            ClearPendingItem();

            int savedLoanId;
            string itemName;

            // Keep availability checks and loan creation together.
            await using (var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable))
            {
                try
                {
                    var item = await _context.Items
                        .SingleOrDefaultAsync(i => i.Id == itemId.Value);

                    if (item == null)
                    {
                        TempData["KioskMessage"] =
                            "This item is no longer available.";

                        return RedirectToAction(nameof(Borrow));
                    }

                    string? error =
                        await BorrowErrorAsync(item, borrower.Id);

                    if (error != null)
                    {
                        TempData["KioskMessage"] = error;
                        return RedirectToAction(nameof(Borrow));
                    }

                    var loan = new Loan
                    {
                        ItemId = item.Id,
                        BorrowerId = borrower.Id,
                        BorrowDate = DateTime.Today,
                        DueDate = DateTime.Today.AddDays(14),
                        FineAmount = 0m,
                        FinePaid = false,
                        IssuedBy = "Self-service kiosk"
                    };

                    item.Status = ItemStatus.Borrowed;

                    await _reservations.MarkCollectedAsync(
                        item.Id, borrower.Id);

                    _context.Loans.Add(loan);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    savedLoanId = loan.Id;
                    itemName = item.Name;
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();

                    _logger.LogWarning(
                        ex,
                        "Kiosk borrowing failed for item {ItemId}.",
                        itemId);

                    TempData["KioskMessage"] =
                        "We could not complete borrowing. Check your " +
                        "account before trying again, or ask the service desk.";

                    return RedirectToAction(nameof(Account));
                }
            }

            // A notification failure must not undo a completed loan.
            try
            {
                await _notifications.NotifyBorrowedAsync(savedLoanId);

                TempData["KioskMessage"] =
                    $"You borrowed \"{itemName}\". " +
                    $"Return it by {DateTime.Today.AddDays(14):dd MMM yyyy}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Loan {LoanId} was saved but notification failed.",
                    savedLoanId);

                TempData["KioskMessage"] =
                    $"You borrowed \"{itemName}\" successfully. " +
                    "Your confirmation notification could not be logged. " +
                    "Check the due date below.";
            }

            return RedirectToAction(nameof(Account));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EndSession()
        {
            ClearKiosk();

            TempData["KioskMessage"] =
                "You have signed out. Thank you for visiting.";

            return RedirectToAction(nameof(Index));
        }
    }
}