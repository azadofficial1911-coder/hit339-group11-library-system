using System.Net;
using System.Text.Json;
using LibraryManagementSystem.Data;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LibraryManagementSystem.Tests;

public class ModuleTests : IDisposable
{
    private readonly TestHost host = new();
    public void Dispose() => host.Dispose();
    private async Task<(int item, int source, int destination)> Stock(ItemStatus status = ItemStatus.Available, bool assigned = true)
    {
        using var scope = host.Scope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var a = new Branch { Code = "A", Name = "Source", Address = "Demo source" };
        var b = new Branch { Code = "B", Name = "Destination", Address = "Demo destination" };
        db.Branches.AddRange(a, b);
        await db.SaveChangesAsync();
        var book = new Book { Name = "Test copy", LibraryCode = "COPY-1", Description = "Test description", Author = "Test Author", Genre = "Test", PublicationYear = 2026, Status = status, BranchId = assigned ? a.Id : null };
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return (book.Id, a.Id, b.Id);
    }
    private async Task<TransferResult> Dispatch(int item, int destination)
    {
        using var scope = host.Scope();
        return await scope.ServiceProvider.GetRequiredService<TransferService>().DispatchAsync(item, destination, "Test staff");
    }
    [Theory]
    [InlineData("items")][InlineData("categories")][InlineData("status")]
    public async Task Api_requires_key(string route)
    {
        using var client = host.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalogue/" + route)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalogue/" + route)).StatusCode);
    }
    [Fact]
    public async Task Api_fails_closed_when_unconfigured()
    {
        host.EnableApi = false;
        using var client = host.Client(); client.DefaultRequestHeaders.Add("X-Api-Key", TestHost.ApiKey);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalogue/items")).StatusCode);
    }
    [Theory]
    [InlineData("items")][InlineData("categories")][InlineData("status")]
    public async Task Api_authorised_gets_work(string route)
    {
        await Stock();
        using var client = host.Client(); client.DefaultRequestHeaders.Add("X-Api-Key", TestHost.ApiKey);
        var response = await client.GetAsync("/api/catalogue/" + route);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("borrower", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("librarydemo.com", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData("POST")][InlineData("PUT")][InlineData("PATCH")][InlineData("DELETE")]
    public async Task Api_rejects_writes(string method)
    {
        using var client = host.Client(); client.DefaultRequestHeaders.Add("X-Api-Key", TestHost.ApiKey);
        foreach (var route in new[] { "items", "categories", "status" }) {
            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/api/catalogue/" + route));
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
    [Theory]
    [InlineData("page=0")][InlineData("pageSize=101")][InlineData("branchId=-1")][InlineData("page=2147483647")]
    public async Task Api_validates_filters(string filter)
    {
        using var client = host.Client(); client.DefaultRequestHeaders.Add("X-Api-Key", TestHost.ApiKey);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/catalogue/items?" + filter)).StatusCode);
    }
    [Fact]
    public async Task Staff_cookie_does_not_grant_api_access()
    {
        using var client = await host.Login("admin");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/catalogue/items")).StatusCode);
    }
    [Fact]
    public async Task Dispatch_and_receipt_move_one_copy_and_api_availability()
    {
        var (item, a, b) = await Stock();
        using var client = host.Client(); client.DefaultRequestHeaders.Add("X-Api-Key", TestHost.ApiKey);
        Assert.Contains("COPY-1", await client.GetStringAsync($"/api/catalogue/items?branchId={a}"));
        Assert.True((await Dispatch(item, b)).Success);
        Assert.DoesNotContain("COPY-1", await client.GetStringAsync("/api/catalogue/items"));
        Assert.False((await Dispatch(item, b)).Success);
        using (var scope = host.Scope()) {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stock = await db.Items.SingleAsync(); Assert.Equal(a, stock.BranchId); Assert.Equal(ItemStatus.InTransit, stock.Status);
            var t = await db.ItemTransfers.SingleAsync();
            var svc = scope.ServiceProvider.GetRequiredService<TransferService>();
            Assert.False((await svc.ReceiveAsync(t.Id, a, "Staff")).Success);
            Assert.True((await svc.ReceiveAsync(t.Id, b, "Receiving staff")).Success);
            Assert.False((await svc.ReceiveAsync(t.Id, b, "Receiving staff")).Success);
            Assert.Equal(b, stock.BranchId); Assert.Equal(ItemStatus.Available, stock.Status);
            Assert.NotNull(t.ReceivedAt); Assert.Equal("Receiving staff", t.ReceivedBy);
            Assert.Equal(1, await db.Items.CountAsync());
        }
        Assert.DoesNotContain("COPY-1", await client.GetStringAsync($"/api/catalogue/items?branchId={a}"));
        Assert.Contains("COPY-1", await client.GetStringAsync($"/api/catalogue/items?branchId={b}"));
    }
    [Theory]
    [InlineData(ItemStatus.Borrowed)][InlineData(ItemStatus.Damaged)][InlineData(ItemStatus.Reserved)][InlineData(ItemStatus.Destroyed)][InlineData(ItemStatus.InTransit)]
    public async Task Unavailable_copies_cannot_transfer(ItemStatus status)
    {
        var (item, _, b) = await Stock(status);
        Assert.False((await Dispatch(item, b)).Success);
    }
    [Fact]
    public async Task Reject_same_missing_inactive_branches_and_unassigned_stock()
    {
        var (item, a, b) = await Stock();
        Assert.False((await Dispatch(item, a)).Success);
        Assert.False((await Dispatch(item, 9999)).Success);
        using (var scope = host.Scope()) {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Branches.FindAsync(b))!.IsActive = false; await db.SaveChangesAsync();
        }
        Assert.False((await Dispatch(item, b)).Success);
    }
    [Fact]
    public async Task Assignment_only_applies_to_unassigned_available_stock()
    {
        var (item, a, b) = await Stock(assigned: false);
        Assert.False((await Dispatch(item, b)).Success);
        using var scope = host.Scope(); var svc = scope.ServiceProvider.GetRequiredService<TransferService>();
        Assert.False((await svc.AssignAsync(item, 9999)).Success);
        Assert.True((await svc.AssignAsync(item, a)).Success);
        Assert.False((await svc.AssignAsync(item, b)).Success);
    }
    [Fact]
    public async Task Concurrent_stale_item_change_is_rejected()
    {
        var (item, _, b) = await Stock();
        using var staleScope = host.Scope(); var staleDb = staleScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stale = await staleDb.Items.FindAsync(item);
        Assert.True((await Dispatch(item, b)).Success);
        stale!.Status = ItemStatus.Borrowed;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync());
    }
    [Fact]
    public async Task Reservation_service_cannot_release_in_transit_item()
    {
        var (item, _, b) = await Stock(); await Dispatch(item, b);
        using var scope = host.Scope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await scope.ServiceProvider.GetRequiredService<IReservationService>().OnItemAvailableAsync(item);
        Assert.Equal(ItemStatus.InTransit, (await db.Items.FindAsync(item))!.Status);
    }
    [Fact]
    public async Task Admin_pages_render_and_missing_fields_do_not_crash()
    {
        using var client = await host.Login("admin");
        foreach (var path in new[] { "/Branches", "/Branches/Create", "/ReceptionDesks", "/ReceptionDesks/Create", "/BranchInventory", "/Books", "/Musics", "/Toys", "/Catalogue" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        var html = await client.GetStringAsync("/Branches/Create");
        var response = await client.PostAsync("/Branches/Create", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = TestHost.Token(html), ["IsActive"] = "true"
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("required", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task Staff_actions_require_role_and_antiforgery()
    {
        using var anonymous = host.Client();
        Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync("/BranchInventory")).StatusCode);
        using var manager = await host.Login("manager");
        var denied = await manager.GetAsync("/BranchInventory");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("AccessDenied", denied.Headers.Location!.ToString());
        using var reception = await host.Login("reception");
        Assert.Equal(HttpStatusCode.OK, (await reception.GetAsync("/BranchInventory")).StatusCode);
        Assert.Contains("AccessDenied", (await reception.GetAsync("/Branches")).Headers.Location!.ToString());
        Assert.Contains("AccessDenied", (await reception.GetAsync("/ReceptionDesks")).Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await reception.PostAsync("/BranchInventory/Dispatch", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
    }
    [Fact]
    public async Task Branch_demo_seed_is_repeatable()
    {
        _ = host.Services;
        await BranchDemoSeeder.SeedAsync(host.Services); await BranchDemoSeeder.SeedAsync(host.Services);
        using var scope = host.Scope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(3, await db.Branches.CountAsync()); Assert.Equal(3, await db.ReceptionDesks.CountAsync());
        Assert.Equal(3, await db.Items.CountAsync());
        Assert.All(await db.Branches.ToListAsync(), b => Assert.Contains("Demo", b.Name));
    }
    [Fact]
    public void Sql_server_migrations_match_model_and_generate_script()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=ScriptOnly;Trusted_Connection=True;TrustServerCertificate=True").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE [ItemTransfers]", script); Assert.Contains("CREATE TABLE [ReceptionDesks]", script);
        Assert.Contains("Dispatched", script);
    }

    private static Borrower Patron() => new() { BorrowerCode = "PATRON-TEST", FullName = "Private test patron",
        Email = "private-patron@example.invalid", PhoneNumber = "0400000000", Address = "Private test address" };

    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task Active_loan_or_hold_blocks_dispatch_and_api_even_with_stale_available_status(bool loan)
    {
        var (item, _, b) = await Stock();
        using (var scope = host.Scope()) {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var patron = Patron(); db.Borrowers.Add(patron); await db.SaveChangesAsync();
            if (loan) db.Loans.Add(new Loan { ItemId = item, BorrowerId = patron.Id });
            else db.Reservations.Add(new Reservation { ItemId = item, BorrowerId = patron.Id });
            await db.SaveChangesAsync();
        }
        Assert.False((await Dispatch(item, b)).Success);
        using var client = host.Client(); client.DefaultRequestHeaders.Add("X-Api-Key", TestHost.ApiKey);
        var body = await client.GetStringAsync("/api/catalogue/items");
        Assert.DoesNotContain("COPY-1", body); Assert.DoesNotContain("private-patron", body);
        Assert.DoesNotContain("Private test address", body);
    }
    [Fact]
    public async Task Hold_created_during_transit_is_only_promoted_on_receipt()
    {
        var (item, _, b) = await Stock(); Assert.True((await Dispatch(item, b)).Success);
        using var scope = host.Scope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var patron = Patron(); db.Borrowers.Add(patron); await db.SaveChangesAsync();
        var reservations = scope.ServiceProvider.GetRequiredService<IReservationService>();
        var hold = await reservations.PlaceHoldAsync("COPY-1", patron.BorrowerCode, "Staff");
        Assert.True(hold.Succeeded);
        await reservations.OnItemAvailableAsync(item);
        Assert.Equal(ReservationStatus.Waiting, (await db.Reservations.SingleAsync()).Status);
        var transfer = await db.ItemTransfers.SingleAsync();
        Assert.True((await scope.ServiceProvider.GetRequiredService<TransferService>().ReceiveAsync(transfer.Id, b, "Staff")).Success);
        Assert.Equal(ItemStatus.Reserved, (await db.Items.FindAsync(item))!.Status);
        Assert.Equal(ReservationStatus.Ready, (await db.Reservations.SingleAsync()).Status);
        Assert.NotEmpty(await db.NotificationLogs.ToListAsync());
    }
    [Fact]
    public async Task Reception_can_dispatch_and_receive_through_antiforgery_protected_forms()
    {
        var (item, _, b) = await Stock();
        using var client = await host.Login("reception");
        var html = await client.GetStringAsync("/BranchInventory");
        Assert.Contains("COPY-1", html);
        var response = await client.PostAsync("/BranchInventory/Dispatch", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = TestHost.Token(html), ["itemId"] = item.ToString(), ["destinationId"] = b.ToString()
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        int transferId;
        using (var scope = host.Scope()) {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); transferId = (await db.ItemTransfers.SingleAsync()).Id;
            Assert.Equal(ItemStatus.InTransit, (await db.Items.FindAsync(item))!.Status);
        }
        html = await client.GetStringAsync("/BranchInventory"); Assert.Contains("In transit", html);
        Assert.Contains("Confirm receipt", html);
        response = await client.PostAsync("/BranchInventory/Receive", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = TestHost.Token(html), ["id"] = transferId.ToString(), ["receivingBranchId"] = b.ToString()
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        html = await client.GetStringAsync("/BranchInventory"); Assert.Contains("Completed", html);
        using var finalScope = host.Scope(); var finalDb = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(b, (await finalDb.Items.FindAsync(item))!.BranchId);
    }
    [Fact]
    public async Task Branch_and_desk_forms_save_validate_duplicates_and_protect_deactivation()
    {
        using var client = await host.Login("admin");
        async Task<HttpResponseMessage> Form(string get, string post, Dictionary<string,string> fields) {
            fields["__RequestVerificationToken"] = TestHost.Token(await client.GetStringAsync(get));
            return await client.PostAsync(post, new FormUrlEncodedContent(fields));
        }
        var fields = new Dictionary<string,string> { ["Code"] = " abc ", ["Name"] = "Test branch", ["Address"] = "Demo street", ["IsActive"] = "true" };
        Assert.Equal(HttpStatusCode.Redirect, (await Form("/Branches/Create", "/Branches/Create", fields)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Form("/Branches/Create", "/Branches/Create", fields)).StatusCode);
        int id;
        using (var scope = host.Scope()) {
            var branch = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Branches.SingleAsync();
            id = branch.Id; Assert.Equal("ABC", branch.Code);
        }
        var desk = new Dictionary<string,string> { ["Code"] = "desk-1", ["Name"] = "Test desk", ["BranchId"] = id.ToString(), ["IsActive"] = "true" };
        Assert.Equal(HttpStatusCode.Redirect, (await Form("/ReceptionDesks/Create", "/ReceptionDesks/Save", desk)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Form("/ReceptionDesks/Create", "/ReceptionDesks/Save", desk)).StatusCode);
        fields["Id"] = id.ToString(); fields["IsActive"] = "false";
        var blocked = await Form($"/Branches/Edit/{id}", $"/Branches/Edit/{id}", fields);
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        Assert.Contains("deactivate desks", await blocked.Content.ReadAsStringAsync());
        using var finalScope = host.Scope();
        Assert.True((await finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Branches.SingleAsync()).IsActive);
    }
    [Fact]
    public async Task In_transit_copy_cannot_be_borrowed_or_changed_through_book_editor()
    {
        var (item, _, b) = await Stock(); await Dispatch(item, b);
        using var reception = await host.Login("reception");
        var html = await reception.GetStringAsync("/Loans/Borrow");
        var response = await reception.PostAsync("/Loans/Borrow", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = TestHost.Token(html), ["LibraryCode"] = "COPY-1", ["BorrowerCode"] = "TEST",
            ["DueDate"] = DateTime.Today.AddDays(14).ToString("yyyy-MM-dd")
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("cannot be borrowed", await response.Content.ReadAsStringAsync());
        using var admin = await host.Login("admin"); html = await admin.GetStringAsync($"/Books/Edit/{item}");
        response = await admin.PostAsync($"/Books/Edit/{item}", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["__RequestVerificationToken"] = TestHost.Token(html), ["Id"] = item.ToString(), ["Name"] = "Test copy",
            ["Description"] = "Test description", ["LibraryCode"] = "COPY-1", ["Status"] = "Available",
            ["Author"] = "Test Author", ["Genre"] = "Test", ["PublicationYear"] = "2026"
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Use Transfers", await response.Content.ReadAsStringAsync());
        using var scope = host.Scope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(ItemStatus.InTransit, (await db.Items.FindAsync(item))!.Status); Assert.Empty(await db.Loans.ToListAsync());
    }
}
