namespace EtimadScraper.Configuration;

/// <summary>
/// Settings for the full-scrape background job.
/// Bound from appsettings.json section "ScrapingJob".
/// </summary>
public class ScrapingJobSettings
{
    public const string SectionName = "ScrapingJob";

    /// <summary>
    /// Enables/disables automatic and manual job execution.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How many tenders to collect before calling SaveChangesAsync.</summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>Maximum consecutive empty pages before the job declares "no more data".</summary>
    public int MaxConsecutiveEmptyPages { get; set; } = 2;

    /// <summary>Absolute maximum number of pages to scrape (safety cap, 0 = unlimited).</summary>
    public int MaxPagesToScrape { get; set; } = 0;

    /// <summary>Milliseconds to wait between page requests.</summary>
    public int DelayBetweenPagesMs { get; set; } = 2000;

    /// <summary>How many times to retry a page on transient failure before giving up.</summary>
    public int PageRetryCount { get; set; } = 3;

    /// <summary>Base delay in milliseconds for exponential back-off between retries.</summary>
    public int RetryBaseDelayMs { get; set; } = 1000;
}
