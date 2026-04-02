using EtimadScraper.Jobs;
using EtimadScraper.Models;
using EtimadScraper.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtimadScraper.Controllers;

/// <summary>
/// Exposes status and manual-trigger endpoints for the supplier-tender
/// details background sync job.
///
/// GET  /api/supplier-tender-details-sync/status  — current job state
/// POST /api/supplier-tender-details-sync/run     — manual trigger
/// </summary>
[ApiController]
[Route("api/supplier-tender-details-sync")]
public sealed class SupplierTenderDetailsJobController : ControllerBase
{
    private readonly ISupplierTenderDetailsJobState _jobState;
    private readonly SupplierTenderDetailsSyncBackgroundJob _backgroundJob;
    private readonly ILogger<SupplierTenderDetailsJobController> _logger;

    public SupplierTenderDetailsJobController(
        ISupplierTenderDetailsJobState jobState,
        SupplierTenderDetailsSyncBackgroundJob backgroundJob,
        ILogger<SupplierTenderDetailsJobController> logger)
    {
        _jobState      = jobState;
        _backgroundJob = backgroundJob;
        _logger        = logger;
    }

    // -----------------------------------------------------------------------
    // GET /api/supplier-tender-details-sync/status
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the current runtime state of the details-sync background job.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SupplierTenderDetailsJobStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(BuildStatusDto());
    }

    // -----------------------------------------------------------------------
    // POST /api/supplier-tender-details-sync/run
    // -----------------------------------------------------------------------

    /// <summary>
    /// Manually triggers one execution of the details-sync job.
    /// Respects the <c>IsEnabled</c> flag; returns 409 if a run is already in progress.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(SupplierTenderDetailsJobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SupplierTenderDetailsJobStatusDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(SupplierTenderDetailsJobStatusDto), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RunNow(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/supplier-tender-details-sync/run — manual trigger requested.");

        if (!_jobState.IsEnabled)
        {
            _logger.LogWarning("Manual trigger rejected — job is disabled in configuration.");
            return StatusCode(StatusCodes.Status403Forbidden, BuildStatusDto());
        }

        if (_jobState.IsRunning)
        {
            _logger.LogWarning("Manual trigger rejected — a run is already in progress.");
            return Conflict(BuildStatusDto());
        }

        // RunSyncAsync returns false when the concurrency flag blocked it (race condition
        // between the IsRunning check and the Interlocked.CompareExchange inside the job).
        var started = await _backgroundJob.RunSyncAsync(cancellationToken);

        if (!started)
        {
            _logger.LogWarning(
                "Manual trigger: execution was blocked by the concurrency guard.");
            return Conflict(BuildStatusDto());
        }

        return Ok(BuildStatusDto());
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    private SupplierTenderDetailsJobStatusDto BuildStatusDto() => new()
    {
        IsEnabled       = _jobState.IsEnabled,
        IsRunning       = _jobState.IsRunning,
        LastRunTime     = _jobState.LastStartedAt,
        LastCompletedAt = _jobState.LastCompletedAt,
        LastSuccessTime = _jobState.LastSuccessAt,
        LastResultMessage = _jobState.LastMessage,
        LastError       = _jobState.LastError,
        TotalRuns       = _jobState.TotalRuns,
        TotalProcessed  = _jobState.LastTotalProcessed,
        TotalInserted   = _jobState.LastTotalInserted,
        TotalSkipped    = _jobState.LastTotalSkipped,
        TotalFailed     = _jobState.LastTotalFailed
    };
}
