using System.ComponentModel.DataAnnotations;
using System.Globalization;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;

namespace LibraryManagementSystem.Services
{
    public static class ImportItemFactory
    {
        public static Item? Create(ImportRowViewModel row)
        {
            string Field(string name) =>
                row.Fields.TryGetValue(name, out var value)
                    ? value.Trim()
                    : string.Empty;

            int Number(string name)
            {
                if (int.TryParse(
                    Field(name),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int value))
                {
                    return value;
                }

                row.Errors.Add($"{name} must be a whole number.");
                return 0;
            }

            Item? item = row.ItemType.Trim().ToLowerInvariant() switch
            {
                "book" => new Book
                {
                    Author = Field("Author"),
                    Genre = Field("Genre"),
                    Isbn = Field("Isbn"),
                    PublicationYear = Number("PublicationYear")
                },
                "music" => new Music
                {
                    Artist = Field("Artist"),
                    MusicGenre = Field("MusicGenre"),
                    ReleaseYear = Number("ReleaseYear"),
                    Format = Field("Format")
                },
                "toy" => new Toy
                {
                    ToyType = Field("ToyType"),
                    RecommendedAge = Number("RecommendedAge"),
                    Manufacturer = Field("Manufacturer"),
                    SafetyInformation = Field("SafetyInformation")
                },
                _ => null
            };

            if (item == null)
            {
                row.Errors.Add("ItemType must be Book, Music or Toy.");
                return null;
            }

            item.Name = row.Name.Trim();
            item.Description = row.Description.Trim();
            item.LibraryCode = row.LibraryCode.Trim();
            item.BranchId = row.BranchId;
            item.Status = ItemStatus.Available;
            item.DateAdded = DateTime.Today;

            var results = new List<ValidationResult>();

            Validator.TryValidateObject(
                item,
                new ValidationContext(item),
                results,
                validateAllProperties: true);

            foreach (var result in results)
            {
                string message = result.ErrorMessage ?? "Invalid item.";

                if (!row.Errors.Contains(message))
                {
                    row.Errors.Add(message);
                }
            }

            return item;
        }
    }
}