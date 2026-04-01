using EtimadScraper.Models;
using EtimadScraper.Services;
using Microsoft.AspNetCore.Mvc;

namespace EtimadScraper.Controllers;

/// <summary>
/// Exposes an endpoint to trigger a full sync of all supplier tenders from
/// the Etimad JSON API into SQL Server.
///
/// POST /api/tenders/sync
/// </summary>
[ApiController]
[Route("api/tenders")]
public class SupplierTenderSyncController : ControllerBase
{
    private readonly ISupplierTenderSyncService _syncService;
    private readonly ILogger<SupplierTenderSyncController> _logger;

    public SupplierTenderSyncController(
        ISupplierTenderSyncService syncService,
        ILogger<SupplierTenderSyncController> logger)
    {
        _syncService = syncService;
        _logger      = logger;
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
}
