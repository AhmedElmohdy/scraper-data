using EtimadScraper.Configuration;
using EtimadScraper.Services;

namespace EtimadScraper.Jobs;

/// <summary>
/// Periodic background job that calls
/// <see cref="ISupplierTenderDetailsSyncService.SyncDetailsAsync"/> on a
/// configurable interval (default: every 11 hours).
///
/// Lifecycle:
/// 1. On startup, waits 20 seconds to allow the host to fully initialise.
/// 2. Runs the sync immediately (first execution).
/// 3. Waits <see cref="SupplierTenderDetailsSyncSettings.IntervalHours"/> hours.
/// 4. Repeats until the host shuts down or the job is disabled.
///
/// Concurrency guard: an <see cref="int"/> flag (<c>_running</c>) is flipped
/// with <see cref="Interlocked.CompareExchange"/> so that a slow run that
/// overruns the interval does not launch a second overlapping execution.
///
/// Configuration (appsettings.json ? "SupplierTenderDetailsSync"):
/// • Enabled       — set to false to prevent any automatic execution.
/// • IntervalHours — hours between runs (default: 11).
/// </summary>
public sealed class SupplierTenderDetailsSyncBackgroundJob : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);

    // 0 = idle, 1 = running. Used as an atomic flag to prevent concurrent executions.
    private int _running;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SupplierTenderDetailsSyncSettings _settings;
    private readonly ISupplierTenderDetailsJobState _jobState;
    private readonly ILogger<SupplierTenderDetailsSyncBackgroundJob> _logger;

    public SupplierTenderDetailsSyncBackgroundJob(
        IServiceScopeFactory scopeFactory,
        SupplierTenderDetailsSyncSettings settings,
        ISupplierTenderDetailsJobState jobState,
        ILogger<SupplierTenderDetailsSyncBackgroundJob> logger)
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
        if (!_settings.Enabled)
        {
            _logger.LogInformation(
                "SupplierTenderDetailsSyncBackgroundJob is disabled " +
                "(SupplierTenderDetailsSync:Enabled=false). " +
                "Set it to true in appsettings.json to enable automatic syncing.");
            return;
        }

        var interval = TimeSpan.FromHours(_settings.IntervalHours);

        _logger.LogInformation(
            "SupplierTenderDetailsSyncBackgroundJob started. " +
            "Interval={Hours}h, StartupDelay={Delay}s.",
            _settings.IntervalHours, StartupDelay.TotalSeconds);

        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncAsync(stoppingToken);

            _logger.LogInformation(
                "SupplierTenderDetailsSyncBackgroundJob sleeping for {Hours}h. " +
                "Next run at ~{Next:HH:mm} UTC.",
                _settings.IntervalHours,
                DateTime.UtcNow.Add(interval));

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SupplierTenderDetailsSyncBackgroundJob stopped.");
    }

    // -----------------------------------------------------------------------
    // Single sync execution
    // -----------------------------------------------------------------------

    /// <summary>
    /// Executes one sync cycle. Public so it can also be triggered manually
    /// from the controller via the shared <see cref="IServiceScopeFactory"/>.
    /// Returns <see langword="false"/> if another execution is already in progress.
    /// </summary>
    internal async Task<bool> RunSyncAsync(CancellationToken cancellationToken)
    {
        // Re-check Enabled at runtime so a config change takes effect without restart.
        if (!_settings.Enabled)
        {
            _logger.LogWarning(
                "SupplierTenderDetailsSyncBackgroundJob: skipping run — job is disabled.");
            return false;
        }

        // Atomic concurrency guard: only one execution at a time.
        if (Interlocked.CompareExchange(ref _running, 1, 0) == 1)
        {
            _logger.LogWarning(
                "SupplierTenderDetailsSyncBackgroundJob: skipping run — " +
                "a previous execution is still in progress.");
            return false;
        }

        _jobState.MarkStarted();

        var started = DateTime.UtcNow;
        _logger.LogInformation(
            "=== SupplierTenderDetailsSyncBackgroundJob: run #{Run} started at {Time} UTC ===",
            _jobState.TotalRuns, started);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var syncService = scope.ServiceProvider
                .GetRequiredService<ISupplierTenderDetailsSyncService>();

            var result = await syncService.SyncDetailsAsync(cancellationToken);

            var duration = DateTime.UtcNow - started;

            if (result.Success)
            {
                _jobState.MarkSucceeded(
                    result.Message,
                    result.TotalTendersScanned,
                    result.TotalInserted,
                    result.TotalSkipped,
                    result.TotalFailed);

                _logger.LogInformation(
                    "=== SupplierTenderDetailsSyncBackgroundJob: run #{Run} SUCCESS. " +
                    "Duration={Duration}, Scanned={Scanned}, Inserted={Inserted}, " +
                    "Skipped={Skipped}, Failed={Failed} ===",
                    _jobState.TotalRuns, duration,
                    result.TotalTendersScanned, result.TotalInserted,
                    result.TotalSkipped, result.TotalFailed);
            }
            else
            {
                _jobState.MarkFailed(
                    string.Join("; ", result.Errors),
                    result.Message,
                    result.TotalTendersScanned,
                    result.TotalInserted,
                    result.TotalSkipped,
                    result.TotalFailed);

                _logger.LogWarning(
                    "=== SupplierTenderDetailsSyncBackgroundJob: run #{Run} FINISHED WITH ERRORS. " +
                    "Duration={Duration}, Scanned={Scanned}, Inserted={Inserted}, " +
                    "Skipped={Skipped}, Failed={Failed}. Message={Message} ===",
                    _jobState.TotalRuns, duration,
                    result.TotalTendersScanned, result.TotalInserted,
                    result.TotalSkipped, result.TotalFailed, result.Message);
            }

            return result.Success;
        }
        catch (OperationCanceledException)
        {
            var duration = DateTime.UtcNow - started;
            _jobState.MarkFailed("Run was cancelled by the host.", "Sync cancelled.", 0, 0, 0, 0);
            _logger.LogWarning(
                "SupplierTenderDetailsSyncBackgroundJob: run #{Run} was cancelled. Duration={Duration}",
                _jobState.TotalRuns, duration);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;
            _jobState.MarkFailed(ex.Message, "Sync failed with an unhandled exception.", 0, 0, 0, 0);
            _logger.LogError(
                ex,
                "SupplierTenderDetailsSyncBackgroundJob: unhandled exception in run #{Run}. " +
                "Duration={Duration}. The job will retry at the next scheduled interval.",
                _jobState.TotalRuns, duration);
            return false;
        }
        finally
        {
            // Always release the concurrency flag.
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
