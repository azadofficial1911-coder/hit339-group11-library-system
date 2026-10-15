using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagementSystem.Controllers;

[ApiController]
[Route("api/catalogue")]
[Authorize(AuthenticationSchemes = CatalogueApiAuthenticationHandler.SchemeName)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class CatalogueApiController(ApplicationDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("items")]
    public async Task<IActionResult> Items(int? branchId, int page = 1, int pageSize = 50)
    {
        if (page < 1 || page > 1000000 || pageSize < 1 || pageSize > 100 || branchId is <= 0)
            return BadRequest(new { error = "Use a positive branchId, page 1–1000000 and pageSize 1–100." });
        var query = db.Items.AsNoTracking().Where(i => i.Status == ItemStatus.Available &&
            i.BranchId != null && i.Branch!.IsActive &&
            !db.Loans.Any(l => l.ItemId == i.Id && l.ReturnDate == null) &&
            !db.Reservations.Any(h => h.ItemId == i.Id &&
                (h.Status == ReservationStatus.Waiting || h.Status == ReservationStatus.Ready)) &&
            !db.ItemTransfers.Any(t => t.ItemId == i.Id && t.Status == TransferStatus.Dispatched));
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId);
        var total = await query.CountAsync();
        var items = await query.OrderBy(i => i.Name).ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new {
                i.Id, i.LibraryCode, i.Name, i.Description,
                Category = EF.Property<string>(i, "ItemType"), Status = "Available",
                i.BranchId, BranchCode = i.Branch!.Code, BranchName = i.Branch.Name
            }).ToListAsync();
        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("categories")]
    public IActionResult Categories() => Ok(new[] { "Book", "Music", "Toy" });

    [HttpGet("status")]
    public async Task<IActionResult> Status() => Ok(new {
        status = configuration.GetValue<bool>("CatalogueApi:Maintenance") ? "Maintenance" : "Operating",
        timestampUtc = DateTime.UtcNow,
        branches = await db.Branches.AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Code, b.Name, b.IsActive, b.OpeningHours }).ToListAsync()
    });
}
