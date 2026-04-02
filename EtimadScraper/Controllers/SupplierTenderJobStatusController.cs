using EtimadScraper.Models;
using EtimadScraper.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtimadScraper.Controllers;

/// <summary>
/// Exposes the live runtime state of the supplier-tender background sync job.
/// </summary>
[ApiController]
[Route("api/job-status")]
public sealed class SupplierTenderJobStatusController : ControllerBase
{
    private readonly ISupplierTenderJobState _jobState;

    public SupplierTenderJobStatusController(ISupplierTenderJobState jobState)
    {
        _jobState = jobState;
    }

    /// <summary>
    /// Returns the current status of the supplier-tender background sync job.
    /// </summary>
    /// <response code="200">Current job state snapshot.</response>
    [HttpGet("supplier-tender")]
    [ProducesResponseType(typeof(SupplierTenderJobStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        var dto = new SupplierTenderJobStatusDto
        {
            IsEnabled       = _jobState.IsEnabled,
            IsRunning       = _jobState.IsRunning,
            LastStartedAt   = _jobState.LastStartedAt,
            LastCompletedAt = _jobState.LastCompletedAt,
            LastSuccessAt   = _jobState.LastSuccessAt,
            LastError       = _jobState.LastError,
            LastMessage     = _jobState.LastMessage,
            TotalRuns       = _jobState.TotalRuns
        };

        return Ok(dto);
    }
}
