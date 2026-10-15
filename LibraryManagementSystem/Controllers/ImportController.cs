using System.Security.Claims;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class ImportController : Controller
    {
        private const long MaxFileSize = 1024 * 1024;

        private readonly ICsvImportService _importService;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _context;

        public ImportController(
            ICsvImportService importService,
            IMemoryCache cache,
            ApplicationDbContext context)
        {
            _importService = importService;
            _cache = cache;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ImportPreviewViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> Preview(
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            var preview = new ImportPreviewViewModel();

            if (file == null || file.Length == 0)
            {
                preview.Errors.Add("Please choose a CSV file.");
                return View("Index", preview);
            }

            if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".csv",
                StringComparison.OrdinalIgnoreCase))
            {
                preview.Errors.Add("Only CSV files are accepted.");
                return View("Index", preview);
            }

            if (file.Length > MaxFileSize)
            {
                preview.Errors.Add("The file must be 1 MB or smaller.");
                return View("Index", preview);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);

            var bytes = buffer.ToArray();

            using var csvStream = new MemoryStream(bytes);

            preview = await _importService.PreviewAsync(
                csvStream,
                file.FileName,
                cancellationToken);

            if (preview.CanImport)
            {
                preview.PreviewToken = Guid.NewGuid().ToString("N");

                // Store the original CSV for validation at confirmation.
                var cachedImport = new CachedImport(
                    bytes,
                    preview.FileName);

                _cache.Set(
                    $"csv-import:{userId}:{preview.PreviewToken}",
                    cachedImport,
                    TimeSpan.FromMinutes(15));
            }

            return View("Preview", preview);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(
            string? previewToken,
            CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (!Guid.TryParseExact(previewToken, "N", out _))
            {
                var error = new ImportPreviewViewModel();
                error.Errors.Add(
                    "Invalid preview. Please upload the CSV again.");

                return View("Index", error);
            }

            string cacheKey = $"csv-import:{userId}:{previewToken}";

            if (!_cache.TryGetValue<CachedImport>(cacheKey, out var cached) ||
                cached == null)
            {
                var error = new ImportPreviewViewModel();
                error.Errors.Add(
                    "This preview has expired or was already used. " +
                    "Please upload the CSV again.");

                return View("Index", error);
            }

            await cached.Gate.WaitAsync(cancellationToken);

            try
            {
                // Prevent the same preview being confirmed twice.
                if (!_cache.TryGetValue<CachedImport>(
                        cacheKey, out var current) ||
                    !ReferenceEquals(current, cached))
                {
                    var error = new ImportPreviewViewModel();
                    error.Errors.Add(
                        "This preview has expired or was already used. " +
                        "Please upload the CSV again.");

                    return View("Index", error);
                }

                using var stream = new MemoryStream(cached.Bytes);

                // Recheck the original CSV against current database data.
                var preview = await _importService.PreviewAsync(
                    stream,
                    cached.FileName,
                    cancellationToken);

                _cache.Remove(cacheKey);

                if (!preview.CanImport)
                {
                    preview.Errors.Add(
                        "Validation failed. No items were imported. " +
                        "Please fix the errors and upload again.");

                    return View("Preview", preview);
                }

                var items = new List<Item>();

                foreach (var row in preview.Rows)
                {
                    var item = ImportItemFactory.Create(row);

                    if (item != null && row.IsValid)
                    {
                        items.Add(item);
                    }
                }

                if (items.Count != preview.Rows.Count || !preview.CanImport)
                {
                    preview.Errors.Add(
                        "Some items are invalid. No items were imported.");

                    return View("Preview", preview);
                }

                try
                {
                    _context.Items.AddRange(items);

                    // Save the entire batch in one database transaction.
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    _context.ChangeTracker.Clear();

                    preview.Errors.Add(
                        "The import could not be saved. " +
                        "No items were imported. " +
                        "A library code may already exist. " +
                        "Please upload again to recheck.");

                    return View("Preview", preview);
                }

                TempData["ImportSuccess"] =
                    $"Successfully imported {items.Count} items.";

                return RedirectToAction(nameof(Index));
            }
            finally
            {
                cached.Gate.Release();
            }
        }

        public sealed record CachedImport(
            byte[] Bytes,
            string FileName)
        {
            public SemaphoreSlim Gate { get; } = new(1, 1);
        }
    }
}