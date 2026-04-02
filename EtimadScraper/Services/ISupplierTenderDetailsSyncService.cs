using EtimadScraper.Models;

namespace EtimadScraper.Services;

/// <summary>
/// Reads all rows from <c>SupplierTenders</c>, scrapes any missing detail
/// sections from the Etimad endpoints, and persists them into the five
/// detail tables.
/// </summary>
public interface ISupplierTenderDetailsSyncService
{
    /// <summary>
    /// Executes the details sync loop.
    /// For each tender in <c>SupplierTenders</c>:
    /// <list type="bullet">
    ///   <item>Skips if a row for that <c>TenderId</c> already exists in <c>SupplierTendersDetialsMain</c>.</item>
    ///   <item>Otherwise scrapes all five endpoints and saves the five detail rows in a single transaction.</item>
    ///   <item>Waits 30 seconds between iterations.</item>
    /// </list>
    /// </summary>
    Task<SupplierTenderDetailsSyncResult> SyncDetailsAsync(
        CancellationToken cancellationToken = default);
}
