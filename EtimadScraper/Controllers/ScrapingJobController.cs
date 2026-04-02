using EtimadScraper.Jobs;
using EtimadScraper.Models;
using Microsoft.AspNetCore.Mvc;

namespace EtimadScraper.Controllers;

/// <summary>
/// API endpoints for running and monitoring the full-scrape background job.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ScrapingJobController : ControllerBase
{
    private readonly TenderScrapingHostedService _hostedService;
    private readonly ILogger<ScrapingJobController> _logger;

    public ScrapingJobController(
        TenderScrapingHostedService hostedService,
        ILogger<ScrapingJobController> logger)
    {
        _hostedService = hostedService;
        _logger        = logger;
    }

    /// <summary>
    /// Manually trigger the full scraping job (page 1 ? last page).
    /// Returns 409 Conflict if a job is already running.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("run")]
    [ProducesResponseType(typeof(ScrapingJobResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RunJob(CancellationToken cancellationToken)
    {
        if (!_hostedService.IsEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "Scraping job is disabled by configuration (ScrapingJob:Enabled=false)." });
        }

        if (_hostedService.IsRunning)
        {
            _logger.LogWarning("Job trigger rejected – job already running.");
            return Conflict(new { error = "A scraping job is already in progress. Please wait for it to finish." });
        }

        _logger.LogInformation("Manual job trigger received via API.");

        var (accepted, result) = await _hostedService.TriggerAsync(cancellationToken);

        if (!accepted || result is null)
        {
            return Conflict(new { error = "Could not start the job – it may have just started from another request." });
        }

        return result.Success ? Ok(result) : StatusCode(500, result);
    }

    /// <summary>
    /// Returns the status of the scraping job (running / last result).
    /// </summary>
     [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            isEnabled = _hostedService.IsEnabled,
            isRunning  = _hostedService.IsRunning,
            lastResult = _hostedService.LastResult
        });
    }
}
