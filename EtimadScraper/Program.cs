using EtimadScraper.Configuration;
using EtimadScraper.Data;
using EtimadScraper.Jobs;
using EtimadScraper.Services;
using Microsoft.AspNetCore.HttpOverrides;
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
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

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

        // ?? SupplierTenderSync settings ??????????????????????????????????????
        var supplierSyncSettings = builder.Configuration
            .GetSection(SupplierTenderSyncSettings.SectionName)
            .Get<SupplierTenderSyncSettings>() ?? new SupplierTenderSyncSettings();
        builder.Services.AddSingleton(supplierSyncSettings);

        // ?? SupplierTenderDetailsSync settings ??????????????????????????????????????
        var supplierDetailsSyncSettings = builder.Configuration
            .GetSection(SupplierTenderDetailsSyncSettings.SectionName)
            .Get<SupplierTenderDetailsSyncSettings>() ?? new SupplierTenderDetailsSyncSettings();
        builder.Services.AddSingleton(supplierDetailsSyncSettings);

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

        // IDbContextFactory is used by SupplierTenderDetailsSyncService so it
        // can open a fresh DbContext per tender inside a long-running loop.
        builder.Services.AddDbContextFactory<TenderDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            if (builder.Environment.IsDevelopment())
                options.EnableDetailedErrors();
        }, ServiceLifetime.Scoped);
        builder.Services.AddScoped<EtimadScraperService>();
        builder.Services.AddScoped<TenderPersistenceService>();
        builder.Services.AddScoped<TenderScrapingJobService>();

        // ?? Background hosted service (Singleton) ????????????????????????????
        // Registered as both IHostedService (for .NET to manage) AND as its
        // concrete type (so controllers can inject it directly).
        builder.Services.AddSingleton<TenderScrapingHostedService>();
        builder.Services.AddHostedService(sp =>
            sp.GetRequiredService<TenderScrapingHostedService>());

        // Register HttpClient for TenderDetailsScraperService and SupplierTenderSyncService.
        // BrowserHeadersHandler adds realistic browser headers to every request so that
        // the Etimad F5 WAF does not serve a bot-challenge page instead of JSON.
        builder.Services.AddTransient<BrowserHeadersHandler>();
        builder.Services.AddHttpClient("EtimadClient")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                       | System.Net.DecompressionMethods.Deflate
                                       | System.Net.DecompressionMethods.Brotli,
                AllowAutoRedirect = true,
                UseCookies = true
            })
            .AddHttpMessageHandler<BrowserHeadersHandler>();

        // Register TenderDetailsScraperService
        builder.Services.AddScoped<TenderDetailsScraperService>();

        // ?? Supplier Tender Sync (JSON API) ?????????????????????????????????????
        builder.Services.AddScoped<ISupplierTenderSyncService, SupplierTenderSyncService>();

        // ?? Supplier Tender Details Sync (scrapes detail pages) ?????????????????
        builder.Services.AddScoped<ISupplierTenderDetailsSyncService, SupplierTenderDetailsSyncService>();

        // Singleton state store — shared between the background job (writer)
        // and the status controller (reader).
        builder.Services.AddSingleton<ISupplierTenderJobState, SupplierTenderJobState>();

        // Periodic background job — runs every SupplierTenderSync:IntervalHours hours.
        // Disable without code changes by setting SupplierTenderSync:Enabled=false.
        builder.Services.AddHostedService<SupplierTenderSyncBackgroundJob>();

        // Singleton state store for the details-sync job.
        builder.Services.AddSingleton<ISupplierTenderDetailsJobState, SupplierTenderDetailsJobState>();

        // Register as both its concrete type (for controller injection) AND as IHostedService.
        builder.Services.AddSingleton<SupplierTenderDetailsSyncBackgroundJob>();
        builder.Services.AddHostedService(sp =>
            sp.GetRequiredService<SupplierTenderDetailsSyncBackgroundJob>());


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

        // Forward headers from IIS reverse proxy (X-Forwarded-For, X-Forwarded-Proto).
        // This must come before any middleware that depends on the scheme or remote IP.
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.UseCors("FrontendPolicy");
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}

/// <summary>
/// Delegating handler that injects browser-like HTTP headers on every outbound
/// request made by the named "EtimadClient" HttpClient.
///
/// The Etimad platform sits behind an F5 BIG-IP WAF that fingerprints requests
/// and serves a JavaScript/CAPTCHA challenge page when it detects automated
/// traffic. Sending a realistic Accept, Accept-Language, Referer and
/// User-Agent is usually sufficient to pass the first-level bot check.
/// </summary>
internal sealed class BrowserHeadersHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var headers = request.Headers;

        // Mimic a real Chrome browser on Windows.
        if (!headers.Contains("User-Agent"))
            headers.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
              + "AppleWebKit/537.36 (KHTML, like Gecko) "
              + "Chrome/124.0.0.0 Safari/537.36");

        if (!headers.Contains("Accept"))
            headers.TryAddWithoutValidation(
                "Accept",
                "application/json, text/plain, */*");

        if (!headers.Contains("Accept-Language"))
            headers.TryAddWithoutValidation(
                "Accept-Language",
                "ar-SA,ar;q=0.9,en-US;q=0.8,en;q=0.7");

        if (!headers.Contains("Accept-Encoding"))
            headers.TryAddWithoutValidation(
                "Accept-Encoding",
                "gzip, deflate, br");

        // Referer makes the request look like it originated from the portal itself.
        if (!headers.Contains("Referer"))
            headers.TryAddWithoutValidation(
                "Referer",
                "https://tenders.etimad.sa/Tender/AllTendersForVisitor");

        if (!headers.Contains("X-Requested-With"))
            headers.TryAddWithoutValidation(
                "X-Requested-With",
                "XMLHttpRequest");

        return base.SendAsync(request, cancellationToken);
    }
}
