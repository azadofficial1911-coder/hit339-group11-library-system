namespace LibraryManagementSystem.ViewModels
{
    // Stores a CSV row and any errors shown in the import preview.
    public class ImportRowViewModel
    {
        public int RowNumber { get; set; }

        public string ItemType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LibraryCode { get; set; } = string.Empty;

        // Keeps the original CSV fields for item-specific validation.
        public Dictionary<string, string> Fields { get; set; } = new();

        public List<string> Errors { get; set; } = new();

        public bool IsValid => Errors.Count == 0;
    }
}