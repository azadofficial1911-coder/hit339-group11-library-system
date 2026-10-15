using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagementSystem.Data;

public static class BranchDemoSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var (code, name) in new[] { ("DEMO-DAR", "Darwin"), ("DEMO-CAS", "Casuarina"), ("DEMO-PAL", "Palmerston") }) {
            var branch = await db.Branches.SingleOrDefaultAsync(b => b.Code == code);
            if (branch == null) {
                branch = new Branch { Code = code, Name = $"{name} (Demo)", Address = "Illustrative location only — not a real library address",
                    OpeningHours = "Demo: Mon–Fri 9am–5pm", AccessibilityInformation = "Demo: step-free entrance and accessible service desk" };
                db.Branches.Add(branch);
                await db.SaveChangesAsync();
            }
            if (!await db.ReceptionDesks.AnyAsync(d => d.Code == code + "-01"))
                db.ReceptionDesks.Add(new ReceptionDesk { Code = code + "-01", Name = name + " demo desk", BranchId = branch.Id });
            if (!await db.Items.AnyAsync(i => i.LibraryCode == code + "-BOOK"))
                db.Books.Add(new Book { LibraryCode = code + "-BOOK", Name = name + " demo transfer copy", Description = "Illustrative stock for branch and transfer demonstrations.",
                    BranchId = branch.Id, Author = "Demo author", Genre = "Demonstration", PublicationYear = 2026 });
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
    }
}
