using EtimadScraper.Models;

namespace EtimadScraper.Services;

/// <summary>
/// Contract for the supplier-tender sync service that pulls data from the
/// Etimad JSON endpoint and persists it in SQL Server.
/// </summary>
public interface ISupplierTenderSyncService
{
    /// <summary>
    /// Fetches all pages from the Etimad supplier-tenders API, upserts every
    /// tender into SQL Server, and returns a summary result.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel a long-running sync.</param>
    Task<SupplierTenderSyncResult> SyncAllAsync(CancellationToken cancellationToken = default);
}
