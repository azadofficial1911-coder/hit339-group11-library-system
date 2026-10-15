using System.ComponentModel.DataAnnotations;
namespace LibraryManagementSystem.Models;

public enum TransferStatus { Dispatched, Received }

// One Item is one physical copy; a transfer never creates a second copy.
public class ItemTransfer
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public Item Item { get; set; } = default!;
    public int SourceBranchId { get; set; }
    public Branch SourceBranch { get; set; } = default!;
    public int DestinationBranchId { get; set; }
    public Branch DestinationBranch { get; set; } = default!;
    public TransferStatus Status { get; set; } = TransferStatus.Dispatched;
    public DateTime DispatchedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
    [MaxLength(256)] public string DispatchedBy { get; set; } = string.Empty;
    [MaxLength(256)] public string? ReceivedBy { get; set; }
}
