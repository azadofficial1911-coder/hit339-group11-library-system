using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public static class ReservationDemoSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            ApplicationDbContext context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Borrower one = await GetOrAddBorrowerAsync(
                context,
                "BR-DEMO1",
                "Demo Borrower One",
                "demo1@example.com",
                "0400000001");

            Borrower two = await GetOrAddBorrowerAsync(
                context,
                "BR-DEMO2",
                "Demo Borrower Two",
                "demo2@example.com",
                "0400000002");

            await GetOrAddBorrowerAsync(
                context,
                "BR-DEMO3",
                "Demo Borrower Three",
                "demo3@example.com",
                "0400000003");

            Book queueBook = await GetOrAddBookAsync(
                context,
                "BK-R5-01",
                "Reservation Queue Test Book",
                ItemStatus.Borrowed);

            await GetOrAddBookAsync(
                context,
                "BK-R5-02",
                "Damaged Reservation Test Book",
                ItemStatus.Damaged);

            Book dueSoonBook = await GetOrAddBookAsync(
                context,
                "BK-R3-DUE",
                "Due Soon Reminder Test Book",
                ItemStatus.Borrowed);

            Book overdueBook = await GetOrAddBookAsync(
                context,
                "BK-R3-OVER",
                "Overdue Fine Test Book",
                ItemStatus.Borrowed);

            await AddCatalogueItemsAsync(context);

            await context.SaveChangesAsync();

            await AddActiveLoanIfMissingAsync(
                context,
                queueBook,
                one,
                DateTime.Today.AddDays(-4),
                DateTime.Today.AddDays(10));

            await AddActiveLoanIfMissingAsync(
                context,
                dueSoonBook,
                one,
                DateTime.Today.AddDays(-12),
                DateTime.Today.AddDays(2));

            await AddActiveLoanIfMissingAsync(
                context,
                overdueBook,
                two,
                DateTime.Today.AddDays(-19),
                DateTime.Today.AddDays(-5));

            await context.SaveChangesAsync();
        }

        private static async Task AddCatalogueItemsAsync(
            ApplicationDbContext context)
        {
            // Fictional catalogue records for demonstrations.
            // Existing records are left unchanged, including their loan status.
            var existingCodes = new HashSet<string>(
                await context.Items
                    .Select(item => item.LibraryCode)
                    .ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            void AddIfMissing(Item item)
            {
                if (existingCodes.Add(item.LibraryCode))
                {
                    context.Items.Add(item);
                }
            }

            var books = new[]
            {
                (
                    Code: "BK-DEMO-01",
                    Name: "Beneath the Darwin Sky",
                    Author: "Amelia Hart",
                    Genre: "Fiction",
                    Year: 2024,
                    Description:
                        "A young photographer returns to Darwin and discovers " +
                        "a collection of letters that changes how she sees home."
                ),
                (
                    Code: "BK-DEMO-02",
                    Name: "The Quiet Library",
                    Author: "Oliver Reed",
                    Genre: "Mystery",
                    Year: 2023,
                    Description:
                        "When a handwritten note appears inside a returned book, " +
                        "a librarian follows a trail of clues through a coastal town."
                ),
                (
                    Code: "BK-DEMO-03",
                    Name: "Learning C# Through Projects",
                    Author: "Maya Collins",
                    Genre: "Technology",
                    Year: 2025,
                    Description:
                        "A fictional demonstration title about programming " +
                        "fundamentals, classes, databases and practical applications."
                ),
                (
                    Code: "BK-DEMO-04",
                    Name: "Small Steps, Strong Habits",
                    Author: "Daniel Brooks",
                    Genre: "Personal Development",
                    Year: 2024,
                    Description:
                        "Explore everyday routines, thoughtful goal setting " +
                        "and practical ways to build consistent habits."
                ),
                (
                    Code: "BK-DEMO-05",
                    Name: "Adventures Beyond the Mango Tree",
                    Author: "Sophie Lane",
                    Genre: "Children",
                    Year: 2022,
                    Description:
                        "Two friends discover a hidden path beyond their garden " +
                        "and begin an adventure filled with puzzles and kindness."
                ),
                (
                    Code: "BK-DEMO-06",
                    Name: "Flavours Around the Table",
                    Author: "Aisha Rahman",
                    Genre: "Cooking",
                    Year: 2025,
                    Description:
                        "A fictional collection of family cooking stories, " +
                        "simple meal ideas and inspiration for sharing food."
                )
            };

            foreach (var book in books)
            {
                AddIfMissing(new Book
                {
                    LibraryCode = book.Code,
                    Name = book.Name,
                    Description = book.Description,
                    Status = ItemStatus.Available,
                    Author = book.Author,
                    Genre = book.Genre,
                    PublicationYear = book.Year
                });
            }

            var musicItems = new[]
            {
                (
                    Code: "MU-DEMO-01",
                    Name: "Golden Hour Sessions",
                    Artist: "The Harbour Collective",
                    Genre: "Acoustic",
                    Year: 2024,
                    Format: "CD",
                    Description:
                        "Warm acoustic guitar and gentle vocals inspired " +
                        "by slow afternoons beside the water."
                ),
                (
                    Code: "MU-DEMO-02",
                    Name: "City After Dark",
                    Artist: "Neon Avenue",
                    Genre: "Electronic",
                    Year: 2025,
                    Format: "CD",
                    Description:
                        "Layered synthesizers and steady rhythms create " +
                        "a soundtrack for late-night city journeys."
                ),
                (
                    Code: "MU-DEMO-03",
                    Name: "Sunday Jazz Room",
                    Artist: "The Willow Quartet",
                    Genre: "Jazz",
                    Year: 2023,
                    Format: "CD",
                    Description:
                        "Relaxed piano, double bass, drums and saxophone " +
                        "come together in a mellow instrumental collection."
                ),
                (
                    Code: "MU-DEMO-04",
                    Name: "Open Road",
                    Artist: "Northern Echo",
                    Genre: "Rock",
                    Year: 2024,
                    Format: "CD",
                    Description:
                        "Bright guitar melodies and energetic percussion " +
                        "capture the feeling of an open-road adventure."
                ),
                (
                    Code: "MU-DEMO-05",
                    Name: "Little Songs, Big Smiles",
                    Artist: "The Sunshine Singers",
                    Genre: "Children",
                    Year: 2022,
                    Format: "CD",
                    Description:
                        "Playful songs about colours, counting, animals " +
                        "and everyday adventures for young listeners."
                ),
                (
                    Code: "MU-DEMO-06",
                    Name: "Evening Strings",
                    Artist: "Aurora Chamber Ensemble",
                    Genre: "Classical",
                    Year: 2025,
                    Format: "CD",
                    Description:
                        "A fictional instrumental album featuring " +
                        "expressive strings and quiet chamber arrangements."
                )
            };

            foreach (var music in musicItems)
            {
                AddIfMissing(new Music
                {
                    LibraryCode = music.Code,
                    Name = music.Name,
                    Description = music.Description,
                    Status = ItemStatus.Available,
                    Artist = music.Artist,
                    MusicGenre = music.Genre,
                    ReleaseYear = music.Year,
                    Format = music.Format
                });
            }

            var toys = new[]
            {
                (
                    Code: "TY-DEMO-01",
                    Name: "Rainbow Building Blocks",
                    Type: "Construction",
                    Age: 3,
                    Manufacturer: "Demo Playworks",
                    Description:
                        "A colourful building set for creating towers, " +
                        "bridges and imaginative miniature worlds.",
                    Safety:
                        "Demo record only. Verify the real product age label, " +
                        "parts and safety instructions before lending."
                ),
                (
                    Code: "TY-DEMO-02",
                    Name: "Ocean Adventure Puzzle",
                    Type: "Puzzle",
                    Age: 6,
                    Manufacturer: "Demo Discovery",
                    Description:
                        "An ocean-themed puzzle featuring colourful sea life " +
                        "and a detailed underwater scene.",
                    Safety:
                        "Small pieces. Not suitable for children under 3. " +
                        "Check the real product instructions before lending."
                ),
                (
                    Code: "TY-DEMO-03",
                    Name: "Family Adventure Board Game",
                    Type: "Board Game",
                    Age: 8,
                    Manufacturer: "Demo Tabletop",
                    Description:
                        "A fictional family game about planning journeys, " +
                        "collecting supplies and solving challenges together.",
                    Safety:
                        "Contains small playing pieces. Not suitable under 3. " +
                        "Verify the real product age recommendation."
                ),
                (
                    Code: "TY-DEMO-04",
                    Name: "Wooden Shape Sorter",
                    Type: "Educational",
                    Age: 3,
                    Manufacturer: "Demo Playworks",
                    Description:
                        "Match colourful wooden shapes to their openings " +
                        "while exploring colours and problem solving.",
                    Safety:
                        "Inspect for loose parts, cracks and splinters. " +
                        "Verify the real product label before lending."
                ),
                (
                    Code: "TY-DEMO-05",
                    Name: "Space Explorer Construction Kit",
                    Type: "Construction",
                    Age: 8,
                    Manufacturer: "Demo Discovery",
                    Description:
                        "Build an imaginary spacecraft and create " +
                        "stories about distant planets and exploration.",
                    Safety:
                        "Small components. Not suitable for children under 3. " +
                        "Check all parts and real product instructions."
                ),
                (
                    Code: "TY-DEMO-06",
                    Name: "Animal Memory Match",
                    Type: "Memory Game",
                    Age: 4,
                    Manufacturer: "Demo Tabletop",
                    Description:
                        "A matching-card game featuring friendly animals " +
                        "for practising observation and turn taking.",
                    Safety:
                        "Inspect cards and packaging before lending. " +
                        "Follow the real product age label and instructions."
                )
            };

            foreach (var toy in toys)
            {
                AddIfMissing(new Toy
                {
                    LibraryCode = toy.Code,
                    Name = toy.Name,
                    Description = toy.Description,
                    Status = ItemStatus.Available,
                    ToyType = toy.Type,
                    RecommendedAge = toy.Age,
                    Manufacturer = toy.Manufacturer,
                    SafetyInformation = toy.Safety
                });
            }
        }

        private static async Task<Borrower> GetOrAddBorrowerAsync(
            ApplicationDbContext context,
            string code,
            string name,
            string email,
            string phone)
        {
            Borrower? existing = await context.Borrowers
                .FirstOrDefaultAsync(
                    borrower => borrower.BorrowerCode == code);

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
                .FirstOrDefaultAsync(
                    book => book.LibraryCode == code);

            if (existing != null)
            {
                return existing;
            }

            Book book = new()
            {
                Name = name,
                Description =
                    "Demo record for testing reservations and notifications.",
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
                .AnyAsync(
                    loan => loan.ItemId == book.Id &&
                            loan.ReturnDate == null);

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