using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class BranchesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BranchesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var branches = await _context.Branches
                .AsNoTracking()
                .OrderBy(branch => branch.Name)
                .ToListAsync();

            return View(branches);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Branch());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Code,Name,Address,PhoneNumber,OpeningHours,AccessibilityInformation,IsActive")]
            Branch branch)
        {
            branch.Code = (branch.Code ?? string.Empty).Trim().ToUpperInvariant();
            branch.Name = (branch.Name ?? string.Empty).Trim();
            branch.Address = (branch.Address ?? string.Empty).Trim();

            if (await _context.Branches.AnyAsync(b => b.Code == branch.Code))
            {
                ModelState.AddModelError(
                    nameof(Branch.Code),
                    "This branch code is already in use.");
            }

            if (!ModelState.IsValid)
            {
                return View(branch);
            }

            try
            {
                _context.Branches.Add(branch);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Branch added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "The branch could not be saved. Please try again.");
                return View(branch);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            return branch == null ? NotFound() : View(branch);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Code,Name,Address,PhoneNumber,OpeningHours,AccessibilityInformation,IsActive")] Branch submitted)
        {
            if (id != submitted.Id) return NotFound();
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            submitted.Code = (submitted.Code ?? "").Trim().ToUpperInvariant();
            submitted.Name = (submitted.Name ?? "").Trim();
            submitted.Address = (submitted.Address ?? "").Trim();
            if (await _context.Branches.AnyAsync(b => b.Id != id && b.Code == submitted.Code))
                ModelState.AddModelError(nameof(Branch.Code), "This branch code is already in use.");
            if (!submitted.IsActive && (await _context.Items.AnyAsync(i => i.BranchId == id) ||
                await _context.ReceptionDesks.AnyAsync(d => d.BranchId == id && d.IsActive) ||
                await _context.ItemTransfers.AnyAsync(t => t.Status == TransferStatus.Dispatched &&
                    (t.SourceBranchId == id || t.DestinationBranchId == id))))
                ModelState.AddModelError(nameof(Branch.IsActive), "Move stock, deactivate desks and finish transfers before deactivating this branch.");
            if (!ModelState.IsValid) return View(submitted);
            branch.Code = submitted.Code;
            branch.Name = submitted.Name;
            branch.Address = submitted.Address;
            branch.PhoneNumber = submitted.PhoneNumber;
            branch.OpeningHours = submitted.OpeningHours;
            branch.AccessibilityInformation = submitted.AccessibilityInformation;
            branch.IsActive = submitted.IsActive;
            try {
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = "Branch updated.";
                return RedirectToAction(nameof(Index));
            } catch (DbUpdateException) {
                ModelState.AddModelError("", "The branch could not be saved. Reload and try again.");
                return View(submitted);
            }
        }
    }
}
