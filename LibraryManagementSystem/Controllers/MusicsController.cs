using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class MusicsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReservationService _reservations;

        public MusicsController(
            ApplicationDbContext context,
            IReservationService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        // Displays music items and supports searching and filtering.
        public async Task<IActionResult> Index(
            string? searchTerm,
            ItemStatus? status)
        {
            IQueryable<Music> query = _context.Music
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(m =>
                    m.Name.Contains(searchTerm) ||
                    m.LibraryCode.Contains(searchTerm) ||
                    m.Artist.Contains(searchTerm) ||
                    m.MusicGenre.Contains(searchTerm) ||
                    m.Format.Contains(searchTerm));
            }

            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            ViewData["SearchTerm"] = searchTerm;
            ViewData["SelectedStatus"] = status;

            return View(await query
                .OrderBy(m => m.LibraryCode)
                .ToListAsync());
        }

        // Displays one music item's complete details.
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var music = await _context.Music
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (music is null)
            {
                return NotFound();
            }

            return View(music);
        }

        // Displays the create form.
        public IActionResult Create()
        {
            return View(new Music
            {
                Status = ItemStatus.Available,
                DateAdded = DateTime.UtcNow
            });
        }

        // Creates a music item after server-side validation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,Description,LibraryCode,Status,Artist,MusicGenre,ReleaseYear,Format")]
            Music music)
        {
            music.LibraryCode = NormaliseLibraryCode(music.LibraryCode);
            music.DateAdded = DateTime.UtcNow;

            if (await LibraryCodeExistsAsync(music.LibraryCode))
            {
                ModelState.AddModelError(
                    nameof(Music.LibraryCode),
                    "This library code is already being used.");
            }

            if (!ModelState.IsValid)
            {
                return View(music);
            }

            try
            {
                _context.Music.Add(music);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"The music item \"{music.Name}\" was added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The music item could not be saved. Please check the information and try again.");

                return View(music);
            }
        }

        // Displays the edit form.
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var music = await _context.Music.FindAsync(id);

            if (music is null)
            {
                return NotFound();
            }

            return View(music);
        }

        // Updates only the permitted properties.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var musicToUpdate = await _context.Music.FindAsync(id);

            if (musicToUpdate is null)
            {
                return NotFound();
            }

            // Remember the old status so reservations can react to a repair.
            ItemStatus oldStatus = musicToUpdate.Status;

            if (await TryUpdateModelAsync(
                musicToUpdate,
                string.Empty,
                m => m.Name,
                m => m.Description,
                m => m.LibraryCode,
                m => m.Status,
                m => m.Artist,
                m => m.MusicGenre,
                m => m.ReleaseYear,
                m => m.Format))
            {
                musicToUpdate.LibraryCode =
                    NormaliseLibraryCode(musicToUpdate.LibraryCode);

                if (await LibraryCodeExistsAsync(
                    musicToUpdate.LibraryCode,
                    musicToUpdate.Id))
                {
                    ModelState.AddModelError(
                        nameof(Music.LibraryCode),
                        "This library code is already being used.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        await _context.SaveChangesAsync();

                        await _reservations.OnItemStatusChangedAsync(
                            musicToUpdate.Id,
                            oldStatus,
                            musicToUpdate.Status);

                        TempData["SuccessMessage"] =
                            $"The music item \"{musicToUpdate.Name}\" was updated successfully.";

                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await MusicExistsAsync(id))
                        {
                            return NotFound();
                        }

                        ModelState.AddModelError(
                            string.Empty,
                            "This music item was changed by another user. Please try again.");
                    }
                    catch (DbUpdateException)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "The changes could not be saved. Please check the information and try again.");
                    }
                }
            }

            return View(musicToUpdate);
        }

        // Displays the delete confirmation page.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var music = await _context.Music
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (music is null)
            {
                return NotFound();
            }

            return View(music);
        }

        // Deletes an item unless it is currently borrowed.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var music = await _context.Music.FindAsync(id);

            if (music is null)
            {
                return NotFound();
            }

            if (music.Status == ItemStatus.Borrowed)
            {
                TempData["ErrorMessage"] =
                    "A borrowed music item cannot be deleted. Return it first.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Music.Remove(music);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"The music item \"{music.Name}\" was deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "The music item could not be deleted because it is connected to other records.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> MusicExistsAsync(int id)
        {
            return await _context.Music.AnyAsync(m => m.Id == id);
        }

        private async Task<bool> LibraryCodeExistsAsync(
            string libraryCode,
            int? excludedItemId = null)
        {
            return await _context.Items.AnyAsync(item =>
                item.LibraryCode == libraryCode &&
                (!excludedItemId.HasValue || item.Id != excludedItemId.Value));
        }

        private static string NormaliseLibraryCode(string libraryCode)
        {
            return libraryCode.Trim().ToUpperInvariant();
        }
    }
}