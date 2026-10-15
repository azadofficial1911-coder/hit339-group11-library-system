# LibraryLink NT

**HIT339 Distributed Development — Group 11**

LibraryLink NT is an ASP.NET Core MVC library management application built with .NET 10. It supports books, music, toys, borrowers, loans, returns and fines, with a public catalogue and dedicated self-service kiosk.

## Azd R2 and R4 update

This feature branch adds the secure read-only API, branches, reception desks, inventory assignment and audited transfers. It integrates main through `0ed3060` (kiosk, importer and reports).

- [R2/R4 installation and running](Docs/Azd/INSTALLATION.md)
- [API and branch integration](Docs/Azd/AZD-R2-R4.md)
- [Automated test evidence and remaining checks](Docs/Azd/TEST-EVIDENCE.md)

Peer review and independent SQL Server/VM verification are still required. Do not commit the API key.

## Team and Responsibilities

| Member | Main responsibilities |
|---|---|
| Azad Howlader | Branch inventory, transfers, secure API, database integration, installation guide and project planning |
| Asraful Hasan Asif | Kiosk, barcode input, borrower identification, session handling, accessible interface, testing and patron guide |
| Safiyan Ahmed | Reservations, waitlists, automated notifications, testing and documentation |
| Malia Makisi | CSV import, analytics, exports, format and access information, VM testing and portfolio assembly |

Everyone contributes to testing, documentation and peer review.

## Features

### Public Catalogue

Visitors can search without signing in.

- Search by item name, description or library code.
- Filter by collection: books, music or toys.
- Filter by availability status.
- View item descriptions and collection-specific details.
- Open the self-service kiosk.
- Browse using a responsive interface.

### Self-Service Kiosk

The dedicated `/Kiosk` interface allows patrons to:

- Enter a library card code.
- Use a compatible USB barcode scanner to enter codes into the selected field.
- View active loans, return dates and unpaid assessed fines.
- Enter an item library code.
- Review the item and return date before borrowing.
- Confirm borrowing and view the result.
- Finish the session.

Additional interface controls include:

- Larger text.
- High contrast.
- Reset display.
- Visible keyboard focus.
- Skip links.
- Inactivity warning and session extension.
- Automatic sign-out after inactivity.

### Administrator

- Manage books, music and toys.
- Maintain item-specific details.
- Search and filter inventory.
- Prevent duplicate library codes.

### Reception

- Manage borrowers.
- Identify borrowers by card code.
- Issue available items.
- Process returns.
- View active, overdue and returned loans.
- Assess late fines and record payments.

### Manager

- View borrowing and inventory statistics.
- Review borrower totals.
- View assessed, paid and outstanding fines.
- Monitor availability and return statistics.

### Group Expansion Scope

| Requirement | Module |
|---|---|
| R1 | Public self-service kiosk |
| R2 | Secure read-only catalogue API |
| R3 | Automated simulated email and SMS notifications |
| R4 | Multi-branch inventory and transfers |
| R5 | Digital reservations and waitlist queue |
| R6 | Bulk catalogue importer |
| R7 | Manager analytics and exports |

Feature availability depends on the branch and merged changes. This table identifies the group requirements; the final portfolio and module tests document their implementation.

## Technology

- ASP.NET Core MVC and Razor Views
- .NET 10 and C#
- Entity Framework Core
- SQL Server LocalDB
- ASP.NET Core Identity
- Bootstrap
- JavaScript
- LINQ and Dependency Injection

## Getting Started

### 1. Install the Prerequisites

You need:

- Git
- .NET 10 SDK
- SQL Server Express LocalDB for the default Windows setup
- Network access for NuGet package restore

If using Visual Studio, use a version that supports .NET 10 with the ASP.NET and web development tools installed. The CLI commands below can also run the application independently of Visual Studio.

Check the tools:

```powershell
git --version
dotnet --list-sdks
sqllocaldb info
```

### 2. Clone the Repository

```powershell
git clone https://github.com/azadofficial1911-coder/hit339-group11-library-system.git
cd hit339-group11-library-system
```

For the integrated application:

```powershell
git switch main
git pull
```

To review Asraful's kiosk branch:

```powershell
git switch feature/asraful-kiosk
git pull
```

A Git development branch is different from a physical library location.

### 3. Check the Database Configuration

Open:

```text
LibraryManagementSystem/appsettings.json
```

Check that the database connection string points to an available SQL Server or LocalDB instance.

Keep private credentials in local configuration, environment variables or user secrets.

### 4. Restore and Build

From the repository root:

```powershell
dotnet restore LibraryManagementSystem
dotnet build LibraryManagementSystem
```

Resolve build errors before continuing.

### 5. Apply Database Migrations

In Visual Studio:

1. Open the solution.
2. Set LibraryManagementSystem as the startup project.
3. Open Tools → NuGet Package Manager → Package Manager Console.
4. Select LibraryManagementSystem as the default project.
5. Run:

```powershell
Update-Database -Context ApplicationDbContext
```

Apply the migrations included with the checked-out branch.

### 6. Enable Optional Demo Data

