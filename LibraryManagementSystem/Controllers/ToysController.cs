using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class ToysController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReservationService _reservations;

        public ToysController(
            ApplicationDbContext context,
            IReservationService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        // Displays toys with optional search and status filtering.
        public async Task<IActionResult> Index(
            string? searchTerm,
            ItemStatus? status)
        {
            IQueryable<Toy> query = _context.Toys
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(t =>
                    t.Name.Contains(searchTerm) ||
                    t.LibraryCode.Contains(searchTerm) ||
                    t.ToyType.Contains(searchTerm) ||
                    t.Manufacturer.Contains(searchTerm));
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            ViewData["SearchTerm"] = searchTerm;
            ViewData["SelectedStatus"] = status;

            return View(await query
                .OrderBy(t => t.LibraryCode)
                .ToListAsync());
        }

        // Displays the complete details of one toy.
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var toy = await _context.Toys
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toy is null)
            {
                return NotFound();
            }

            return View(toy);
        }

        // Displays the create form with safe default values.
        public IActionResult Create()
        {
            return View(new Toy
            {
                Status = ItemStatus.Available,
                DateAdded = DateTime.UtcNow
            });
        }

        // Creates a toy after server-side validation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,LibraryCode,Status,ToyType,RecommendedAge,Manufacturer,SafetyInformation")]
            Toy toy)
        {
            toy.LibraryCode = NormaliseLibraryCode(toy.LibraryCode);
            toy.DateAdded = DateTime.UtcNow;

            if (await LibraryCodeExistsAsync(toy.LibraryCode))
            {
                ModelState.AddModelError(
                    nameof(Toy.LibraryCode),
                    "This library code is already being used.");
            }

            if (!ModelState.IsValid)
            {
                return View(toy);
            }

            try
            {
                _context.Toys.Add(toy);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"The toy \"{toy.Name}\" was added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The toy could not be saved. Please check the information and try again.");

                return View(toy);
            }
        }

        // Displays the edit form.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var toy = await _context.Toys.FindAsync(id);

            if (toy is null)
            {
                return NotFound();
            }

            return View(toy);
        }

        // Updates only the permitted properties.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var toyToUpdate = await _context.Toys.FindAsync(id);

            if (toyToUpdate is null)
            {
                return NotFound();
            }

            // Remember the old status so reservations can react to a repair.
            ItemStatus oldStatus = toyToUpdate.Status;

            if (await TryUpdateModelAsync(
                toyToUpdate,
                string.Empty,
                t => t.Name,
                t => t.Description,
                t => t.LibraryCode,
                t => t.Status,
                t => t.ToyType,
                t => t.RecommendedAge,
                t => t.Manufacturer,
                t => t.SafetyInformation))
            {
                toyToUpdate.LibraryCode =
                    NormaliseLibraryCode(toyToUpdate.LibraryCode);

                if (await LibraryCodeExistsAsync(
                    toyToUpdate.LibraryCode,
                    toyToUpdate.Id))
                {
                    ModelState.AddModelError(
                        nameof(Toy.LibraryCode),
                        "This library code is already being used.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        await _context.SaveChangesAsync();

                        await _reservations.OnItemStatusChangedAsync(
                            toyToUpdate.Id,
                            oldStatus,
                            toyToUpdate.Status);

                        TempData["SuccessMessage"] =
                            $"The toy \"{toyToUpdate.Name}\" was updated successfully.";

                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await ToyExistsAsync(id))
                        {
                            return NotFound();
                        }

                        ModelState.AddModelError(
                            string.Empty,
                            "This toy was changed by another user. Please try again.");
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "The changes could not be saved. Please check the information and try again.");
                    }
                }
            }

            return View(toyToUpdate);
        }

        // Displays the delete confirmation page.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var toy = await _context.Toys
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (toy is null)
            {
                return NotFound();
            }

            return View(toy);
        }

        // Deletes a toy unless it is currently borrowed.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var toy = await _context.Toys.FindAsync(id);

            if (toy is null)
            {
                return NotFound();
            }

            if (toy.Status == ItemStatus.Borrowed)
            {
                TempData["ErrorMessage"] =
                    "A borrowed toy cannot be deleted. Return it first.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Toys.Remove(toy);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"The toy \"{toy.Name}\" was deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "The toy could not be deleted because it is connected to other records.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> ToyExistsAsync(int id)
        {
            return await _context.Toys.AnyAsync(t => t.Id == id);
        }

        // Checks every item type because LibraryCode must be globally unique.
        private async Task<bool> LibraryCodeExistsAsync(
            string libraryCode,
            int? excludedItemId = null)
        {
            return await _context.Items.AnyAsync(item =>
                item.LibraryCode == libraryCode &&
                (!excludedItemId.HasValue ||
                 item.Id != excludedItemId.Value));
        }

        private static string NormaliseLibraryCode(string libraryCode)
        {
            return libraryCode.Trim().ToUpperInvariant();
        }
    }
}