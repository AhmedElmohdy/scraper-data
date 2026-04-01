using EtimadScraper.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EtimadScraper.Infrastructure;

/// <summary>
/// Enables EF Core CLI tools (dotnet ef migrations add / database update)
/// to instantiate <see cref="TenderDbContext"/> without a running host.
/// </summary>
public class TenderDbContextFactory : IDesignTimeDbContextFactory<TenderDbContext>
{
    public TenderDbContext CreateDbContext(string[] args)
    {
        // Read the connection string from appsettings.json at design-time.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["Database:ConnectionString"]
            ?? "Server=207.180.213.46;Database=EtimadTenders;User Id=sa;Password=dev_09072023ha$;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<TenderDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new TenderDbContext(optionsBuilder.Options);
    }
}
