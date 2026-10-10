using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Reception)]
    public class BorrowersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Displays borrowers with search and account-status filtering.
        public async Task<IActionResult> Index(
            string? searchTerm,
            bool? isActive)
        {
            IQueryable<Borrower> query = _context.Borrowers
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(b =>
                    b.BorrowerCode.Contains(searchTerm) ||
                    b.FullName.Contains(searchTerm) ||
                    b.Email.Contains(searchTerm) ||
                    b.PhoneNumber.Contains(searchTerm));
            }

            if (isActive.HasValue)
            {
                query = query.Where(b =>
                    b.IsActive == isActive.Value);
            }

            ViewData["SearchTerm"] = searchTerm;
            ViewData["IsActive"] = isActive;

            return View(await query
                .OrderBy(b => b.FullName)
                .ToListAsync());
        }

        // Displays one borrower's complete details.
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower is null)
            {
                return NotFound();
            }

            return View(borrower);
        }

        // Displays the create form with safe defaults.
        public IActionResult Create()
        {
            return View(new Borrower
            {
                RegistrationDate = DateTime.UtcNow,
                IsActive = true
            });
        }

        // Creates a borrower after server-side validation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("BorrowerCode,FullName,Email,PhoneNumber,Address,DateOfBirth,IsActive")]
            Borrower borrower)
        {
            NormaliseBorrowerDetails(borrower);
            borrower.RegistrationDate = DateTime.UtcNow;

            await AddDuplicateErrorsAsync(borrower);

            if (!ModelState.IsValid)
            {
                return View(borrower);
            }

            try
            {
                _context.Borrowers.Add(borrower);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Borrower \"{borrower.FullName}\" was added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The borrower could not be saved. The borrower code or email may already exist.");

                return View(borrower);
            }
        }

        // Displays the edit form.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var borrower = await _context.Borrowers.FindAsync(id);

            if (borrower is null)
            {
                return NotFound();
            }

            return View(borrower);
        }

        // Updates only properties that Reception staff may change.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var borrowerToUpdate =
                await _context.Borrowers.FindAsync(id);

            if (borrowerToUpdate is null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync(
                borrowerToUpdate,
                string.Empty,
                b => b.BorrowerCode,
                b => b.FullName,
                b => b.Email,
                b => b.PhoneNumber,
                b => b.Address,
                b => b.DateOfBirth,
                b => b.IsActive))
            {
                NormaliseBorrowerDetails(borrowerToUpdate);

                await AddDuplicateErrorsAsync(
                    borrowerToUpdate,
                    borrowerToUpdate.Id);

                if (ModelState.IsValid)
                {
                    try
                    {
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] =
                            $"Borrower \"{borrowerToUpdate.FullName}\" was updated successfully.";

                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await BorrowerExistsAsync(id))
                        {
                            return NotFound();
                        }

                        ModelState.AddModelError(
                            string.Empty,
                            "This borrower was changed by another user. Please try again.");
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "The changes could not be saved. The borrower code or email may already exist.");
                    }
                }
            }

            return View(borrowerToUpdate);
        }

        // Displays the delete confirmation page.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var borrower = await _context.Borrowers
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (borrower is null)
            {
                return NotFound();
            }

            return View(borrower);
        }

        // Deletes the selected borrower.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var borrower =
                await _context.Borrowers.FindAsync(id);

            if (borrower is null)
            {
                return NotFound();
            }

            try
            {
                _context.Borrowers.Remove(borrower);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Borrower \"{borrower.FullName}\" was deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This borrower cannot be deleted because they are connected to borrowing records.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task AddDuplicateErrorsAsync(
            Borrower borrower,
            int? excludedBorrowerId = null)
        {
            bool codeExists = await _context.Borrowers.AnyAsync(b =>
                b.BorrowerCode == borrower.BorrowerCode &&
                (!excludedBorrowerId.HasValue ||
                 b.Id != excludedBorrowerId.Value));

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(Borrower.BorrowerCode),
                    "This borrower code is already being used.");
            }

            bool emailExists = await _context.Borrowers.AnyAsync(b =>
                b.Email == borrower.Email &&
                (!excludedBorrowerId.HasValue ||
                 b.Id != excludedBorrowerId.Value));

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(Borrower.Email),
                    "This email address is already being used.");
            }
        }

        private async Task<bool> BorrowerExistsAsync(int id)
        {
            return await _context.Borrowers
                .AnyAsync(b => b.Id == id);
        }

        private static void NormaliseBorrowerDetails(
            Borrower borrower)
        {
            borrower.BorrowerCode = borrower.BorrowerCode
                .Trim()
                .ToUpperInvariant();

            borrower.FullName = borrower.FullName.Trim();

            borrower.Email = borrower.Email
                .Trim()
                .ToLowerInvariant();

            borrower.PhoneNumber = borrower.PhoneNumber.Trim();

            borrower.Address = borrower.Address.Trim();
        }
    }
}