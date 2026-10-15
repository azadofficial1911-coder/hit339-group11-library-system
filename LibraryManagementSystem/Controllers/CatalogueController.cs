using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // This portal is available without logging in.
    [AllowAnonymous]
    public class CatalogueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogueController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? itemType,
            string? status,
            string? bookFormat,
            CancellationToken cancellationToken)
        {
            searchTerm = searchTerm?.Trim();

            itemType = itemType switch
            {
                "Book" => "Book",
                "Music" => "Music",
                "Toy" => "Toy",
                _ => string.Empty
            };

            bookFormat = bookFormat switch
            {
                "LargePrint" => "LargePrint",
                "AudioBook" => "AudioBook",
                _ => string.Empty
            };

            IQueryable<Item> query = _context.Items
                .AsNoTracking();

            // Search common item properties.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(item =>
                    item.Name.Contains(searchTerm) ||
                    item.LibraryCode.Contains(searchTerm) ||
                    (
                        item.Description != null &&
                        item.Description.Contains(searchTerm)
                    ));
            }

            // Filter by collection.
            switch (itemType)
            {
                case "Book":
                    query = query.OfType<Book>();
                    break;

                case "Music":
                    query = query.OfType<Music>();
                    break;

                case "Toy":
                    query = query.OfType<Toy>();
                    break;
            }

            // A book-format filter only matches books.
            // Combining it with Music or Toy produces no matches.
            if (!string.IsNullOrEmpty(bookFormat))
            {
                if (itemType == "Music" || itemType == "Toy")
                {
                    query = query.Where(item => false);
                }
                else
                {
                    IQueryable<Book> books = query.OfType<Book>();

                    query = bookFormat == "LargePrint"
                        ? books.Where(book => book.IsLargePrint)
                        : books.Where(book => book.IsAudioBook);
                }
            }

            // Accept only defined ItemStatus values.
            string selectedStatusText = string.Empty;

            if (Enum.TryParse<ItemStatus>(
                    status,
                    true,
                    out var selectedStatus) &&
                Enum.IsDefined(typeof(ItemStatus), selectedStatus))
            {
                query = query.Where(item =>
                    item.Status == selectedStatus);

                selectedStatusText = selectedStatus.ToString();
            }

            List<Item> databaseItems = await query
                .OrderBy(item => item.Name)
                .ThenBy(item => item.LibraryCode)
                .ToListAsync(cancellationToken);

            CatalogueSearchViewModel model = new()
            {
                SearchTerm = searchTerm ?? string.Empty,
                SelectedItemType = itemType,
                SelectedStatus = selectedStatusText,
                SelectedBookFormat = bookFormat,
                Items = databaseItems
                    .Select(CreatePublicItem)
                    .ToList()
            };

            return View(model);
        }

        // Converts database entities into safe public information.
        private static CatalogueItemViewModel CreatePublicItem(
            Item item)
        {
            return item switch
            {
                Book book => new CatalogueItemViewModel
                {
                    Id = book.Id,
                    Name = book.Name,
                    Description =
                        book.Description ?? "No description available.",
                    LibraryCode = book.LibraryCode,
                    Status = book.Status,
                    ItemType = "Book",
                    PrimaryDetail = $"Author: {book.Author}",
                    SecondaryDetail =
                        $"Genre: {book.Genre} | " +
                        $"Year: {book.PublicationYear}",
                    IsLargePrint = book.IsLargePrint,
                    IsAudioBook = book.IsAudioBook
                },

                Music music => new CatalogueItemViewModel
                {
                    Id = music.Id,
                    Name = music.Name,
                    Description =
                        music.Description ?? "No description available.",
                    LibraryCode = music.LibraryCode,
                    Status = music.Status,
                    ItemType = "Music",
                    PrimaryDetail = $"Artist: {music.Artist}",
                    SecondaryDetail =
                        $"Genre: {music.MusicGenre} | " +
                        $"Format: {music.Format} | " +
                        $"Year: {music.ReleaseYear}"
                },

                Toy toy => new CatalogueItemViewModel
                {
                    Id = toy.Id,
                    Name = toy.Name,
                    Description =
                        toy.Description ?? "No description available.",
                    LibraryCode = toy.LibraryCode,
                    Status = toy.Status,
                    ItemType = "Toy",
                    PrimaryDetail =
                        $"Manufacturer: {toy.Manufacturer}",
                    SecondaryDetail =
                        $"Type: {toy.ToyType} | " +
                        $"Recommended age: {toy.RecommendedAge}+"
                },

                _ => new CatalogueItemViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description =
                        item.Description ?? "No description available.",
                    LibraryCode = item.LibraryCode,
                    Status = item.Status,
                    ItemType = "Library Item",
                    PrimaryDetail = string.Empty,
                    SecondaryDetail = string.Empty
                }
            };
        }
    }
}