In the existing `appsettings.json`, set:

```json
"DemoData": {
  "SeedReservationScenario": true
}
```

Preserve the other settings and valid JSON formatting.

The optional seeder is:

```text
LibraryManagementSystem/Data/ReservationDemoSeeder.cs
```

### 7. Run the Application

```powershell
dotnet run --project LibraryManagementSystem
```

Open the URL printed beside `Now listening on`.

The screenshots in the patron guide use:

```text
https://localhost:7016/
```

Your port may differ. Always use the address printed by your running application.

If a local HTTPS development certificate needs configuring:

```powershell
dotnet dev-certs https --trust
```

Keep the application running while using the website.

## Staff Login

The baseline `IdentitySeeder` creates these demonstration accounts:

| Role | Email | Password |
|---|---|---|
| Admin | admin@librarydemo.com | Library123! |
| Reception | reception@librarydemo.com | Library123! |
| Manager | manager@librarydemo.com | Library123! |

### Login Steps

1. Start the application.
2. Open the website.
3. Select **Login** in the top navigation.
4. Enter the email and password for the required role.
5. Select **Log in**.
6. Open the corresponding dashboard from the navigation.
7. Select **Logout** when finished.

Use the **Reception** account for borrower management, loan issuing and returns.

Each role has separate permissions. An Admin account does not automatically have Reception or Manager permissions.

If an account password has been changed locally, use its current password.

These credentials are for demonstration only. Replace default credentials before deploying with real users or data.

## Kiosk Account Access

Kiosk access is separate from staff login.

1. Select **Self-service Kiosk**.
2. Select the **Library card code** field.
3. Enter a valid borrower code or scan a compatible library card.
4. Select **Open my account**.

With demo data enabled, use:

| Borrower code | Sample borrower |
|---|---|
| BR-DEMO1 | Demo Borrower One |
| BR-DEMO2 | Demo Borrower Two |
| BR-DEMO3 | Demo Borrower Three |

You do not enter a staff email or password on the kiosk card-entry screen.

Borrower codes identify demonstration accounts. They should not be treated as strong authentication for a public production service.

## Borrow an Item Through the Kiosk

1. Open your kiosk account.
2. Select **Borrow an item**.
3. Enter the item's **library code**.
4. Select **Review this item**.
5. Check the item name, library code and return date.
6. Select **Confirm borrowing**.
7. Check the success message and updated loan list.
8. Select **Finish session** before leaving.

Use the library's item label, not the book ISBN.

A compatible USB scanner can enter a code into the focused input field. Physical scanner behaviour must be checked with the actual device and configuration.

### Example Demo Items

| Code | Item | Collection |
|---|---|---|
| BK-DEMO-01 | Beneath the Darwin Sky | Book |
| BK-DEMO-02 | The Quiet Library | Book |
| MU-DEMO-01 | Golden Hour Sessions | Music |
| TY-DEMO-01 | Rainbow Building Blocks | Toy |

The expanded seeder adds:

- Six books: `BK-DEMO-01` to `BK-DEMO-06`
- Six music items: `MU-DEMO-01` to `MU-DEMO-06`
- Six toys: `TY-DEMO-01` to `TY-DEMO-06`

These are fictional demonstration records. Newly inserted items begin as Available.

Existing records are left unchanged. Restarting does not reset an existing item's loan status.

If an example is already borrowed, choose another available item or return it through the authorised staff workflow.

### Reservation and Reminder Demo Records

The optional seeder also includes:

| Code | Scenario |
|---|---|
| BK-R5-01 | Borrowed item for reservation queue testing |
| BK-R5-02 | Damaged item for reservation testing |
| BK-R3-DUE | Loan approaching its due date |
| BK-R3-OVER | Overdue loan |

Initial scenario dates are calculated relative to seeding. Existing active loans are not refreshed on every restart.

## Display Options and Sessions

The kiosk provides:

- **Larger text:** increases the base text size.
- **High contrast:** uses black panels, white text and yellow controls.
- **Reset display:** restores the default display settings.

The interface includes visible focus indicators and reduced-motion styling.

On the current account and borrowing pages:

- An inactivity warning appears after approximately 60 seconds.
- The session finishes after approximately 90 seconds without activity.
- The warning provides an option to continue.
- Continuing from Borrow returns to item entry.

After sign-out, account and borrowing pages require a valid kiosk session before displaying borrower information.

## Main Routes

| Page | Route |
|---|---|
| Homepage | `/` |
| Public catalogue | `/Catalogue` |
| Kiosk welcome | `/Kiosk` |
| Kiosk account | `/Kiosk/Account` |
| Kiosk borrowing | `/Kiosk/Borrow` |
| Admin dashboard | `/Admin` |
| Reception dashboard | `/Reception` |
| Manager dashboard | `/Manager` |
| Borrowers | `/Borrowers` |
| Loans | `/Loans` |
| Reservations | `/Holds` |
| Notification log | `/Notifications` |

Staff routes require the corresponding role. Kiosk account routes require a valid borrower session.

Use the integrated module documentation for API endpoints, import, transfer and export instructions.

