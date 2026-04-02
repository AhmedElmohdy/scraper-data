using EtimadScraper.Configuration;
using EtimadScraper.Services;

namespace EtimadScraper.Jobs;

/// <summary>
/// Periodic background job that calls <see cref="ISupplierTenderSyncService.SyncAllAsync"/>
/// on a configurable interval.
///
/// Lifecycle:
/// 1. On startup, waits 15 seconds to allow the host to finish initialising.
/// 2. Runs the sync immediately (first execution).
/// 3. Waits <see cref="SupplierTenderSyncSettings.IntervalHours"/> hours.
/// 4. Repeats until the host shuts down or the job is disabled.
///
/// Configuration (appsettings.json ? "SupplierTenderSync"):
/// • Enabled      – set to false to prevent any automatic execution.
/// • IntervalHours – how many hours between runs (default: 3).
/// </summary>
public sealed class SupplierTenderSyncBackgroundJob : BackgroundService
{
    // Delay before the very first run so the web server is fully ready.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SupplierTenderSyncSettings _settings;
    private readonly ISupplierTenderJobState _jobState;
    private readonly ILogger<SupplierTenderSyncBackgroundJob> _logger;

    public SupplierTenderSyncBackgroundJob(
        IServiceScopeFactory scopeFactory,
        SupplierTenderSyncSettings settings,
        ISupplierTenderJobState jobState,
        ILogger<SupplierTenderSyncBackgroundJob> logger)
    {
        _scopeFactory = scopeFactory;
        _settings     = settings;
        _jobState     = jobState;
        _logger       = logger;
    }

    // -----------------------------------------------------------------------
    // BackgroundService entry point
    // -----------------------------------------------------------------------

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // If the job is disabled in config, log once and exit — no resources consumed.
        if (!_settings.Enabled)
        {
            _logger.LogInformation(
                "SupplierTenderSyncBackgroundJob is disabled (SupplierTenderSync:Enabled=false). " +
                "Set it to true in appsettings.json to enable automatic syncing.");
            return;
        }

        var interval = TimeSpan.FromHours(_settings.IntervalHours);

        _logger.LogInformation(
            "SupplierTenderSyncBackgroundJob started. Interval={Hours}h, startup delay={Delay}s.",
            _settings.IntervalHours, StartupDelay.TotalSeconds);

        // Brief pause so the host, EF migrations, and HTTP clients are all ready.
        await Task.Delay(StartupDelay, stoppingToken);

        // Run immediately on startup, then repeat on the configured interval.
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncAsync(stoppingToken);

            // Wait for the next scheduled run — respects cancellation so shutdown is instant.
            _logger.LogInformation(
                "SupplierTenderSyncBackgroundJob sleeping for {Hours} hour(s). Next run at ~{Next:HH:mm} UTC.",
                _settings.IntervalHours,
                DateTime.UtcNow.Add(interval));

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down — exit the loop cleanly.
                break;
            }
        }

        _logger.LogInformation("SupplierTenderSyncBackgroundJob stopped.");
    }

    // -----------------------------------------------------------------------
    // Single sync execution — always inside a fresh DI scope so that
    // Scoped services (DbContext, EF change tracker) are isolated per run.
    // -----------------------------------------------------------------------

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        // Re-read Enabled at the start of every run so a config change without
        // a restart (e.g. via Azure App Config) takes effect immediately.
        if (!_settings.Enabled)
        {
            _logger.LogWarning(
                "SupplierTenderSyncBackgroundJob: skipping run — job was disabled at runtime.");
            return;
        }

        _jobState.MarkStarted();

        _logger.LogInformation(
            "=== SupplierTenderSyncBackgroundJob: run #{Run} started at {Time} UTC ===",
            _jobState.TotalRuns, DateTime.UtcNow);

        try
        {
            // Create a new DI scope per run so EF's DbContext is not shared
            // across runs and change-tracker state is always fresh.
            await using var scope = _scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider
                .GetRequiredService<ISupplierTenderSyncService>();

            var result = await syncService.SyncAllAsync(cancellationToken);

            if (result.Success)
            {
                _jobState.MarkSucceeded(result.Message);

                _logger.LogInformation(
                    "=== SupplierTenderSyncBackgroundJob: run completed successfully. " +
                    "Pages={Pages}, Fetched={Fetched}, Inserted={Inserted}, Duration={Duration} ===",
                    result.TotalPagesProcessed,
                    result.TotalFetched,
                    result.TotalInserted,
                    result.Duration);
            }
            else
            {
                _jobState.MarkFailed(
                    $"{result.TotalFailedPages} page(s) failed.",
                    result.Message);

                _logger.LogWarning(
                    "=== SupplierTenderSyncBackgroundJob: run finished with errors. " +
                    "Message={Message}, FailedPages={FailedPages} ===",
                    result.Message,
                    result.TotalFailedPages);
            }
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down mid-run — update state then re-throw so the loop exits.
            _jobState.MarkFailed("Run was cancelled by the host.", "Sync cancelled.");
            _logger.LogWarning(
                "SupplierTenderSyncBackgroundJob: run was cancelled (host shutting down).");
            throw;
        }
        catch (Exception ex)
        {
            // Log and swallow — a transient error must never crash the background service.
            _jobState.MarkFailed(ex.Message, "Sync failed with an unhandled exception.");
            _logger.LogError(
                ex,
                "SupplierTenderSyncBackgroundJob: unhandled exception during run. " +
                "The job will retry at the next scheduled interval.");
        }
    }
}
