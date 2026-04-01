namespace EtimadScraper.Configuration;

/// <summary>
/// Settings for the supplier-tender sync job that calls the Etimad JSON API.
/// Bound from appsettings.json section "SupplierTenderSync".
/// </summary>
public class SupplierTenderSyncSettings
{
    public const string SectionName = "SupplierTenderSync";

    /// <summary>Base URL of the Etimad supplier tenders endpoint (no query string).</summary>
    public string BaseUrl { get; set; } =
        "https://tenders.etimad.sa/Tender/AllSupplierTendersForVisitorAsync";

    /// <summary>Number of tenders to request per page (matches Etimad default).</summary>
    public int PageSize { get; set; } = 6;

    /// <summary>Publish-date filter identifier sent to the API.</summary>
    public int PublishDateId { get; set; } = 5;

    /// <summary>Seconds to wait between consecutive HTTP requests to avoid rate-limiting.</summary>
    public int DelayBetweenRequestsSeconds { get; set; } = 5;

    /// <summary>Maximum consecutive empty pages before the sync considers all data fetched.</summary>
    public int MaxConsecutiveEmptyPages { get; set; } = 3;

    /// <summary>How many times to retry a failed page request (transient errors only).</summary>
    public int PageRetryCount { get; set; } = 3;

    /// <summary>Base delay (ms) for exponential back-off between retries.</summary>
    public int RetryBaseDelayMs { get; set; } = 1000;
}
