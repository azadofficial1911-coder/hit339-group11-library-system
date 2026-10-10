using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.ViewModels
{
    // Contains the public search filters and search results.
    public class CatalogueSearchViewModel
    {
        public string SearchTerm { get; set; } = string.Empty;

        public string SelectedItemType { get; set; } = string.Empty;

        public string SelectedStatus { get; set; } = string.Empty;

        public List<CatalogueItemViewModel> Items { get; set; } =
            new List<CatalogueItemViewModel>();
    }

    // Contains only information that is safe for public display.
    public class CatalogueItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        public ItemStatus Status { get; set; }

        public string ItemType { get; set; } = string.Empty;

        public string PrimaryDetail { get; set; } = string.Empty;

        public string SecondaryDetail { get; set; } = string.Empty;
    }
}