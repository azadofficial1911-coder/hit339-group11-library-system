namespace LibraryManagementSystem.ViewModels
{
    // Holds the full upload preview before import is confirmed.
    public class ImportPreviewViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public string PreviewToken { get; set; } = string.Empty;

        public List<ImportRowViewModel> Rows { get; set; } = new();

        public List<string> Errors { get; set; } = new();

        public int ValidRowCount => Rows.Count(row => row.IsValid);

        public int InvalidRowCount => Rows.Count(row => !row.IsValid);

        public bool CanImport =>
            Errors.Count == 0 &&
            Rows.Count > 0 &&
            InvalidRowCount == 0;
    }
}