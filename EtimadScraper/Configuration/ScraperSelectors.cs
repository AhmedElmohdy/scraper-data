namespace EtimadScraper.Configuration;

/// <summary>
/// CSS and XPath selectors for scraping tender data from Etimad platform
/// Updated based on actual website structure (2024)
/// </summary>
public static class ScraperSelectors
{
    /// <summary>
    /// Selector for the main tender container/card list
    /// Etimad uses card-based layout, not traditional tables
    /// </summary>
    public const string TenderContainer = ".tenders-list, .tender-cards, div[class*='tender'], .card-body, div[id*='tender']";

    /// <summary>
    /// Selector for individual tender cards/items
    /// More specific to avoid generic layout divs
    /// </summary>
    public const string TenderCard = "div[class*='tender-card'], div[id*='tender'], article, .result-item, li[class*='tender']";

    /// <summary>
    /// Alternative: Wait for ANY content to load
    /// </summary>
    public const string AnyContent = "body, main, #app, #root, .container";

    /// <summary>
    /// Selector for tender number/reference in card
    /// </summary>
    public const string TenderNumber = "span[class*='number'], .tender-number, strong:has-text('???'), [class*='reference']";

    /// <summary>
    /// Selector for tender title in card
    /// </summary>
    public const string TenderTitle = "h3, h4, h5, .tender-title, a[class*='title'], .card-title";

    /// <summary>
    /// Selector for organization/entity name
    /// </summary>
    public const string Organization = "[class*='organization'], [class*='entity'], .agency-name, span:has-text('?????')";

    /// <summary>
    /// Selector for publish date
    /// </summary>
    public const string PublishDate = "[class*='publish'], [class*='date'], span:has-text('?????'), .announcement-date";

    /// <summary>
    /// Selector for closing/deadline date
    /// </summary>
    public const string ClosingDate = "[class*='closing'], [class*='deadline'], span:has-text('?????'), .end-date";

    /// <summary>
    /// Selector for tender details link
    /// </summary>
    public const string DetailsLink = "a[href*='Details'], a[href*='tender'], .details-link, .btn-primary";

    /// <summary>
    /// Selector for tender status
    /// </summary>
    public const string Status = "[class*='status'], .badge, .label, span[class*='state']";

    /// <summary>
    /// Selector for category/department information (highlighted badge - any color)
    /// Example: "?????? ???? ????? ??????? - ????? ?????????"
    /// The badge can be yellow, blue, or any color depending on tender type
    /// </summary>
    public const string Category = ".badge, [class*='badge'], .bg-primary, .bg-warning, .bg-info, .bg-success, span[class*='bg-'], div[class*='bg-'], [style*='background-color'], [style*='background:'], [class*='highlight'], [class*='category'], [class*='department']";

    /// <summary>
    /// Selector for department/sub-category (highlighted section - any color)
    /// Often the same element as category in Etimad's structure
    /// </summary>
    public const string Department = ".badge, [class*='badge'], .bg-primary, .bg-warning, .bg-info, span[class*='bg-'], div[class*='bg-'], [style*='background-color'], [style*='background:'], [class*='department'], [class*='dept'], [class*='sub-category']";

    /// <summary>
    /// Selector for pagination container
    /// </summary>
    public const string PaginationContainer = ".pagination, ul.pagination, nav[aria-label*='page']";

    /// <summary>
    /// Selector for next page button
    /// </summary>
    public const string NextPageButton = ".pagination .next, a[aria-label='Next'], button:has-text('??????')";

    /// <summary>
    /// Indicators that suggest anti-bot protection or captcha
    /// </summary>
    public static readonly string[] AntiBotIndicators = new[]
    {
        "#px-captcha",
        ".g-recaptcha",
        "#cf-challenge-running",
        "[data-ray]", // Cloudflare
        "div[id*='captcha']",
        "iframe[title*='recaptcha']",
        "text='Access Denied'",
        "text='Human Verification'",
        "text='Please verify you are human'",
        "text='?????? ??????'" // Arabic for "Please verify"
    };

    /// <summary>
    /// Selectors for loading indicators
    /// </summary>
    public const string LoadingIndicator = ".loading, .spinner, #loading, [class*='load']";
}
