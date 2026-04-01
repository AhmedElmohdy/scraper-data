using EtimadScraper.Configuration;
using EtimadScraper.Services;

namespace EtimadScraper;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        // Register scraper service as scoped (new instance per request)
        builder.Services.AddScoped<EtimadScraperService>();

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
