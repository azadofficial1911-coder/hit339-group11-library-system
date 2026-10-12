using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options
    ) : IdentityDbContext(options)
    {
        public DbSet<Item> Items { get; set; } = default!;

        public DbSet<Book> Books { get; set; } = default!;

        public DbSet<Music> Music { get; set; } = default!;

        public DbSet<Toy> Toys { get; set; } = default!;

        public DbSet<Borrower> Borrowers { get; set; } = default!;

        public DbSet<Loan> Loans { get; set; } = default!;

        public DbSet<Reservation> Reservations { get; set; } = default!;

        public DbSet<NotificationLog> NotificationLogs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Store Book, Music and Toy records in one Items table.
            builder.Entity<Item>()
                .HasDiscriminator<string>("ItemType")
                .HasValue<Book>("Book")
                .HasValue<Music>("Music")
                .HasValue<Toy>("Toy");

            // Every library item must have a unique library code.
            builder.Entity<Item>()
                .HasIndex(item => item.LibraryCode)
                .IsUnique();

            // Store ItemStatus as readable text.
            builder.Entity<Item>()
                .Property(item => item.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Borrower codes and email addresses must be unique.
            builder.Entity<Borrower>()
                .HasIndex(borrower => borrower.BorrowerCode)
                .IsUnique();

            builder.Entity<Borrower>()
                .HasIndex(borrower => borrower.Email)
                .IsUnique();

            builder.Entity<Borrower>()
                .Property(borrower => borrower.BorrowerCode)
                .HasMaxLength(20);

            builder.Entity<Borrower>()
                .Property(borrower => borrower.FullName)
                .HasMaxLength(100);

            builder.Entity<Borrower>()
                .Property(borrower => borrower.Email)
                .HasMaxLength(150);

            builder.Entity<Borrower>()
                .Property(borrower => borrower.PhoneNumber)
                .HasMaxLength(25);

            builder.Entity<Borrower>()
                .Property(borrower => borrower.Address)
                .HasMaxLength(250);

            // Each loan belongs to one item.
            builder.Entity<Loan>()
                .HasOne(loan => loan.Item)
                .WithMany()
                .HasForeignKey(loan => loan.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Each loan belongs to one borrower.
            builder.Entity<Loan>()
                .HasOne(loan => loan.Borrower)
                .WithMany()
                .HasForeignKey(loan => loan.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Store fine values accurately as currency.
            builder.Entity<Loan>()
                .Property(loan => loan.FineAmount)
                .HasPrecision(10, 2);

            // Prevent the same item from having two active loans.
            builder.Entity<Loan>()
                .HasIndex(loan => loan.ItemId)
                .IsUnique()
                .HasFilter("[ReturnDate] IS NULL");

            // Improves borrower loan-history searches.
            builder.Entity<Loan>()
                .HasIndex(loan => loan.BorrowerId);

            // ---- R5 Reservations ----
            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.Item)
                .WithMany()
                .HasForeignKey(reservation => reservation.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Reservation>()
                .HasOne(reservation => reservation.Borrower)
                .WithMany()
                .HasForeignKey(reservation => reservation.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Store the hold status as readable text, like ItemStatus.
            builder.Entity<Reservation>()
                .Property(reservation => reservation.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // A borrower can have only one active hold on an item.
            builder.Entity<Reservation>()
                .HasIndex(reservation => new
                {
                    reservation.ItemId,
                    reservation.BorrowerId
                })
                .IsUnique()
                .HasFilter("[Status] IN ('Waiting', 'Ready')");

            // Speeds up queue lookups for an item.
            builder.Entity<Reservation>()
                .HasIndex(reservation => new
                {
                    reservation.ItemId,
                    reservation.Status,
                    reservation.CreatedAt
                });

            // ---- R3 Notifications ----
            builder.Entity<NotificationLog>()
                .HasOne(notification => notification.Borrower)
                .WithMany()
                .HasForeignKey(notification => notification.BorrowerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationLog>()
                .Property(notification => notification.Channel)
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Entity<NotificationLog>()
                .Property(notification => notification.EventType)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Stops the same message being logged twice.
            builder.Entity<NotificationLog>()
                .HasIndex(notification => notification.DedupKey)
                .IsUnique();

            builder.Entity<NotificationLog>()
                .HasIndex(notification => notification.CreatedAt);
        }
    }
}