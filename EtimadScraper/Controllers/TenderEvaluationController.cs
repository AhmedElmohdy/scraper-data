using EtimadScraper.Configuration;
using EtimadScraper.Data;
using EtimadScraper.Jobs;
using EtimadScraper.Models;
using EtimadScraper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper.Controllers;

/// <summary>
/// Exposes endpoints for the AI-based tender evaluation feature.
///
/// POST /api/tender-evaluation/run            — trigger evaluation manually
/// POST /api/tender-evaluation/test           — ad-hoc single-tender test
/// GET  /api/tender-evaluation/status         — current job state
/// GET  /api/tender-evaluation/results        — paginated list of evaluated tenders
/// GET  /api/tender-evaluation/results/top    — tenders with MatchingScore >= MinMatchingScore
/// </summary>
[ApiController]
[Route("api/tender-evaluation")]
public sealed class TenderEvaluationController : ControllerBase
{
    private readonly ITenderEvaluationJobState _jobState;
    private readonly TenderEvaluationBackgroundJob _backgroundJob;
    private readonly TenderEvaluationSettings _settings;
    private readonly TenderDbContext _db;
    private readonly ITenderEvaluationService _evaluationService;
    private readonly ILogger<TenderEvaluationController> _logger;

    public TenderEvaluationController(
        ITenderEvaluationJobState jobState,
        TenderEvaluationBackgroundJob backgroundJob,
        TenderEvaluationSettings settings,
        TenderDbContext db,
        ITenderEvaluationService evaluationService,
        ILogger<TenderEvaluationController> logger)
    {
        _jobState          = jobState;
        _backgroundJob     = backgroundJob;
        _settings          = settings;
        _db                = db;
        _evaluationService = evaluationService;
        _logger            = logger;
    }

    // -----------------------------------------------------------------------
    // POST /api/tender-evaluation/run
    // -----------------------------------------------------------------------

    /// <summary>
    /// Manually triggers one AI evaluation cycle for all unevaluated tenders.
    /// Returns 409 Conflict if an evaluation is already running.
    /// Returns 503 if the OpenRouter API key is not configured.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(TenderEvaluationJobStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TenderEvaluationJobStatusDto), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(TenderEvaluationJobStatusDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RunNow(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/tender-evaluation/run — manual trigger requested.");

