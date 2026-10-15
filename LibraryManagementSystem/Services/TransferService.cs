using System.Data;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace LibraryManagementSystem.Services;

public record TransferResult(bool Success, string Message);
public class TransferService(ApplicationDbContext db, IReservationService reservations)
{
    public async Task<TransferResult> AssignAsync(int itemId, int branchId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await db.Items.SingleOrDefaultAsync(i => i.Id == itemId);
        if (item == null || item.BranchId != null || item.Status != ItemStatus.Available)
            return new(false, "Only an available, unassigned copy can be assigned. Use a transfer to move existing stock.");
        if (!await db.Branches.AnyAsync(b => b.Id == branchId && b.IsActive))
            return new(false, "Select an active branch.");
        if (await db.Loans.AnyAsync(l => l.ItemId == itemId && l.ReturnDate == null) ||
            await db.Reservations.AnyAsync(h => h.ItemId == itemId &&
                (h.Status == ReservationStatus.Waiting || h.Status == ReservationStatus.Ready)))
            return new(false, "Resolve active loans and holds before assigning this copy.");
        item.BranchId = branchId;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, "Copy assigned to branch.");
    }

    public async Task<TransferResult> DispatchAsync(int itemId, int destinationId, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var item = await db.Items.Include(i => i.Branch).SingleOrDefaultAsync(i => i.Id == itemId);
        if (item == null || item.BranchId == null || item.Status != ItemStatus.Available)
            return new(false, "Only an available copy assigned to a branch can be dispatched.");
        if (destinationId == item.BranchId)
            return new(false, "Source and destination must be different branches.");
        if (item.Branch?.IsActive != true || !await db.Branches.AnyAsync(b => b.Id == destinationId && b.IsActive))
            return new(false, "Both branches must be active.");
        if (await db.Loans.AnyAsync(l => l.ItemId == itemId && l.ReturnDate == null) ||
            await db.Reservations.AnyAsync(h => h.ItemId == itemId &&
                (h.Status == ReservationStatus.Waiting || h.Status == ReservationStatus.Ready)) ||
            await db.ItemTransfers.AnyAsync(t => t.ItemId == itemId && t.Status == TransferStatus.Dispatched))
            return new(false, "This copy has an active loan, hold or transfer.");
        db.ItemTransfers.Add(new ItemTransfer {
            ItemId = itemId, SourceBranchId = item.BranchId.Value,
            DestinationBranchId = destinationId, DispatchedBy = actor
        });
        item.Status = ItemStatus.InTransit;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new(true, "Transfer dispatched. The copy is unavailable until receipt.");
    }

    public async Task<TransferResult> ReceiveAsync(int transferId, int receivingBranchId, string actor)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var transfer = await db.ItemTransfers.Include(t => t.Item)
            .SingleOrDefaultAsync(t => t.Id == transferId);
        if (transfer == null || transfer.Status != TransferStatus.Dispatched)
            return new(false, "This transfer is missing or has already been received.");
        if (transfer.DestinationBranchId != receivingBranchId)
            return new(false, "Select the destination branch to confirm receipt.");
        if (!await db.Branches.AnyAsync(b => b.Id == receivingBranchId && b.IsActive))
            return new(false, "The receiving branch is inactive.");
        if (transfer.Item.Status != ItemStatus.InTransit || transfer.Item.BranchId != transfer.SourceBranchId)
            return new(false, "The copy no longer matches the dispatched transfer. Ask an administrator to review it.");
        transfer.Item.BranchId = transfer.DestinationBranchId;
        transfer.Item.Status = ItemStatus.Available;
        transfer.Status = TransferStatus.Received;
        transfer.ReceivedAt = DateTime.UtcNow;
        transfer.ReceivedBy = actor;
        await db.SaveChangesAsync();
        // Respect any queue created while the copy was away, using the same transaction.
        await reservations.OnItemAvailableAsync(transfer.ItemId);
        await tx.CommitAsync();
        return new(true, "Transfer received at the destination branch.");
    }
}
