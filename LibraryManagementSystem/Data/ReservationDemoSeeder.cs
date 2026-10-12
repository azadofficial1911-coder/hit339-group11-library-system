using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    // Optional demo data for testing reservations (R5) and notifications (R3).
    // Turn it on with  "DemoData": { "SeedReservationScenario": true }  in appsettings,
    // or the environment variable  DemoData__SeedReservationScenario=true.
    // Safe to run more than once: it only adds records that are missing.
    public static class ReservationDemoSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            ApplicationDbContext context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Borrower one = await GetOrAddBorrowerAsync(
                context, "BR-DEMO1", "Demo Borrower One", "demo1@example.com", "0400000001");

            Borrower two = await GetOrAddBorrowerAsync(
                context, "BR-DEMO2", "Demo Borrower Two", "demo2@example.com", "0400000002");

            await GetOrAddBorrowerAsync(
                context, "BR-DEMO3", "Demo Borrower Three", "demo3@example.com", "0400000003");

            // R5: a borrowed item to queue on, and a damaged item to repair.
            Book queueBook = await GetOrAddBookAsync(
                context, "BK-R5-01", "Reservation Queue Test Book", ItemStatus.Borrowed);

            await GetOrAddBookAsync(
                context, "BK-R5-02", "Damaged Reservation Test Book", ItemStatus.Damaged);

            // R3: one loan due in 2 days and one that is 5 days overdue.
            Book dueSoonBook = await GetOrAddBookAsync(
                context, "BK-R3-DUE", "Due Soon Reminder Test Book", ItemStatus.Borrowed);

            Book overdueBook = await GetOrAddBookAsync(
                context, "BK-R3-OVER", "Overdue Fine Test Book", ItemStatus.Borrowed);

            await context.SaveChangesAsync();

            await AddActiveLoanIfMissingAsync(
                context, queueBook, one, DateTime.Today.AddDays(-4), DateTime.Today.AddDays(10));

            await AddActiveLoanIfMissingAsync(
                context, dueSoonBook, one, DateTime.Today.AddDays(-12), DateTime.Today.AddDays(2));

            await AddActiveLoanIfMissingAsync(
                context, overdueBook, two, DateTime.Today.AddDays(-19), DateTime.Today.AddDays(-5));

            await context.SaveChangesAsync();
        }

        private static async Task<Borrower> GetOrAddBorrowerAsync(
            ApplicationDbContext context,
            string code,
            string name,
            string email,
            string phone)
        {
            Borrower? existing = await context.Borrowers
                .FirstOrDefaultAsync(borrower => borrower.BorrowerCode == code);

            if (existing != null)
            {
                return existing;
            }

            Borrower borrower = new()
            {
                BorrowerCode = code,
                FullName = name,
                Email = email,
                PhoneNumber = phone,
                Address = "1 Demo Street, Darwin NT",
                IsActive = true
            };

            context.Borrowers.Add(borrower);
            await context.SaveChangesAsync();

            return borrower;
        }

        private static async Task<Book> GetOrAddBookAsync(
            ApplicationDbContext context,
            string code,
            string name,
            ItemStatus status)
        {
            Book? existing = await context.Books
                .FirstOrDefaultAsync(book => book.LibraryCode == code);

            if (existing != null)
            {
                return existing;
            }

            Book book = new()
            {
                Name = name,
                Description = "Demo record for testing reservations and notifications.",
                LibraryCode = code,
                Status = status,
                Author = "Demo Author",
                Genre = "Demo",
                PublicationYear = 2024
            };

            context.Books.Add(book);

            return book;
        }

        private static async Task AddActiveLoanIfMissingAsync(
            ApplicationDbContext context,
            Book book,
            Borrower borrower,
            DateTime borrowDate,
            DateTime dueDate)
        {
            bool hasActiveLoan = await context.Loans
                .AnyAsync(loan => loan.ItemId == book.Id && loan.ReturnDate == null);

            if (hasActiveLoan)
            {
                return;
            }

            context.Loans.Add(new Loan
            {
                ItemId = book.Id,
                BorrowerId = borrower.Id,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                IssuedBy = "demo-seeder"
            });
        }
    }
}
