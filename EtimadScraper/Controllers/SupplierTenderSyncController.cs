using EtimadScraper.Configuration;
using EtimadScraper.Data;
using EtimadScraper.Models;
using EtimadScraper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EtimadScraper.Controllers;

/// <summary>
/// Exposes endpoints to manage and query supplier tenders synced from the
/// Etimad JSON API.
///
/// POST /api/tenders/sync
/// GET  /api/tenders/list
/// </summary>
[ApiController]
[Route("api/tenders-integration")]
public class SupplierTenderSyncController : ControllerBase
{
    private readonly ISupplierTenderSyncService _syncService;
    private readonly TenderDbContext _db;
    private readonly TenderEvaluationSettings _evaluationSettings;
    private readonly ILogger<SupplierTenderSyncController> _logger;

    public SupplierTenderSyncController(
        ISupplierTenderSyncService syncService,
        TenderDbContext db,
        TenderEvaluationSettings evaluationSettings,
        ILogger<SupplierTenderSyncController> logger)
    {
        _syncService         = syncService;
        _db                  = db;
        _evaluationSettings  = evaluationSettings;
        _logger              = logger;
    }

    /// <summary>
    /// Triggers a full sync of all supplier tenders from the Etimad JSON API.
    ///
    /// The process:
    /// 1. Fetches page 1 to determine the total number of pages.
    /// 2. Iterates through every page, waiting 5 seconds between requests.
    /// 3. Upserts each tender into the SupplierTenders table.
    /// 4. Returns a summary of inserted / updated / failed records.
    ///
    /// Note: this is a long-running operation. Consider running it via a
    /// background job for production use with many pages.
    /// </summary>
    /// <param name="cancellationToken">Standard ASP.NET Core request cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="SupplierTenderSyncResult"/> on success, or
    /// 500 Internal Server Error with the same model when errors occurred.
    /// </returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SupplierTenderSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SupplierTenderSyncResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/tenders/sync received – starting supplier tender sync.");

        var result = await _syncService.SyncAllAsync(cancellationToken);

