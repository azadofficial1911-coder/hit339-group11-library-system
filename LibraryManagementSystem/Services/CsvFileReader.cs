using Microsoft.VisualBasic.FileIO;

namespace LibraryManagementSystem.Services
{
    // Reads CSV fields, including quoted commas and multiline text.
    public static class CsvFileReader
    {
        public static List<string[]> Read(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            using var parser = new TextFieldParser(stream)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true,
                TrimWhiteSpace = true
            };

            parser.SetDelimiters(",");

            var records = new List<string[]>();

            while (!parser.EndOfData)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fields = parser.ReadFields();

                if (fields != null)
                {
                    records.Add(fields);
                }

                // Allow one header and up to 1,000 item rows.
                if (records.Count > 1001)
                {
                    throw new FormatException(
                        "The CSV can contain at most 1,000 item rows.");
                }
            }

            return records;
        }
    }
}