using LibraryManagementSystem.Data;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database configuration.
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Authentication and roles.
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

// MVC pages and controllers.
builder.Services.AddControllersWithViews();

// Kiosk session storage.
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);

    options.Cookie.Name = ".LibraryLink.Kiosk";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // Allows local HTTP development and protects cookies on HTTPS.
    options.Cookie.SecurePolicy =
        builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
});

// Fine calculation.
builder.Services.Configure<FineSettings>(
    builder.Configuration.GetSection(FineSettings.SectionName));

builder.Services.AddScoped<IFineCalculator, FineCalculator>();

// Reservations and notification settings.
builder.Services.Configure<ReservationSettings>(
    builder.Configuration.GetSection(
        ReservationSettings.SectionName));

builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection(
        NotificationSettings.SectionName));

builder.Services.AddScoped<
    INotificationService,
    NotificationService>();

builder.Services.AddScoped<
    IReservationService,
    ReservationService>();

// Automatically check for loan reminders while the app runs.
builder.Services.AddHostedService<NotificationBackgroundService>();

var app = builder.Build();

// Error handling.
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

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Default MVC routing includes /Kiosk once its controller exists.
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

// Create application roles and demonstration login accounts.
// Apply database migrations before starting the application.
await IdentitySeeder.SeedAsync(app.Services);

// Optional reservation demonstration records.
if (app.Configuration.GetValue<bool>(
        "DemoData:SeedReservationScenario"))
{
    await ReservationDemoSeeder.SeedAsync(app.Services);
}

app.Run();