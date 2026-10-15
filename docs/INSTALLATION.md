# Installation and running

These instructions cover Azd's R2/R4 branch. They extend the original Assignment 2 guide. Windows SQL Server LocalDB and the university VM still require independent verification; the automated test record explains what was run here.

## Prerequisites

Install a current .NET 10 SDK, Git, and Visual Studio with ASP.NET and web development support if using the IDE. Use an IDE version that supports .NET 10. Install SQL Server Express LocalDB on Windows, or use an accessible SQL Server instance with database creation permissions. LocalDB does not run on Linux.

From the repository root, in PowerShell:

```powershell
dotnet --info
dotnet restore
dotnet build -m:1
dotnet tool restore
```

If no tool manifest is present, install the local EF tool once:

```powershell
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.10
```

## Configure and create the database

The baseline `appsettings.json` contains a LocalDB development connection. To use another instance, configure a user secret; keep real credentials out of Git:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=Group11Library;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project LibraryManagementSystem
dotnet ef database update --project LibraryManagementSystem
```

The added migration sequence is `AddBranchesAndReceptionDesks`, then `AddItemTransfers`. Existing items keep a nullable BranchId, so the upgrade does not invent locations or delete baseline data. Apply the migrations before running, because startup seeds the existing demonstration accounts. In Visual Studio Package Manager Console, use `Update-Database` with LibraryManagementSystem selected.

To inspect the SQL without changing a database:

```powershell
dotnet ef migrations has-pending-model-changes --project LibraryManagementSystem
dotnet ef migrations script --idempotent --project LibraryManagementSystem --output schema.sql
```

## Optional branch demonstration data

```powershell
dotnet user-secrets set "DemoData:SeedBranchScenario" "true" --project LibraryManagementSystem
```

On the next start this adds Darwin, Casuarina and Palmerston branches, one reception desk and one book at each. Names, addresses, hours and facilities are explicitly illustrative. Repeated starts do not duplicate them or reset their current location/status. Existing items are not silently assigned. Disable the flag afterwards if desired; records remain.

## Configure the read-only API

Generate a private random key of at least 32 characters. In PowerShell:

```powershell
$apiKey = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "CatalogueApi:Key" "$apiKey" --project LibraryManagementSystem
```

Do not paste the key into screenshots, commits or the portfolio. Environment variable `CatalogueApi__Key` is also supported. An unset or shorter key leaves all API endpoints closed. API requests require HTTPS and the `X-Api-Key` header; a staff login alone does not grant API access. Restart after changing configuration. `CatalogueApi:Maintenance=true` changes the reported operating status.

## Start and choose a physical branch

```powershell
dotnet dev-certs https --trust
dotnet run --project LibraryManagementSystem --launch-profile https
```

Use the HTTPS URL printed by the application. Login with the baseline classroom-only accounts: `admin@librarydemo.com`, `reception@librarydemo.com`, or `manager@librarydemo.com`, password `Library123!`. Do not expose these demonstration accounts in a public deployment.

Admin navigation exposes Branches and Reception desks. Admin and Reception see Branch stock and transfers (`/BranchInventory`). Select a physical branch using the View branch filter. This is a view of shared records, not a separate Git branch or database. Repeat with another physical location for the VM evidence.

Admin can assign an available unassigned copy to a branch. Subsequent location changes must use Dispatch and Confirm receipt. See the design guide for the demonstration.

## Test on the laptop or VM

Default tests use an isolated SQLite database and the real MVC/Identity pipeline:

```powershell
dotnet test -m:1 --logger "trx;LogFileName=azd-r2-r4.trx"
```

To verify the same suite against actual SQL Server migrations:

```powershell
$env:AZD_TEST_SQLSERVER = "Server=(localdb)\mssqllocaldb;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test -m:1 --logger "trx;LogFileName=azd-r2-r4-sqlserver.trx"
Remove-Item Env:AZD_TEST_SQLSERVER
```

This optional mode creates a uniquely named `LibraryR2R4Tests_<guid>` database for each test, applies all migrations and removes only that generated test database afterwards. It ignores any Database value in the supplied connection string. The account needs create/drop database permissions. It never targets the application's data database.

## Troubleshooting

- Missing SDK: check `dotnet --info` for a .NET 10 SDK.
- Cannot open database or invalid object name: check the connection string and run `dotnet ef database update` before startup.
- LocalDB unsupported: use Windows LocalDB or configure an actual SQL Server instance on the VM.
- API 401: use HTTPS, check the header and secret length, then restart. Cookie login is deliberately separate.
- API 405: the API supports GET only.
- No API items: available copies must be assigned to an active branch, with no active loan, hold or dispatched transfer.
- Transfer refused: check item state, active branches, existing loans/holds, and distinct source/destination. Reload after concurrent changes.
- Package/build failure on this constrained environment: `dotnet restore --disable-parallel -m:1` and `dotnet build -m:1` avoid parallel worker startup issues.

For independent VM evidence, record the actual OS, SDK/database versions, commands, selected branch, output and screenshots. Do not mark VM installation as passed until it is performed there.
