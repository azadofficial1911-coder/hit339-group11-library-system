using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibraryManagementSystem.Data;

// EF tooling can generate scripts without starting seeders or background services.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(directory, "appsettings.json")))
            directory = Path.Combine(directory, "LibraryManagementSystem");
        var config = new ConfigurationBuilder().SetBasePath(directory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configure DefaultConnection before running EF tools.");
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connection).Options);
    }
}
