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
        }
    }
}