## Multi-Branch Setup

Physical library locations are handled by the group's multi-branch module.

Use the integrated installation instructions to:

1. Seed physical branch records.
2. Associate reception desks with a branch.
3. Select the required location.
4. Check item location and availability.
5. Exercise transfer dispatch and receipt.

Do not substitute a Git feature branch name for a physical library location.

For the actual seed, branch filter, desk association, transfer lifecycle and SQL Server instructions, follow [the R2/R4 installation guide](Docs/Azd/INSTALLATION.md).

## Application Structure

The application follows the Model-View-Controller pattern.

| Component | Purpose |
|---|---|
| Models | Define items, borrowers, loans and other library records |
| Views | Display public, kiosk and staff pages |
| Controllers | Handle requests and coordinate behaviour |
| ViewModels | Define submitted and displayed information |
| Services | Provide reusable business logic |
| ApplicationDbContext | Manage Entity Framework database access |

`Item` is the base class for `Book`, `Music` and `Toy`.

Entity Framework uses Table-per-Hierarchy inheritance with an `ItemType` discriminator. Each loan connects one item with one borrower.

The baseline includes a filtered unique index intended to prevent multiple active loans for the same item.

## Fine Handling

The baseline configuration uses:

- Daily late fee: **$2.00**
- Maximum fine: **$100.00**

Check the current `appsettings.json` for the configured values.

`FineCalculator` implements `IFineCalculator` and is supplied through Dependency Injection.

Unpaid assessed fines and payments received are different values. Reports should distinguish outstanding charges from fine revenue.

## Security

Security controls include:

- ASP.NET Core Identity for staff accounts.
- Server-side role authorization.
- Antiforgery validation on state-changing forms.
- Server-side input validation.
- ViewModels and controlled model binding.
- Duplicate-code protection.
- Parameterised Entity Framework queries.
- Public catalogue information that excludes patron details.
- Kiosk session checks and explicit sign-out.

Do not commit private credentials, API keys or real patron information.

## Documentation

Asraful's documents are stored in:

```text
Docs/Asraful/
```

They include:

- **Patron User Guide:** screenshots and instructions for searching, account access, borrowing, sign-out and display options.
- **Kiosk Journeys and Test Record:** patron journeys, observed results and evidence references.
- **Strengths and Weaknesses:** individual contribution reflection.

Use the latest corrected document versions in this folder.

The final group portfolio also includes:

- Team contract
- Requirements and notes
- Team approach and project plan
- Task allocations and hours
- Meeting minutes
- Team strengths and weaknesses
- Installation and running instructions
- Integration diagrams and user journey maps
- Testing evidence

## Verification

The supplied local screenshots demonstrate:

- Public catalogue browsing while logged out.
- Search by item library code.
- Demo borrower account access.
- Account loan and fine summaries.
- Item review and proposed return date.
- Successful borrowing confirmation.
- Updated active-loan count.
- Sign-out confirmation.
- Redirect to card entry without a valid account session.
- Corrected high-contrast text on the welcome page.

Refer to the test record for the scope and evidence of each check.

Record integration and environment results separately. The existence of code or a screenshot does not establish that every device, accessibility scenario or deployment environment has passed testing.

### Integration Test Areas

- Consistent rules for staff and kiosk borrowing.
- Prevention of conflicting active loans or reservations.
- Notifications following borrowing.
- Reservation allocation following returns or repairs.
- Scheduled reminders without duplicate messages.
- Correct inventory location during transfers.
- API authentication and rejection of writes.
- CSV validation and duplicate handling.
- Report totals matching their filters and exports.
- Separation of assessed fines and payments received.

The group installation guide records the CDU VM setup and results required by the assignment.

## Troubleshooting

### Localhost Refuses to Connect

Make sure the application is still running.

Use the exact listening address printed by the terminal or Visual Studio.

### Page Not Found

Check the route and port.

Press **Ctrl + L** before pasting a full address so it replaces the current URL.

### Database Connection Error

Check the SQL Server instance, connection string and applied migrations.

### Demo Items Missing

Confirm that:

- `SeedReservationScenario` is true.
- The updated seeder exists on the current branch.
- The application has been restarted.
- Catalogue search filters have been cleared.

### Item Already Borrowed

Choose another available item or process a return through Reception.

Restarting does not clear active loans.

### Display Changes Not Appearing

Save the files and press **Ctrl + F5** in the browser.

Restart the application if changes to Razor views have not been applied.

### Kiosk Session Has Ended

Return to the welcome screen and enter the borrower card code again.

## Development Workflow

1. Work on an assigned feature branch.
2. Keep changes focused.
3. Build and test the affected behaviour.
4. Commit with a clear message.
5. Push the branch.
6. Open or update a pull request.
7. Include test evidence and request peer review.
8. Merge after the agreed checks pass.

Keep estimated task hours separate from actual time worked.

## Submission

The group submits the final portfolio PDF and repository link through the assessment submission point.

The portfolio should identify the final code revision and include working instructions, module documentation and supporting evidence.

All four members review the combined submission before upload.
