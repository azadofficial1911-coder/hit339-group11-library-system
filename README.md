# Group 11 Library Management System

HIT339 Distributed Development. ASP.NET Core MVC on .NET 10 with SQL Server, Identity, catalogue, borrowing, reservations and simulated notifications.

Azd's R2/R4 feature branch adds a secure read-only catalogue API, physical branches, reception desks, inventory assignment and audited dispatch/receipt transfers. Every item record represents one physical copy.

- [Installation and running](docs/INSTALLATION.md)
- [R2/R4 design, API and integration](docs/AZD-R2-R4.md)
- [Test evidence and remaining checks](docs/TEST-EVIDENCE.md)
- [Assignment 2 baseline guide](LibraryManagementSystem/README.md)

Run `dotnet restore`, `dotnet build -m:1` and `dotnet test -m:1` from the repository root. Apply EF migrations before starting the application; see the installation guide. Never commit the catalogue API key.

This branch is a review candidate. Peer review, independent VM installation and the final portfolio remain separate team deliverables.
