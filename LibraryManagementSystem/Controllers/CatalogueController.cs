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

        public async Task<IActionResult> Index(
            string? searchTerm,
            string? itemType,
            string? status)
        {
            IQueryable<Item> query = _context.Items
                .AsNoTracking().Include(item => item.Branch);

            // Search using the common properties inherited from Item.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(item =>
                    item.Name.Contains(searchTerm) ||
                    item.LibraryCode.Contains(searchTerm) ||
                    (
                        item.Description != null &&
                        item.Description.Contains(searchTerm)
                    ));
            }

            // Filter using the TPH inherited item types.
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

            // Convert the submitted text into the ItemStatus enum safely.
            if (Enum.TryParse(
                status,
                true,
                out ItemStatus selectedStatus))
            {
                query = query.Where(item =>
                    item.Status == selectedStatus);
            }

            List<Item> databaseItems = await query
                .OrderBy(item => item.Name)
                .ThenBy(item => item.LibraryCode)
                .ToListAsync();

            CatalogueSearchViewModel model = new()
            {
                SearchTerm = searchTerm ?? string.Empty,
                SelectedItemType = itemType ?? string.Empty,
                SelectedStatus = status ?? string.Empty,

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
                    BranchName = book.Branch?.Name,
                    ItemType = "Book",
                    PrimaryDetail = $"Author: {book.Author}",
                    SecondaryDetail =
                        $"Genre: {book.Genre} | Year: {book.PublicationYear}"
                },

                Music music => new CatalogueItemViewModel
                {
                    Id = music.Id,
                    Name = music.Name,
                    Description =
                        music.Description ?? "No description available.",
                    LibraryCode = music.LibraryCode,
                    Status = music.Status,
                    BranchName = music.Branch?.Name,
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
                    BranchName = toy.Branch?.Name,
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
