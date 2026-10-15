using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = RoleNames.Admin + "," + RoleNames.Reception)]
public class BranchInventoryController(ApplicationDbContext db, TransferService transfers,
    ILogger<BranchInventoryController> logger) : Controller
{
    public async Task<IActionResult> Index(int? branchId) => View(new BranchInventoryViewModel {
        BranchId = branchId,
        Branches = await db.Branches.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(),
        Items = await db.Items.AsNoTracking().Include(i => i.Branch)
            .Where(i => !branchId.HasValue || i.BranchId == branchId)
            .OrderBy(i => i.Name).ToListAsync(),
        Transfers = await db.ItemTransfers.AsNoTracking().Include(t => t.Item)
            .Include(t => t.SourceBranch).Include(t => t.DestinationBranch)
            .Where(t => !branchId.HasValue || t.SourceBranchId == branchId || t.DestinationBranchId == branchId)
            .OrderByDescending(t => t.DispatchedAt).Take(100).ToListAsync()
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = RoleNames.Admin)]
    public Task<IActionResult> Assign(int itemId, int branchId) => Execute(() => transfers.AssignAsync(itemId, branchId));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Dispatch(int itemId, int destinationId) => Execute(() =>
        transfers.DispatchAsync(itemId, destinationId, User.Identity?.Name ?? "Staff"));
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Receive(int id, int receivingBranchId) => Execute(() =>
        transfers.ReceiveAsync(id, receivingBranchId, User.Identity?.Name ?? "Staff"));
    private async Task<IActionResult> Execute(Func<Task<TransferResult>> action) {
        try {
            var result = await action();
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        } catch (DbUpdateException ex) {
            logger.LogWarning(ex, "Concurrent or invalid stock change");
            TempData["ErrorMessage"] = "The stock changed or could not be saved. Reload and try again.";
        }
        return RedirectToAction(nameof(Index));
    }
}
