# Library Management System

## Student Information

- Name: Azad Howlader
- Student ID: s379798
- Unit: HIT339 Distributed Development
- Assessment: Coding Assignment 2

## Project Overview

The Library Management System is an ASP.NET Core MVC web application for managing books, music, toys, borrowers, borrowing transactions, returns and late fines.

The application provides separate functionality for Admin, Reception and Manager users. It also provides a responsive public catalogue that visitors can search without creating an account.

## Main Features

### Administrator

Administrators can:

- Create, view, edit and delete books
- Create, view, edit and delete music items
- Create, view, edit and delete toys
- Search items
- Filter items by status
- Manage item-specific information
- Prevent duplicate library codes

### Reception

Reception users can:

- Create, view, edit and delete borrowers
- Search and filter borrowers
- Borrow an available item using its library code
- Identify borrowers using their borrower code
- Return borrowed items
- Calculate late fines automatically
- View active, overdue and returned loans
- Record fine payments

### Manager

Managers can view:

- Total, active, overdue and returned loans
- Total, available, borrowed, damaged and destroyed items
- Book, music and toy totals
- Total registered and active borrowers
- Total, paid and outstanding fine amounts
- Item availability and loan return percentages

### Public Catalogue

Public visitors can:

- Access the catalogue without logging in
- Search by item name, description or library code
- Filter by book, music or toy
- Filter by availability status
- View the catalogue using a phone, tablet or computer

## Technologies

- ASP.NET Core MVC
- C#
- .NET 10
- Entity Framework Core
- SQL Server LocalDB
- ASP.NET Core Identity
- Razor Views
- Bootstrap
- LINQ
- Dependency Injection

## Architecture

The application follows the Model-View-Controller pattern.

- Models define library items, borrowers and loans.
- Views display responsive user interfaces.
- Controllers process requests and coordinate application behaviour.
- ApplicationDbContext manages Entity Framework database access.
- Services contain reusable business logic such as fine calculation.
- ViewModels restrict submitted and displayed information.

## Object-Oriented Design

Item is the base class for:

- Book
- Music
- Toy

Entity Framework uses Table-per-Hierarchy inheritance. The database stores the inherited item records in one Items table and uses an ItemType discriminator.

Common item properties include:

- Name
- Description
- LibraryCode
- Status
- DateAdded

The ItemStatus enumeration contains:

- Available
- Borrowed
- Damaged
- Destroyed

## Database Design

Important tables include:

- Items
- Borrowers
- Loans
- ASP.NET Core Identity tables

Each Loan connects one Item with one Borrower.

A filtered unique database index prevents the same item from having more than one active loan. Foreign-key restrictions preserve borrowing history.

## Fine Calculation

The default late-fine rules are configured in appsettings.json:

- Daily late fee: $2.00
- Maximum fine: $100.00

FineCalculator implements IFineCalculator. The controller depends on the interface through Dependency Injection, demonstrating the Dependency Inversion Principle.

## Security

The application uses ASP.NET Core Identity and role-based authorization.

Roles:

- Admin
- Reception
- Manager

Security controls include:

- Password complexity rules
- Account lockout
- Authentication before authorization
- Role-based controller protection
- Antiforgery validation on POST requests
- ViewModels and controlled model binding
- Server-side validation
- Duplicate-value protection
- Parameterised Entity Framework queries
- Public pages exposing only safe item information

## Unit Learning Material Connections

The project applies concepts from the unit materials:

- C# classes, inheritance, interfaces and enumerations
- ASP.NET Core MVC controllers and Razor Views
- Entity Framework Core relationships, LINQ and migrations
- Dependency Injection and the Dependency Inversion Principle
- ASP.NET Core Identity, authentication and authorization
- Validation attributes and overposting protection
- Exception handling and user-friendly messages
- TempData for messages between requests
- Bootstrap responsive web design

## Installation

### Requirements

- Visual Studio 2022 or later
- .NET 10 SDK
- ASP.NET and web development workload
- SQL Server Express LocalDB

### Setup

1. Extract the submitted ZIP file.
2. Open the solution in Visual Studio.
3. Wait for NuGet packages to restore.
4. Open Package Manager Console.
5. Run:

   Update-Database

6. Build the solution.
7. Run the application using HTTPS.

## Demonstration Accounts

### Administrator

- Email: admin@librarydemo.com
- Password: Library123!

### Reception

- Email: reception@librarydemo.com
- Password: Library123!

### Manager

- Email: manager@librarydemo.com
- Password: Library123!

These demonstration accounts are automatically created by IdentitySeeder.

## Important Routes

- Public catalogue: /Catalogue
- Admin dashboard: /Admin
- Reception dashboard: /Reception
- Manager dashboard: /Manager
- Borrowers: /Borrowers
- Loans: /Loans

## Testing Summary

The application was tested for:

- Item CRUD operations
- Borrower CRUD operations
- Duplicate-code validation
- Role authorization
- Borrowing and returning
- Late-fine calculation
- Fine payment
- Search and filtering
- Manager statistics
- Anonymous public access
- Responsive mobile layout
- Successful database migration
- Successful solution build