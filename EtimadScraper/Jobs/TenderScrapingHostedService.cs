using EtimadScraper.Models;
using EtimadScraper.Services;
using EtimadScraper.Configuration;

namespace EtimadScraper.Jobs;

/// <summary>
/// ASP.NET Core <see cref="BackgroundService"/> that runs the full scraping job
/// on application startup (one-shot) and exposes a manual trigger via
/// <see cref="TriggerAsync"/>.
///
/// The job is deliberately NOT scheduled on a timer here – trigger it
/// manually through the API endpoint or extend this class with a timer
/// if periodic execution is required.
/// </summary>
public class TenderScrapingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenderScrapingHostedService> _logger;
    private readonly ScrapingJobSettings _settings;

    // Allows the controller to request a new run while the hosted service is alive.
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private ScrapingJobResult? _lastResult;

    public TenderScrapingHostedService(
        IServiceScopeFactory scopeFactory,
        ScrapingJobSettings settings,
        ILogger<TenderScrapingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings     = settings;
        _logger       = logger;
    }

    /// <summary>Latest job result; null if the job has never run.</summary>
    public ScrapingJobResult? LastResult => _lastResult;

    /// <summary>True while a job is executing.</summary>
    public bool IsRunning => _runLock.CurrentCount == 0;

    /// <summary>True when job execution is enabled by configuration.</summary>
    public bool IsEnabled => _settings.Enabled;

    /// <summary>
    /// Triggers a job run from outside (e.g. from the API controller).
    /// Returns false immediately if a run is already in progress.
    /// </summary>
    public async Task<(bool Accepted, ScrapingJobResult? Result)> TriggerAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Scraping job is disabled by configuration (ScrapingJob:Enabled=false).");
            return (false, _lastResult);
        }

        if (!await _runLock.WaitAsync(0, cancellationToken))
        {
            _logger.LogWarning("A scraping job is already running. Trigger ignored.");
            return (false, _lastResult);
        }

        try
        {
            _lastResult = await ExecuteJobAsync(cancellationToken);
            return (true, _lastResult);
        }
        finally
        {
            _runLock.Release();
        }
    }

    // -----------------------------------------------------------------------
    // BackgroundService lifecycle
    // -----------------------------------------------------------------------

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("TenderScrapingHostedService is disabled by configuration (ScrapingJob:Enabled=false). Startup run skipped.");
            return;
        }

        // Give the application a moment to finish starting up before the
        // first automatic run.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("TenderScrapingHostedService: automatic startup run begins.");

        if (!await _runLock.WaitAsync(0, stoppingToken))
        {
            _logger.LogWarning("Could not acquire run lock on startup. Skipping automatic run.");
            return;
        }

        try
        {
            _lastResult = await ExecuteJobAsync(stoppingToken);
        }
        finally
        {
            _runLock.Release();
        }
    }

    // -----------------------------------------------------------------------
    // Core execution – always runs inside a fresh DI scope.
    // -----------------------------------------------------------------------

    private async Task<ScrapingJobResult> ExecuteJobAsync(CancellationToken cancellationToken)
    {
        // Create a new DI scope so that Scoped services (DbContext, persistence
        // service, scraper service) are properly lifetime-managed.
        await using var scope = _scopeFactory.CreateAsyncScope();

        var jobService = scope.ServiceProvider.GetRequiredService<TenderScrapingJobService>();

        try
        {
            return await jobService.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Scraping job was cancelled.");
            return new ScrapingJobResult
            {
                Success     = false,
                Message     = "Job was cancelled.",
                StartedAt   = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception in scraping hosted service.");
            return new ScrapingJobResult
            {
                Success     = false,
                Message     = $"Unhandled error: {ex.Message}",
                StartedAt   = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                Errors      = new List<string> { ex.ToString() }
            };
        }
    }
}