        if (!_jobState.IsEnabled)
        {
            _logger.LogWarning(
                "Manual trigger rejected — job is disabled (TenderEvaluation:Enabled=false) " +
                "or OpenRouterApiKey is not configured.");
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                BuildStatusDto());
        }

        if (_jobState.IsRunning)
        {
            _logger.LogWarning("Manual trigger rejected — a run is already in progress.");
            return Conflict(BuildStatusDto());
        }

        var started = await _backgroundJob.RunEvaluationAsync(cancellationToken);

        if (!started)
        {
            _logger.LogWarning(
                "Manual trigger: execution was blocked by the concurrency guard.");
            return Conflict(BuildStatusDto());
        }

        return Ok(BuildStatusDto());
    }

    // -----------------------------------------------------------------------
    // POST /api/tender-evaluation/test
    // -----------------------------------------------------------------------

    /// <summary>
    /// Sends a single ad-hoc tender to the AI and returns the evaluation result
    /// without persisting anything to the database.
    /// </summary>
    /// <param name="tenderName">The tender name to evaluate.</param>
    /// <param name="agencyName">The agency name to evaluate.</param>
    [HttpPost("test")]
    [ProducesResponseType(typeof(TenderEvaluationTestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> TestEvaluate(
        [FromQuery] string tenderName,
        [FromQuery] string agencyName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenderName) || string.IsNullOrWhiteSpace(agencyName))
            return BadRequest("Both tenderName and agencyName are required.");

        //if (!_jobState.IsEnabled)
        //    return StatusCode(StatusCodes.Status503ServiceUnavailable,
        //        "Tender evaluation is disabled or OpenRouterApiKey is not configured.");

        _logger.LogInformation(
            "POST /api/tender-evaluation/test — TenderName={Name}, AgencyName={Agency}",
            tenderName, agencyName);

        var result = await _evaluationService.TestEvaluateAsync(
            tenderName, agencyName, cancellationToken);

        return Ok(result);
    }

    // -----------------------------------------------------------------------
    // GET /api/tender-evaluation/status
    // -----------------------------------------------------------------------

    /// <summary>Returns the current runtime state of the evaluation background job.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TenderEvaluationJobStatusDto), StatusCodes.Status200OK)]
    public IActionResult GetStatus() => Ok(BuildStatusDto());

    // -----------------------------------------------------------------------
    // GET /api/tender-evaluation/results
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns a paginated list of evaluated tenders, sorted by
    /// <c>MatchingScore</c> descending.
    ///
    /// Optional query parameters:
    /// • <c>minScore</c>  — include only tenders with MatchingScore >= this value.
    /// • <c>page</c>      — 1-based page number (default: 1).
    /// • <c>pageSize</c>  — items per page (default: 20, max: 100).
    /// </summary>
    [HttpGet("results")]
    [ProducesResponseType(typeof(EvaluationResultsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetResults(
        [FromQuery] decimal? minScore    = null,
        [FromQuery] int      page        = 1,
        [FromQuery] int      pageSize    = 20,
        CancellationToken cancellationToken = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.SupplierTenders
            .AsNoTracking()
            .Where(t => t.Evaluated == true);

        if (minScore.HasValue)
            query = query.Where(t => t.MatchingScore >= minScore.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.MatchingScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new EvaluatedTenderDto
            {
                Id               = t.Id,
                ReferenceNumber  = t.ReferenceNumber,
                TenderName       = t.TenderName,
                AgencyName       = t.AgencyName,
                BranchName       = t.BranchName,
                TenderStatusName = t.TenderStatusName,
                SubmitionDate    = t.SubmitionDate,
                MatchingScore    = t.MatchingScore,
                MatchingReason   = t.MatchingReason,
                Evaluated        = t.Evaluated,
                LastSyncedAt     = t.LastSyncedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new EvaluationResultsResponse
        {
            TotalCount  = totalCount,
            Page        = page,
            PageSize    = pageSize,
            TotalPages  = (int)Math.Ceiling(totalCount / (double)pageSize),
            MinScore    = minScore,
            Data        = items
        });
    }

    // -----------------------------------------------------------------------
    // GET /api/tender-evaluation/results/top
    // -----------------------------------------------------------------------

    /// <summary>
    /// Convenience endpoint: returns evaluated tenders with
    /// <c>MatchingScore >= <see cref="TenderEvaluationSettings.MinMatchingScore"/></c>,
    /// sorted by score descending.
    /// </summary>
    [HttpGet("results/top")]
    [ProducesResponseType(typeof(EvaluationResultsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopResults(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await GetResults(_settings.MinMatchingScore, page, pageSize, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    private TenderEvaluationJobStatusDto BuildStatusDto() => new()
    {
        IsEnabled           = _jobState.IsEnabled,
        IsRunning           = _jobState.IsRunning,
        LastRunTime         = _jobState.LastStartedAt,
        LastCompletedAt     = _jobState.LastCompletedAt,
        LastSuccessTime     = _jobState.LastSuccessAt,
        LastResultMessage   = _jobState.LastMessage,
        LastError           = _jobState.LastError,
        TotalRuns           = _jobState.TotalRuns,
        LastTotalCandidates = _jobState.LastTotalCandidates,
        LastTotalEvaluated  = _jobState.LastTotalEvaluated,
        LastTotalFailed     = _jobState.LastTotalFailed
    };
}

// ── Response DTOs (evaluation-specific) ─────────────────────────────────────

/// <summary>A single evaluated tender row.</summary>
public class EvaluatedTenderDto
{
    public int Id { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? TenderName { get; set; }
    public string? AgencyName { get; set; }
    public string? BranchName { get; set; }
    public string? TenderStatusName { get; set; }
    public string? SubmitionDate { get; set; }
    public decimal? MatchingScore { get; set; }
    public string? MatchingReason { get; set; }
    public bool? Evaluated { get; set; }
    public DateTime LastSyncedAt { get; set; }
}

/// <summary>Paginated wrapper for the evaluated tenders list endpoint.</summary>
public class EvaluationResultsResponse
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }

    /// <summary>
    /// The <c>minScore</c> filter applied to this response
    /// (<see langword="null"/> = no filter).
    /// </summary>
    public decimal? MinScore { get; set; }

    public List<EvaluatedTenderDto> Data { get; set; } = [];
}
