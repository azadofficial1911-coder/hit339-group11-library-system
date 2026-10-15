using LibraryManagementSystem.Models;
namespace LibraryManagementSystem.ViewModels;
public class BranchInventoryViewModel
{
    public int? BranchId { get; set; }
    public List<Branch> Branches { get; set; } = [];
    public List<Item> Items { get; set; } = [];
    public List<ItemTransfer> Transfers { get; set; } = [];
}
