using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.RegularExpressions;

namespace LibraryManagementSystem.Tests;

public class TestHost : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-only-key-0123456789-abcdefghijklmnopqrstuvwxyz";
    private readonly SqliteConnection? connection;
    private readonly string? sqlTestConnection;
    public TestHost()
    {
        var supplied = Environment.GetEnvironmentVariable("AZD_TEST_SQLSERVER");
        if (!string.IsNullOrWhiteSpace(supplied)) {
            // Always use a newly named disposable database, never the supplied catalog.
            var config = new SqlConnectionStringBuilder(supplied) {
                InitialCatalog = "LibraryR2R4Tests_" + Guid.NewGuid().ToString("N")
            };
            sqlTestConnection = config.ConnectionString;
        } else {
            connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
        }
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        ConfigureDatabase(options);
        using var db = new ApplicationDbContext(options.Options);
        if (sqlTestConnection != null) db.Database.Migrate();
        else db.Database.EnsureCreated();
    }
    private void ConfigureDatabase(DbContextOptionsBuilder options) {
        if (sqlTestConnection != null) options.UseSqlServer(sqlTestConnection);
        else options.UseSqlite(connection!);
    }
    public bool EnableApi { get; set; } = true;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?> {
            ["CatalogueApi:Key"] = EnableApi ? ApiKey : "",
            ["DemoData:SeedReservationScenario"] = "false",
            ["DemoData:SeedBranchScenario"] = "false"
        }));
        builder.ConfigureTestServices(services => {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(ConfigureDatabase);
            var background = services.SingleOrDefault(d => d.ServiceType == typeof(IHostedService)
                && d.ImplementationType == typeof(NotificationBackgroundService));
            if (background != null) services.Remove(background);
        });
    }
    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
    });
    public IServiceScope Scope() { _ = Services; return Services.CreateScope(); }
    public static string Token(string html) => WebUtility.HtmlDecode(
        Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    public async Task<HttpClient> Login(string role)
    {
        var client = Client();
        var html = await client.GetStringAsync("/Identity/Account/Login");
        var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string> {
            ["Input.Email"] = role + "@librarydemo.com", ["Input.Password"] = "Library123!",
            ["__RequestVerificationToken"] = Token(html)
        }));
        if (response.StatusCode != HttpStatusCode.Redirect) throw new Exception("Test login failed: " + await response.Content.ReadAsStringAsync());
        return client;
    }
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing) return;
        if (sqlTestConnection != null) {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>(); ConfigureDatabase(options);
            using var db = new ApplicationDbContext(options.Options);
            db.Database.EnsureDeleted();
        }
        connection?.Dispose();
    }
}
