using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class BooksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReservationService _reservations;

        public BooksController(
            ApplicationDbContext context,
            IReservationService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        // GET: Books
        public async Task<IActionResult> Index(
            string? searchString,
            ItemStatus? status
        )
        {
            IQueryable<Book> books = _context.Books
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                books = books.Where(book =>
                    book.Name.Contains(searchString) ||
                    book.LibraryCode.Contains(searchString) ||
                    book.Author.Contains(searchString) ||
                    book.Genre.Contains(searchString));
            }

            if (status.HasValue)
            {
                books = books.Where(book => book.Status == status.Value);
            }

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStatus"] = status;

            return View(
                await books
                    .OrderBy(book => book.Name)
                    .ToListAsync()
            );
        }

        // GET: Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Book? book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(book => book.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // GET: Books/Create
        public IActionResult Create()
        {
            return View(
                new Book
                {
                    DateAdded = DateTime.Today,
                    Status = ItemStatus.Available
                }
            );
        }

        // POST: Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind(
                "Name,Description,LibraryCode,Status," +
                "Author,Genre,Isbn,PublicationYear"
            )]
            Book book
        )
        {
            book.LibraryCode = book.LibraryCode.Trim().ToUpperInvariant();
            book.DateAdded = DateTime.Today;

            bool libraryCodeExists = await _context.Items
                .AnyAsync(item => item.LibraryCode == book.LibraryCode);

            if (libraryCodeExists)
            {
                ModelState.AddModelError(
                    nameof(Book.LibraryCode),
                    "This library code is already being used."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(book);
            }

            try
            {
                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"The book \"{book.Name}\" was added successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The book could not be saved. Please check the information and try again."
                );

                return View(book);
            }
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Book? book = await _context.Books.FindAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind(
                "Id,Name,Description,LibraryCode,Status," +
                "Author,Genre,Isbn,PublicationYear"
            )]
            Book submittedBook
        )
        {
            if (id != submittedBook.Id)
            {
                return NotFound();
            }

            Book? existingBook = await _context.Books
                .FirstOrDefaultAsync(book => book.Id == id);

            if (existingBook == null)
            {
                return NotFound();
            }

            submittedBook.LibraryCode =
                submittedBook.LibraryCode.Trim().ToUpperInvariant();

            bool libraryCodeExists = await _context.Items
                .AnyAsync(item =>
                    item.LibraryCode == submittedBook.LibraryCode &&
                    item.Id != submittedBook.Id);

            if (libraryCodeExists)
            {
                ModelState.AddModelError(
                    nameof(Book.LibraryCode),
                    "This library code is already being used."
                );
            }

            if (!ModelState.IsValid)
            {
                submittedBook.DateAdded = existingBook.DateAdded;
                return View(submittedBook);
            }

            existingBook.Name = submittedBook.Name;
            existingBook.Description = submittedBook.Description;
            existingBook.LibraryCode = submittedBook.LibraryCode;
            // Remember the old status so reservations can react to a repair.
            ItemStatus oldStatus = existingBook.Status;

            existingBook.Status = submittedBook.Status;
            existingBook.Author = submittedBook.Author;
            existingBook.Genre = submittedBook.Genre;
            existingBook.Isbn = submittedBook.Isbn;
            existingBook.PublicationYear = submittedBook.PublicationYear;

            try
            {
                await _context.SaveChangesAsync();

                await _reservations.OnItemStatusChangedAsync(
                    existingBook.Id,
                    oldStatus,
                    existingBook.Status);

                TempData["SuccessMessage"] =
                    $"The book \"{existingBook.Name}\" was updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await BookExistsAsync(id))
                {
                    return NotFound();
                }

                ModelState.AddModelError(
                    string.Empty,
                    "Another user changed this book. Please reload the page and try again."
                );

                submittedBook.DateAdded = existingBook.DateAdded;
                return View(submittedBook);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The book could not be updated. Please check the information and try again."
                );

                submittedBook.DateAdded = existingBook.DateAdded;
                return View(submittedBook);
            }
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Book? book = await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(book => book.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            if (book.Status == ItemStatus.Borrowed)
            {
                TempData["ErrorMessage"] =
                    "A borrowed book cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            return View(book);
        }

        // POST: Books/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Book? book = await _context.Books.FindAsync(id);

            if (book == null)
            {
                return NotFound();
            }

            if (book.Status == ItemStatus.Borrowed)
            {
                TempData["ErrorMessage"] =
                    "A borrowed book cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                string bookName = book.Name;

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"The book \"{bookName}\" was deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "The book could not be deleted because it is connected to another record.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> BookExistsAsync(int id)
        {
            return await _context.Books
                .AnyAsync(book => book.Id == id);
        }
    }
}