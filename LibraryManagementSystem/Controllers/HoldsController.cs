using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    // Reservation queue (R5). Reception manages holds; Managers can view them.
    [Authorize(Roles = RoleNames.Reception + "," + RoleNames.Manager)]
    public class HoldsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReservationService _reservations;

        public HoldsController(
            ApplicationDbContext context,
            IReservationService reservations)
        {
            _context = context;
            _reservations = reservations;
        }

        // Lists holds with each borrower's queue position.
        public async Task<IActionResult> Index(
            string? searchTerm,
            string? status)
        {
            status = string.IsNullOrWhiteSpace(status) ? "Active" : status;

            IQueryable<Reservation> query = _context.Reservations
                .AsNoTracking()
                .Include(hold => hold.Item)
                .Include(hold => hold.Borrower);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();

                query = query.Where(hold =>
                    hold.Item.LibraryCode.Contains(searchTerm) ||
                    hold.Item.Name.Contains(searchTerm) ||
                    hold.Borrower.BorrowerCode.Contains(searchTerm) ||
                    hold.Borrower.FullName.Contains(searchTerm));
            }

            if (status == "Active")
            {
                query = query.Where(hold =>
                    hold.Status == ReservationStatus.Waiting ||
                    hold.Status == ReservationStatus.Ready);
            }
            else if (Enum.TryParse(status, true, out ReservationStatus selected))
            {
                query = query.Where(hold => hold.Status == selected);
            }
            else
            {
                status = "All";
            }

            List<Reservation> holds = await query
                .OrderBy(hold => hold.ItemId)
                .ThenBy(hold => hold.CreatedAt)
                .ThenBy(hold => hold.Id)
                .ToListAsync();

            // Queue position among ALL active holds on the same item, so a filtered
            // list still shows the true position.
            var activeOrder = await _context.Reservations
                .AsNoTracking()
                .Where(hold =>
                    hold.Status == ReservationStatus.Waiting ||
                    hold.Status == ReservationStatus.Ready)
                .OrderBy(hold => hold.CreatedAt)
                .ThenBy(hold => hold.Id)
                .Select(hold => new { hold.Id, hold.ItemId })
                .ToListAsync();

            Dictionary<int, int> positions = new();

            foreach (var group in activeOrder.GroupBy(hold => hold.ItemId))
            {
                int position = 1;

                foreach (var hold in group)
                {
                    positions[hold.Id] = position++;
                }
            }

            HoldsIndexViewModel model = new()
            {
                SearchTerm = searchTerm ?? string.Empty,
                SelectedStatus = status,
                Holds = holds.Select(hold => new HoldRowViewModel
                {
                    ReservationId = hold.Id,
                    ItemName = hold.Item.Name,
                    LibraryCode = hold.Item.LibraryCode,
                    BorrowerName = hold.Borrower.FullName,
                    BorrowerCode = hold.Borrower.BorrowerCode,
                    Status = hold.Status,
                    Position = positions.TryGetValue(hold.Id, out int p) ? p : null,
                    CreatedAt = hold.CreatedAt,
                    ExpiresAt = hold.ExpiresAt
                }).ToList()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Reception)]
        public IActionResult Place()
        {
            return View(new PlaceHoldViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Reception)]
        public async Task<IActionResult> Place(PlaceHoldViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HoldResult result = await _reservations.PlaceHoldAsync(
                model.LibraryCode,
                model.BorrowerCode,
                User.Identity?.Name ?? "Unknown Reception User");

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleNames.Reception)]
        public async Task<IActionResult> Cancel(int id)
        {
            HoldResult result = await _reservations.CancelHoldAsync(id);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
