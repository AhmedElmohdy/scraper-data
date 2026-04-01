using EtimadScraper.Configuration;
using EtimadScraper.Data;
using EtimadScraper.Jobs;
using EtimadScraper.Services;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper;

public class Program
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        
        // Configure Swagger with XML documentation
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Etimad Tender Scraper API",
                Version = "v1",
                Description = "API for scraping tender data from the Saudi Etimad platform",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Etimad Scraper",
                    Email = "support@example.com"
                }
            });
        });

        // Register scraper configuration
        builder.Services.AddSingleton(new ScraperConfiguration
        {
            BaseUrl = "https://tenders.etimad.sa/Tender/AllTendersForVisitor",
            Headless = true,              // Set to false to see browser UI
            StartPage = 1,                 // Start from page 1
            MaxPages = 5,                  // Scrape 5 pages
            PageLoadTimeout = 30000,       // 30 seconds timeout
            DelayBetweenPages = 2000,      // 2 seconds delay between pages
            MaxRetryAttempts = 3,          // Retry 3 times on failure
            OutputFilePath = "tenders.json" // Output file name
        });

        // ?? ScrapingJob settings ??????????????????????????????????????????????
        var jobSettings = builder.Configuration
            .GetSection(ScrapingJobSettings.SectionName)
            .Get<ScrapingJobSettings>() ?? new ScrapingJobSettings();
        builder.Services.AddSingleton(jobSettings);

        // ?? SQL Server / EF Core ??????????????????????????????????????????????
        var connectionString = builder.Configuration["Database:ConnectionString"]
            ?? "Server=207.180.213.46;Database=EtimadTenders;User Id=sa;Password=dev_09072023ha$;TrustServerCertificate=True;";

        builder.Services.AddDbContext<TenderDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            if (builder.Environment.IsDevelopment())
            {
                var enableSensitiveDataLogging = builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging");
                if (enableSensitiveDataLogging)
                    options.EnableSensitiveDataLogging();

                options.EnableDetailedErrors();
            }
        });
        builder.Services.AddScoped<EtimadScraperService>();
        builder.Services.AddScoped<TenderPersistenceService>();
        builder.Services.AddScoped<TenderScrapingJobService>();

        // ?? Background hosted service (Singleton) ????????????????????????????
        // Registered as both IHostedService (for .NET to manage) AND as its
        // concrete type (so controllers can inject it directly).
        builder.Services.AddSingleton<TenderScrapingHostedService>();
        builder.Services.AddHostedService(sp =>
            sp.GetRequiredService<TenderScrapingHostedService>());

        // Register HttpClient for TenderDetailsScraperService
        builder.Services.AddHttpClient("EtimadClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                UseCookies = true
            });

        // Register TenderDetailsScraperService
        builder.Services.AddScoped<TenderDetailsScraperService>();

        var app = builder.Build();

        // ?? Auto-migrate on startup (controlled by appsettings) ???????????????
        var enableAutoMigration = builder.Configuration.GetValue<bool>("Database:EnableAutoMigration");
        if (enableAutoMigration)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TenderDbContext>();
            db.Database.Migrate();
        }

        // Enable Swagger in all environments (not just Development)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Etimad Tender Scraper API v1");
            c.RoutePrefix = "swagger"; // Access at /swagger
        });

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
