namespace EtimadScraper.Configuration;

/// <summary>
/// Settings for the supplier-tender <em>details</em> scraping background job.
/// Bound from appsettings.json section <c>"SupplierTenderDetailsSync"</c>.
/// </summary>
public class SupplierTenderDetailsSyncSettings
{
    public const string SectionName = "SupplierTenderDetailsSync";

    /// <summary>Enables or disables the automatic background details-sync job.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often the background job runs (in hours). Default: 6.</summary>
    public int IntervalHours { get; set; } = 6;
}
