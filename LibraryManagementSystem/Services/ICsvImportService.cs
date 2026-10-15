using LibraryManagementSystem.ViewModels;

namespace LibraryManagementSystem.Services
{
    public interface ICsvImportService
    {
        Task<ImportPreviewViewModel> PreviewAsync(
            Stream csvStream,
            string fileName,
            CancellationToken cancellationToken = default);
    }
}