using EtimadScraper.Services;

namespace EtimadScraper.Jobs;

/// <summary>
/// One-shot background job that triggers the AI evaluation of unevaluated tenders
/// when invoked manually via the API controller.
///
/// Unlike the periodic sync jobs, this job does not run on a timer; it only
/// executes when <see cref="RunEvaluationAsync"/> is called.
///
/// Concurrency guard: an <c>int</c> flag (<c>_running</c>) is flipped with
/// <see cref="Interlocked.CompareExchange"/> so that a slow run that overruns
/// a second manual trigger does not launch overlapping executions.
/// </summary>
public sealed class TenderEvaluationBackgroundJob : BackgroundService
{
    // 0 = idle, 1 = running.
    private int _running;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITenderEvaluationJobState _jobState;
    private readonly ILogger<TenderEvaluationBackgroundJob> _logger;

    public TenderEvaluationBackgroundJob(
        IServiceScopeFactory scopeFactory,
        ITenderEvaluationJobState jobState,
        ILogger<TenderEvaluationBackgroundJob> logger)
    {
        _scopeFactory = scopeFactory;
        _jobState     = jobState;
        _logger       = logger;
    }

    // -----------------------------------------------------------------------
    // BackgroundService entry point — idles; evaluation is triggered manually.
    // -----------------------------------------------------------------------

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "TenderEvaluationBackgroundJob started (manual-trigger mode). " +
            "Use POST /api/tender-evaluation/run to trigger evaluation.");

        // Keep alive until the host shuts down.
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
    }

    // -----------------------------------------------------------------------
    // Manual-trigger entry point (called from the controller)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Runs one evaluation cycle.  Returns <see langword="false"/> when another
    /// execution is already in progress or the job is not enabled.
    /// </summary>
    internal async Task<bool> RunEvaluationAsync(CancellationToken cancellationToken)
    {
        if (!_jobState.IsEnabled)
        {
            _logger.LogWarning(
                "TenderEvaluationBackgroundJob: skipping run — " +
                "job is disabled (TenderEvaluation:Enabled=false) or OpenRouterApiKey is not configured.");
            return false;
        }

        // Atomic concurrency guard: only one execution at a time.
        if (Interlocked.CompareExchange(ref _running, 1, 0) == 1)
        {
            _logger.LogWarning(
                "TenderEvaluationBackgroundJob: skipping run — " +
                "a previous execution is still in progress.");
            return false;
        }

        _jobState.MarkStarted();

        var started = DateTime.UtcNow;
        _logger.LogInformation(
            "=== TenderEvaluationBackgroundJob: run #{Run} started at {Time} UTC ===",
            _jobState.TotalRuns, started);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var evalService = scope.ServiceProvider
                .GetRequiredService<ITenderEvaluationService>();

            var result = await evalService.EvaluateUnevaluatedTendersAsync(cancellationToken);

            var duration = DateTime.UtcNow - started;

            if (result.Success)
            {
                _jobState.MarkSucceeded(
                    result.Message,
                    result.TotalCandidates,
                    result.TotalEvaluated,
                    result.TotalFailed);

                _logger.LogInformation(
                    "=== TenderEvaluationBackgroundJob: run #{Run} SUCCESS. " +
                    "Duration={Duration}, Candidates={C}, Evaluated={E}, Failed={F} ===",
                    _jobState.TotalRuns, duration,
                    result.TotalCandidates, result.TotalEvaluated, result.TotalFailed);
            }
            else
            {
                _jobState.MarkFailed(
                    string.Join("; ", result.Errors),
                    result.Message,
                    result.TotalCandidates,
                    result.TotalEvaluated,
                    result.TotalFailed);

                _logger.LogWarning(
                    "=== TenderEvaluationBackgroundJob: run #{Run} FINISHED WITH ERRORS. " +
                    "Duration={Duration}, Candidates={C}, Evaluated={E}, Failed={F}. " +
                    "Message={Message} ===",
                    _jobState.TotalRuns, duration,
                    result.TotalCandidates, result.TotalEvaluated, result.TotalFailed,
                    result.Message);
            }

            return result.Success;
        }
        catch (OperationCanceledException)
        {
            var duration = DateTime.UtcNow - started;
            _jobState.MarkFailed("Run was cancelled by the host.", "Evaluation cancelled.", 0, 0, 0);
            _logger.LogWarning(
                "TenderEvaluationBackgroundJob: run #{Run} was cancelled. Duration={Duration}",
                _jobState.TotalRuns, duration);
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - started;
            _jobState.MarkFailed(
                ex.Message, "Evaluation failed with an unhandled exception.", 0, 0, 0);
            _logger.LogError(
                ex,
                "TenderEvaluationBackgroundJob: unhandled exception in run #{Run}. " +
                "Duration={Duration}.",
                _jobState.TotalRuns, duration);
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
