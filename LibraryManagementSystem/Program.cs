using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Read the SQL Server connection string.
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found."
    );

// Register Entity Framework and the application database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure ASP.NET Core Identity, password security and roles.
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Register MVC controllers and Razor views.
builder.Services.AddControllersWithViews();

// Load the fine rules from appsettings.json.
builder.Services.Configure<FineSettings>(
    builder.Configuration.GetSection(FineSettings.SectionName)
);

// Register the fine calculator using Dependency Injection.
builder.Services.AddScoped<IFineCalculator, FineCalculator>();

// R3 and R5: reservation queue and simulated notifications.
builder.Services.Configure<ReservationSettings>(
    builder.Configuration.GetSection(ReservationSettings.SectionName)
);

builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection(NotificationSettings.SectionName)
);

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReservationService, ReservationService>();

// Checks for due-soon and overdue reminders while the app is running.
builder.Services.AddHostedService<NotificationBackgroundService>();

// Register the CSV import preview service.
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ReportService>();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var app = builder.Build();

// Create the roles and demonstration accounts.
await IdentitySeeder.SeedAsync(app.Services);

// Optional demo records for testing reservations and notifications.
if (app.Configuration.GetValue<bool>("DemoData:SeedReservationScenario"))
{
    await ReservationDemoSeeder.SeedAsync(app.Services);
}

// Configure error handling.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Configure the standard MVC route.
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"
    )
    .WithStaticAssets();

// Configure the Identity Razor Pages.
app.MapRazorPages()
    .WithStaticAssets();

app.Run();