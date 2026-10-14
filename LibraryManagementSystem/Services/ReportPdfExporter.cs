using System.Globalization;
using LibraryManagementSystem.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryManagementSystem.Services
{
    public static class ReportPdfExporter
    {
        public static byte[] Export(ReportViewModel report)
        {
            var culture = CultureInfo.GetCultureInfo("en-AU");

            string Money(decimal amount) => amount.ToString("C", culture);
            string Date(DateTime? date) =>
                date?.ToString("dd MMM yyyy") ?? "Any";

            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(style => style.FontSize(10));

                    page.Header()
                        .Text("Library Management System — Report")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    page.Content().PaddingVertical(15).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text(
                            $"Generated: {report.GeneratedAt:dd MMM yyyy HH:mm}");

                        column.Item().Text(
                            $"Borrowed from: {Date(report.FromDate)}   " +
                            $"To: {Date(report.ToDate)}   " +
                            $"Type: {report.ItemType ?? "All"}");

                        column.Item().Text("Current inventory")
                            .FontSize(14).Bold();

                        column.Item().Text(
                            $"Total: {report.TotalItems}   " +
                            $"Available: {report.AvailableItems}   " +
                            $"Borrowed: {report.BorrowedItems}");

                        column.Item().Text(
                            "Inventory shows current stock. " +
                            "Loan date filters do not apply to inventory.");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Type");
                                header.Cell().Element(HeaderCell).Text("Status");
                                header.Cell().Element(HeaderCell).Text("Count");
                            });

                            foreach (var item in report.Inventory)
                            {
                                table.Cell().Element(BodyCell)
                                    .Text(item.ItemType);
                                table.Cell().Element(BodyCell)
                                    .Text(item.Status);
                                table.Cell().Element(BodyCell)
                                    .Text(item.Count.ToString());
                            }

                            if (report.Inventory.Count == 0)
                            {
                                table.Cell().ColumnSpan(3).Element(BodyCell)
                                    .Text("No inventory matches this item type.");
                            }
                        });

                        column.Item().Text("Loan summary")
                            .FontSize(14).Bold();

                        column.Item().Text(
                            $"Total: {report.TotalLoans}   " +
                            $"Active: {report.ActiveLoans}   " +
                            $"Overdue: {report.OverdueLoans}   " +
                            $"Returned: {report.ReturnedLoans}");

                        column.Item().Text(
                            "Active loans include overdue loans. " +
                            "Status is evaluated when the report is generated.");

                        column.Item().Text("Recorded fines")
                            .FontSize(14).Bold();

                        column.Item().Text(
                            $"Assessed: {Money(report.AssessedFines)}   " +
                            $"Marked paid: {Money(report.PaidFines)}   " +
                            $"Outstanding: {Money(report.OutstandingFines)}");

                        column.Item().Text(
                            "Fines are recorded amounts for the selected loans. " +
                            "These are not payment-date totals or estimates " +
                            "of unrecorded fines.");

                        column.Item().Text("Loan details")
                            .FontSize(14).Bold();

                        if (report.Loans.Count == 0)
                        {
                            column.Item().Text(
                                "No loans match the selected filters.");
                        }

                        foreach (var loan in report.Loans)
                        {
                            column.Item()
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten2)
                                .PaddingBottom(8)
                                .Column(details =>
                                {
                                    details.Spacing(3);

                                    details.Item().Text(
                                        $"Loan {loan.LoanId}: {loan.ItemName}")
                                        .Bold();

                                    details.Item().Text(
                                        $"Code: {loan.LibraryCode}   " +
                                        $"Type: {loan.ItemType}   " +
                                        $"Status: {loan.Status}");

                                    details.Item().Text(
                                        $"Borrowed: {Date(loan.BorrowDate)}   " +
                                        $"Due: {Date(loan.DueDate)}");

                                    details.Item().Text(
                                        $"Returned: " +
                                        $"{loan.ReturnDate?.ToString("dd MMM yyyy") ?? "Not returned"}");

                                    details.Item().Text(
                                        $"Fine: {Money(loan.FineAmount)}   " +
                                        $"Marked paid: {(loan.FinePaid ? "Yes" : "No")}");
                                });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .Padding(6)
                .DefaultTextStyle(style => style.Bold());
        }

        private static IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6);
        }
    }
}