        return result.Success
            ? Ok(result)
            : StatusCode(StatusCodes.Status500InternalServerError, result);
    }

    /// <summary>
    /// Fetches every page from the Etimad supplier-tenders API (30-second delay
    /// between requests) and updates all existing rows, inserting any new ones.
    ///
    /// Unlike <c>POST /sync</c>, this endpoint never stops early when it encounters
    /// an already-known TenderId — it refreshes the entire dataset.
    /// </summary>
    /// <param name="cancellationToken">Standard ASP.NET Core request cancellation token.</param>
    [HttpPost("update-all")]
    [ProducesResponseType(typeof(SupplierTenderSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SupplierTenderSyncResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/tenders-integration/update-all received – starting full update.");

        var result = await _syncService.UpdateAllAsync(cancellationToken);

        return result.Success
            ? Ok(result)
            : StatusCode(StatusCodes.Status500InternalServerError, result);
    }

    /// <summary>
    /// Updates the <c>InOutStatus</c> field of a supplier tender identified by
    /// <paramref name="tenderIdString"/>.
    /// </summary>
    /// <param name="tenderIdString">The TenderIdString of the record to update.</param>
    /// <param name="request">Request body containing the new status value.</param>
    /// <param name="cancellationToken">Standard ASP.NET Core cancellation token.</param>
    [HttpPatch("{tenderId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        int tenderId,
        [FromBody] UpdateInOutStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest("Status value must not be empty.");

        var tender = await _db.SupplierTenders
            .FirstOrDefaultAsync(t => t.Id == tenderId, cancellationToken);

        if (tender is null)
            return NotFound($"No tender found with TenderIdString '{tenderId}'.");

        tender.InOutStatus  = request.Status;
        tender.LastSyncedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "InOutStatus for TenderIdString '{TenderIdString}' updated to '{Status}'.",
            tenderId, request.Status);

        return NoContent();
    }

    /// <summary>
    /// Returns three counts of supplier tenders from the local database,
    /// grouped by SubmitionDate: today, yesterday, and the last 7 days.
    /// </summary>
    /// <param name="cancellationToken">Standard ASP.NET Core cancellation token.</param>
    [HttpGet("counts")]
    [ProducesResponseType(typeof(SupplierTenderCountSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Counts(CancellationToken cancellationToken)
    {
        var today     = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var sevenDaysAgo = today.AddDays(-6);

        var rows = await _db.SupplierTenders
            .AsNoTracking()
            .Where(t => t.SubmitionDate != null)
            .Select(t => t.SubmitionDate!)
            .ToListAsync(cancellationToken);

        int todayCount     = 0;
        int yesterdayCount = 0;
        int last7DaysCount = 0;

        foreach (var raw in rows)
        {
            if (!DateTime.TryParse(raw, out var parsed))
                continue;

            var date = parsed.Date;

            if (date == today)
                todayCount++;

            if (date == yesterday)
                yesterdayCount++;

            if (date >= sevenDaysAgo && date <= today)
                last7DaysCount++;
        }

        return Ok(new SupplierTenderCountSummaryDto
        {
            TodayCount     = todayCount,
            YesterdayCount = yesterdayCount,
            Last7DaysCount = last7DaysCount,
        });
    }

    /// <summary>
    /// Returns a statistics summary of supplier tenders grouped by
    /// <c>InOutStatus</c> (IN / OUT / PENDING) and <c>SubmitionDate</c>.
    ///
    /// Cards returned:
    /// - Total Tenders (all + today)
    /// - Matching breakdown (IN / OUT / PENDING counts across all tenders that have an InOutStatus)
    /// - Total IN (all + today)
    /// </summary>
    /// <param name="cancellationToken">Standard ASP.NET Core cancellation token.</param>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(TenderStatisticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Statistics(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var rows = await _db.SupplierTenders
            .AsNoTracking()
            .Select(t => new
            {
                t.InOutStatus,
                t.SubmitionDate,
                t.Evaluated,
            })
            .ToListAsync(cancellationToken);

        int totalTenders      = rows.Count;
        int totalTodayTenders = 0;
        int matchingTotal     = 0;
        int matchingIn        = 0;
        int matchingOut       = 0;
        int matchingPending   = 0;
        int totalIn           = 0;
        int totalInToday      = 0;
        int evaluatedTotal    = 0;
        int evaluatedToday    = 0;

        foreach (var row in rows)
        {
            // Parse SubmitionDate once
            DateTime? submitionDate = null;
            if (row.SubmitionDate != null && DateTime.TryParse(row.SubmitionDate, out var parsed))
                submitionDate = parsed.Date;

            bool isToday = submitionDate.HasValue && submitionDate.Value == today;

            if (isToday)
                totalTodayTenders++;

            // Evaluated card
            if (row.Evaluated == true)
            {
                evaluatedTotal++;
                if (isToday)
                    evaluatedToday++;
            }

            // Matching card: count every row that has a non-null InOutStatus
            if (!string.IsNullOrWhiteSpace(row.InOutStatus))
            {
                matchingTotal++;

                var status = row.InOutStatus.Trim().ToUpperInvariant();

                if (status == "IN")
                {
                    matchingIn++;
                    totalIn++;

                    if (isToday)
                        totalInToday++;
                }
                else if (status == "OUT")
                {
                    matchingOut++;
                }
                else
                {
                    // PENDING or any other value
                    matchingPending++;
                }
            }
        }

        return Ok(new TenderStatisticsDto
        {
            TotalTenders      = totalTenders,
            TotalTodayTenders = totalTodayTenders,
            MatchingTotal     = matchingTotal,
            MatchingIn        = matchingIn,
            MatchingOut       = matchingOut,
            MatchingPending   = matchingPending,
            TotalIn           = totalIn,
            TotalInToday      = totalInToday,
            EvaluatedTotal    = evaluatedTotal,
            EvaluatedToday    = evaluatedToday,
        });
    }

    /// <summary>
    /// Returns a paginated list of supplier tenders from the local database,
    /// sorted by SubmitionDate descending.
    ///
    /// Optional filters: tenderName, branchName, agencyName (case-insensitive, partial match).
    /// </summary>
    /// <param name="page">Page number, 1-based (default: 1).</param>
    /// <param name="pageSize">Number of records per page (default: 10, max: 100).</param>
    /// <param name="tenderName">Optional filter – partial match on TenderName.</param> 
    /// <param name="branchName">Optional filter – partial match on BranchName.</param>
    /// <param name="agencyName">Optional filter – partial match on AgencyName.</param>
    /// <param name="matchedScore">When true, returns only tenders with MatchingScore &gt;= MinMatchingScore from settings.</param>
    /// <param name="cancellationToken">Standard ASP.NET Core cancellation token.</param>
    [HttpGet("list")]
    [ProducesResponseType(typeof(SupplierTenderListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int    page          = 1,
        [FromQuery] int    pageSize      = 10,
        [FromQuery] string? tenderName   = null,
        [FromQuery] string? branchName   = null,
        [FromQuery] string? agencyName   = null,
        [FromQuery] bool    matchedScore = false,
        CancellationToken cancellationToken = default)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.SupplierTenders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(tenderName))
            query = query.Where(t => t.TenderName != null &&
                                     t.TenderName.Contains(tenderName));

        if (!string.IsNullOrWhiteSpace(branchName))
            query = query.Where(t => t.BranchName != null &&
                                     t.BranchName.Contains(branchName));

        if (!string.IsNullOrWhiteSpace(agencyName))
            query = query.Where(t => t.AgencyName != null &&
                                     t.AgencyName.Contains(agencyName));

        if (matchedScore)
            query = query.Where(t => t.MatchingScore != null &&
                                     t.MatchingScore >= _evaluationSettings.MinMatchingScore);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.SubmitionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new SupplierTenderListItemDto
            {
                Id                              = t.Id,
                TenderId                        = t.TenderIdString,
                ReferenceNumber                 = t.ReferenceNumber,
                TenderName                      = t.TenderName,
                TenderNumber                    = t.TenderNumber,
                BranchName                      = t.BranchName,
                AgencyName                      = t.AgencyName,
                TenderTypeName                  = t.TenderTypeName,
                TenderStatusName                = t.TenderStatusName,
                TenderStatusIdString            = t.TenderStatusIdString,
                SubmitionDate                   = t.SubmitionDate,
                LastEnqueriesDate               = t.LastEnqueriesDate,
                LastEnqueriesDateHijri          = t.LastEnqueriesDateHijri,
                LastOfferPresentationDate       = t.LastOfferPresentationDate,
                LastOfferPresentationDateHijri  = t.LastOfferPresentationDateHijri,
                OffersOpeningDate               = t.OffersOpeningDate,
                OffersOpeningDateHijri          = t.OffersOpeningDateHijri,
                RemainingDays                   = t.RemainingDays,
                RemainingHours                  = t.RemainingHours,
                RemainingMins                   = t.RemainingMins,
                FinancialFees                   = t.FinancialFees,
                InvitationCost                  = t.InvitationCost,
                BuyingCost                      = t.BuyingCost,
                CondetionalBookletPrice         = t.CondetionalBookletPrice,
                HasInvitations                  = t.HasInvitations,
                IsUGRP                          = t.IsUGRP,
                UgrpRfxUrl                      = t.UgrpRfxUrl,
                UgrpRFXResponseURL              = t.UgrpRFXResponseURL,
                MatchingScore                   = t.MatchingScore,
                MatchingReason                  = t.MatchingReason,
                Evaluated                       = t.Evaluated,
                InOutStatus                     = t.InOutStatus,
            })
            .ToListAsync(cancellationToken);

        var response = new SupplierTenderListResponse
        {
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Data       = items,
        };

        return Ok(response);
    }
}
