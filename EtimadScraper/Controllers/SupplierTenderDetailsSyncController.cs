using EtimadScraper.Models;
using EtimadScraper.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtimadScraper.Controllers;

/// <summary>
/// Triggers scraping of detailed tender information for every tender in
/// the <c>SupplierTenders</c> table that does not yet have details stored.
///
/// POST /api/supplier-tenders/sync-details
/// </summary>
[ApiController]
[Route("api/supplier-tenders")]
public class SupplierTenderDetailsSyncController : ControllerBase
{
    private readonly ISupplierTenderDetailsSyncService _syncService;
    private readonly ILogger<SupplierTenderDetailsSyncController> _logger;

    public SupplierTenderDetailsSyncController(
        ISupplierTenderDetailsSyncService syncService,
        ILogger<SupplierTenderDetailsSyncController> logger)
    {
        _syncService = syncService;
        _logger      = logger;
    }

    /// <summary>
    /// Iterates all records in <c>SupplierTenders</c>, skips any tender
    /// whose details already exist, and scrapes + saves details for the rest.
    /// A 30-second delay is enforced between each scrape to avoid rate-limiting.
    ///
    /// ?? This is a long-running operation. For production use with many
    /// tenders, consider triggering it via a background job instead.
    /// </summary>
    /// <param name="cancellationToken">Standard ASP.NET Core request cancellation token.</param>
    /// <returns>
    /// 200 OK with a <see cref="SupplierTenderDetailsSyncResult"/> on full success,
    /// or 207 Multi-Status when some tenders failed but others succeeded,
    /// or 500 Internal Server Error if the sync itself could not complete.
    /// </returns>
    [HttpPost("sync-details")]
    [ProducesResponseType(typeof(SupplierTenderDetailsSyncResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(SupplierTenderDetailsSyncResult), StatusCodes.Status207MultiStatus)]
    [ProducesResponseType(typeof(SupplierTenderDetailsSyncResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SyncDetails(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "POST /api/supplier-tenders/sync-details — starting details sync.");

        var result = await _syncService.SyncDetailsAsync(cancellationToken);

        if (!result.Success && result.TotalInserted == 0 && result.TotalSkipped == 0)
            return StatusCode(StatusCodes.Status500InternalServerError, result);

        if (result.TotalFailed > 0)
            return StatusCode(StatusCodes.Status207MultiStatus, result);

        return Ok(result);
    }
}
