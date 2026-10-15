using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagementSystem.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class ReceptionDesksController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.ReceptionDesks.AsNoTracking()
        .Include(d => d.Branch).OrderBy(d => d.Code).ToListAsync());
    private async Task Choices() => ViewBag.Branches = new SelectList(
        await db.Branches.Where(b => b.IsActive).OrderBy(b => b.Name).ToListAsync(), "Id", "Name");
    [HttpGet]
    public async Task<IActionResult> Create() { await Choices(); return View("Edit", new ReceptionDesk()); }
    [HttpGet]
    public async Task<IActionResult> Edit(int id) {
        var desk = await db.ReceptionDesks.FindAsync(id);
        if (desk == null) return NotFound();
        await Choices(); return View(desk);
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([Bind("Id,Code,Name,BranchId,IsActive")] ReceptionDesk submitted) {
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var desk = submitted.Id == 0 ? new ReceptionDesk() : await db.ReceptionDesks.FindAsync(submitted.Id);
        if (desk == null) return NotFound();
        submitted.Code = (submitted.Code ?? "").Trim().ToUpperInvariant();
        submitted.Name = (submitted.Name ?? "").Trim();
        if (await db.ReceptionDesks.AnyAsync(d => d.Code == submitted.Code && d.Id != submitted.Id))
            ModelState.AddModelError(nameof(ReceptionDesk.Code), "This desk code is already in use.");
        if (!await db.Branches.AnyAsync(b => b.Id == submitted.BranchId && b.IsActive))
            ModelState.AddModelError(nameof(ReceptionDesk.BranchId), "Select an active branch.");
        if (!ModelState.IsValid) { await Choices(); return View("Edit", submitted); }
        desk.Code = submitted.Code; desk.Name = submitted.Name;
        desk.BranchId = submitted.BranchId; desk.IsActive = submitted.IsActive;
        if (desk.Id == 0) db.ReceptionDesks.Add(desk);
        try {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            TempData["SuccessMessage"] = "Reception desk saved.";
            return RedirectToAction(nameof(Index));
        } catch (DbUpdateException) {
            ModelState.AddModelError("", "The desk could not be saved. Reload and try again.");
            await Choices(); return View("Edit", submitted);
        }
    }
}
