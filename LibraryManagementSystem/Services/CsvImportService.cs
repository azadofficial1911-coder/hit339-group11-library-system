using LibraryManagementSystem.Data;
using LibraryManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace LibraryManagementSystem.Services
{
    public class CsvImportService : ICsvImportService
    {
        private readonly ApplicationDbContext _context;

        public CsvImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ImportPreviewViewModel> PreviewAsync(
            Stream csvStream,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            var preview = new ImportPreviewViewModel
            {
                FileName = Path.GetFileName(fileName)
            };

            List<string[]> records;

            try
            {
                records = CsvFileReader.Read(csvStream, cancellationToken);
            }
            catch (MalformedLineException)
            {
                preview.Errors.Add(
                    "The CSV contains incorrectly formatted quotation marks.");
                return preview;
            }
            catch (FormatException ex)
            {
                preview.Errors.Add(ex.Message);
                return preview;
            }

            if (records.Count < 2)
            {
                preview.Errors.Add(
                    "Include a header and at least one item row.");
                return preview;
            }

            var headers = records[0]
                .Select(header => header.Trim())
                .ToArray();

            var comparer = StringComparer.OrdinalIgnoreCase;

            if (headers.Any(string.IsNullOrWhiteSpace) ||
                headers.Distinct(comparer).Count() != headers.Length)
            {
                preview.Errors.Add(
                    "Column headings must be unique and cannot be empty.");
                return preview;
            }

            string[] requiredHeaders =
            {
                "ItemType", "Name", "Description", "LibraryCode"
            };

            foreach (var required in requiredHeaders)
            {
                if (!headers.Contains(required, comparer))
                {
                    preview.Errors.Add($"Missing column: {required}.");
                }
            }

            string[] allowedHeaders =
            {
                "ItemType", "Name", "Description", "LibraryCode",
                "Author", "Genre", "Isbn", "PublicationYear",
                "Artist", "MusicGenre", "ReleaseYear", "Format",
                "ToyType", "RecommendedAge", "Manufacturer",
                "SafetyInformation"
            };

            foreach (var header in headers)
            {
                if (!allowedHeaders.Contains(header, comparer))
                {
                    preview.Errors.Add($"Unknown column: {header}.");
                }
            }

            if (preview.Errors.Count > 0)
            {
                return preview;
            }

            for (int index = 1; index < records.Count; index++)
            {
                var row = new ImportRowViewModel
                {
                    RowNumber = index + 1,
                    Fields = new Dictionary<string, string>(comparer)
                };

                preview.Rows.Add(row);

                if (records[index].Length != headers.Length)
                {
                    row.Errors.Add(
                        "The number of fields does not match the header.");
                    continue;
                }

                for (int column = 0; column < headers.Length; column++)
                {
                    row.Fields[headers[column]] =
                        records[index][column].Trim();
                }

                row.ItemType = row.Fields["ItemType"];
                row.Name = row.Fields["Name"];
                row.Description = row.Fields["Description"];
                row.LibraryCode = row.Fields["LibraryCode"];

                ImportItemFactory.Create(row);
            }

            var duplicateGroups = preview.Rows
                .Where(row => !string.IsNullOrWhiteSpace(row.LibraryCode))
                .GroupBy(row => row.LibraryCode, comparer)
                .Where(group => group.Count() > 1);

            foreach (var group in duplicateGroups)
            {
                foreach (var row in group)
                {
                    row.Errors.Add(
                        "This LibraryCode appears more than once in the CSV.");
                }
            }

            var codes = preview.Rows
                .Where(row => !string.IsNullOrWhiteSpace(row.LibraryCode))
                .Select(row => row.LibraryCode.ToUpperInvariant())
                .Distinct()
                .ToArray();

            var existingCodes = await _context.Items
                .AsNoTracking()
                .Where(item => codes.Contains(item.LibraryCode.ToUpper()))
                .Select(item => item.LibraryCode)
                .ToListAsync(cancellationToken);

            var existingCodeSet = new HashSet<string>(
                existingCodes, comparer);

            foreach (var row in preview.Rows)
            {
                if (existingCodeSet.Contains(row.LibraryCode))
                {
                    row.Errors.Add(
                        "This LibraryCode already exists in the database.");
                }
            }

            return preview;
        }
    }
}