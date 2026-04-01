namespace EtimadScraper.Configuration;

/// <summary>
/// Configuration settings for the Etimad scraper
/// </summary>
public class ScraperConfiguration
{
    /// <summary>
    /// Base URL for Etimad tenders
    /// </summary>
    public string BaseUrl { get; set; } = "https://tenders.etimad.sa/Tender/AllTendersForVisitor";

    /// <summary>
    /// Run browser in headless mode (no UI)
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Maximum number of pages to scrape
    /// </summary>
    public int MaxPages { get; set; } = 5;

    /// <summary>
    /// Starting page number
    /// </summary>
    public int StartPage { get; set; } = 1;

    /// <summary>
    /// Timeout in milliseconds for page load
    /// </summary>
    public int PageLoadTimeout { get; set; } = 30000;

    /// <summary>
    /// Delay between page requests in milliseconds
    /// </summary>
    public int DelayBetweenPages { get; set; } = 2000;

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Output file path for scraped data
    /// </summary>
    public string OutputFilePath { get; set; } = "tenders.json";
}
