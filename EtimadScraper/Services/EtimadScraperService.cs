using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using EtimadScraper.Models;
using EtimadScraper.Configuration;
using System.Text.Json;

namespace EtimadScraper.Services;

/// <summary>
/// Service responsible for scraping tender data from Etimad platform
/// Uses Playwright for browser automation and JavaScript rendering
/// </summary>
public class EtimadScraperService : IDisposable
{
    private readonly ILogger<EtimadScraperService> _logger;
    private readonly ScraperConfiguration _config;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private bool _disposed = false;

    public EtimadScraperService(ILogger<EtimadScraperService> logger, ScraperConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Initialize Playwright and browser
    /// </summary>
    private async Task InitializeAsync()
    {
        if (_browser != null)
            return;

        _logger.LogInformation("Initializing Playwright browser...");
        
        _playwright = await Playwright.CreateAsync();
        
        // Launch browser with configuration
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _config.Headless,
            Args = new[] 
            { 
                "--disable-blink-features=AutomationControlled",
                "--disable-dev-shm-usage",
                "--no-sandbox"
            }
        });

        _logger.LogInformation("Browser initialized successfully");
    }

    /// <summary>
    /// Main method to scrape tenders from multiple pages
    /// </summary>
    /// <param name="startPage">Starting page number</param>
    /// <param name="endPage">Ending page number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scraped tender data</returns>
    public async Task<List<TenderDto>> ScrapeTendersAsync(int startPage, int endPage, CancellationToken cancellationToken = default)
    {
        if (startPage < 1 || endPage < startPage)
        {
            throw new ArgumentException("Invalid page range. Start page must be >= 1 and end page >= start page.");
        }

        var allTenders = new List<TenderDto>();

        try
        {
            await InitializeAsync();

            _logger.LogInformation("Starting tender scraping from page {StartPage} to {EndPage}", startPage, endPage);

            // Scrape each page in the range
            for (int pageNumber = startPage; pageNumber <= endPage; pageNumber++)
            {
                try
                {
                    _logger.LogInformation("Scraping page {PageNumber}...", pageNumber);
                    
                    var tenders = await ScrapePageWithRetryAsync(pageNumber);
                    
                    if (tenders.Count == 0)
                    {
                        _logger.LogWarning("No tenders found on page {PageNumber}. Stopping.", pageNumber);
                        break;
                    }

                    allTenders.AddRange(tenders);
                    _logger.LogInformation("Successfully scraped {Count} tenders from page {PageNumber}", tenders.Count, pageNumber);

                    // Add delay between pages to be respectful to the server
                    if (pageNumber < endPage)
                    {
                        _logger.LogDebug("Waiting {Delay}ms before next page...", _config.DelayBetweenPages);
                        await Task.Delay(_config.DelayBetweenPages);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping page {PageNumber}. Continuing to next page...", pageNumber);
                }
            }

            _logger.LogInformation("Scraping completed. Total tenders scraped: {TotalCount}", allTenders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during scraping process");
            throw;
        }

        return allTenders;
    }

    /// <summary>
    /// Scrape a single page with retry logic
    /// </summary>
    private async Task<List<TenderDto>> ScrapePageWithRetryAsync(int pageNumber)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt < _config.MaxRetryAttempts)
        {
            attempt++;
            
            try
            {
                return await ScrapePageAsync(pageNumber);
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Attempt {Attempt} of {MaxAttempts} failed for page {PageNumber}", 
                    attempt, _config.MaxRetryAttempts, pageNumber);

                if (attempt < _config.MaxRetryAttempts)
                {
                    var delay = attempt * 1000; // Exponential backoff
                    _logger.LogDebug("Waiting {Delay}ms before retry...", delay);
                    await Task.Delay(delay);
                }
            }
        }

        throw new Exception($"Failed to scrape page {pageNumber} after {_config.MaxRetryAttempts} attempts", lastException);
    }

    /// <summary>
    /// Scrape a single page
    /// </summary>
    private async Task<List<TenderDto>> ScrapePageAsync(int pageNumber)
    {
        if (_browser == null)
            throw new InvalidOperationException("Browser not initialized");

        var tenders = new List<TenderDto>();

        // Create a new browser context (isolated session)
        await using var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            Locale = "ar-SA",
            TimezoneId = "Asia/Riyadh"
        });

        // Create a new page
        var page = await context.NewPageAsync();

        try
        {
            // Build URL with page number
            var url = $"{_config.BaseUrl}?PageNumber={pageNumber}";
            _logger.LogDebug("Navigating to {Url}", url);

            // Navigate to the page
            var response = await page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = _config.PageLoadTimeout
            });

            if (response == null || !response.Ok)
            {
                throw new Exception($"Failed to load page. Status: {response?.Status ?? 0}");
            }

            // Check for anti-bot protection
            if (await DetectAntiBotProtectionAsync(page))
            {
                throw new Exception("Anti-bot protection or CAPTCHA detected. Cannot proceed with scraping.");
            }

            // Wait for page content to load - try multiple strategies
            bool contentLoaded = false;
            
            // Strategy 1: Wait for any content
            try
            {
                await page.WaitForSelectorAsync(ScraperSelectors.AnyContent, new PageWaitForSelectorOptions
                {
                    Timeout = 5000,
                    State = WaitForSelectorState.Attached
                });
                contentLoaded = true;
                _logger.LogDebug("Page content loaded");
            }
            catch
            {
                _logger.LogWarning("Basic content selector timed out");
            }

            // Strategy 2: Wait for specific tender container or fallback tender indicators
            try
            {
                await page.WaitForSelectorAsync(ScraperSelectors.TenderContainer, new PageWaitForSelectorOptions
                {
                    Timeout = 7000,
                    State = WaitForSelectorState.Attached
                });
                contentLoaded = true;
                _logger.LogDebug("Tender container found");
            }
            catch
            {
                var fallbackCards = await page.QuerySelectorAllAsync(ScraperSelectors.TenderCard);
                var fallbackLinks = await page.QuerySelectorAllAsync("a[href*='DetailsForVisitor'], a[href*='STenderId=']");

                if (fallbackCards.Count > 0 || fallbackLinks.Count > 0)
                {
                    contentLoaded = true;
                    _logger.LogDebug(
                        "Primary container not found, but fallback tender content exists (cards={Cards}, links={Links}).",
                        fallbackCards.Count,
                        fallbackLinks.Count);
                }
                else
                {
                    _logger.LogWarning("Tender container selector not found and no fallback tender content detected");
                }
            }

            // Strategy 3: Just wait a bit for dynamic content to load
            if (!contentLoaded)
            {
                _logger.LogDebug("Waiting for dynamic content to load...");
                await Task.Delay(3000); // Wait 3 seconds for JavaScript to render
                contentLoaded = true; // Proceed anyway
            }

            _logger.LogDebug("Content detection completed, proceeding with extraction");

            // Extract tender data
            tenders = await ExtractTenderDataAsync(page);

            _logger.LogDebug("Extracted {Count} tenders from page {PageNumber}", tenders.Count, pageNumber);
        }
        finally
        {
            await page.CloseAsync();
        }

        return tenders;
    }

    /// <summary>
    /// Detect if the page shows anti-bot protection or CAPTCHA
    /// </summary>
    private async Task<bool> DetectAntiBotProtectionAsync(IPage page)
    {
        _logger.LogDebug("Checking for anti-bot protection...");

        foreach (var indicator in ScraperSelectors.AntiBotIndicators)
        {
            try
            {
                var element = await page.QuerySelectorAsync(indicator);
                if (element != null)
                {
                    _logger.LogError("Anti-bot protection detected: {Indicator}", indicator);
                    return true;
                }
            }
            catch
            {
                // Ignore selector errors and continue checking
            }
        }

        // Check page title for common protection messages
        var title = await page.TitleAsync();
        var protectionKeywords = new[] { "Access Denied", "Attention Required", "Just a moment", "Verification" };
        
        if (protectionKeywords.Any(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogError("Anti-bot protection detected in page title: {Title}", title);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extract tender data from the page (works with both table and card layouts)
    /// </summary>
    private async Task<List<TenderDto>> ExtractTenderDataAsync(IPage page)
    {
        var tenders = new List<TenderDto>();

        try
        {
            // First, try to find table rows (traditional layout)
            var tableRows = await page.QuerySelectorAllAsync("table tbody tr, table tr");
            if (tableRows.Count > 0)
            {
                _logger.LogDebug("Found {RowCount} table rows, trying table extraction", tableRows.Count);
                foreach (var row in tableRows)
                {
                    try
                    {
                        var tender = await ExtractTenderFromTableRowAsync(row);
                        if (tender != null)
                        {
                            tenders.Add(tender);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to extract from table row");
                    }
                }
            }

            // If table extraction worked, return de-duplicated results
            if (tenders.Count > 0)
            {
                var unique = DeduplicateByBusinessKey(tenders);
                _logger.LogDebug("Successfully extracted {Count} tenders from table ({Unique} unique)", tenders.Count, unique.Count);
                return unique;
            }

            // Try card-based layout
            _logger.LogDebug("No table data found, trying card-based extraction");
            var cards = await page.QuerySelectorAllAsync(ScraperSelectors.TenderCard);
            
            if (cards.Count > 0)
            {
                _logger.LogDebug("Found {CardCount} potential tender cards", cards.Count);
                
                foreach (var card in cards)
                {
                    try
                    {
                        var tender = await ExtractTenderFromCardAsync(card, page);
                        if (tender != null)
                        {
                            tenders.Add(tender);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to extract tender from card");
                    }
                }
            }
            
            // If no cards found, try alternative extraction by looking for links
            if (tenders.Count == 0)
            {
                _logger.LogDebug("No cards found, trying alternative extraction...");
                tenders = await ExtractTendersAlternativeAsync(page);
            }

            // Final in-page de-duplication by business key
            tenders = DeduplicateByBusinessKey(tenders);

            // If still nothing, take screenshot for debugging
            if (tenders.Count == 0)
            {
                _logger.LogWarning("No tenders extracted with any method, capturing debug info");
                await CaptureDebugScreenshotAsync(page, 0);
                
                var pageContent = await page.ContentAsync();
                _logger.LogDebug("Page HTML (first 1000 chars): {Content}", 
                    pageContent.Length > 1000 ? pageContent.Substring(0, 1000) : pageContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting tender data from page");
            throw;
        }

        return tenders;
    }

    /// <summary>
    /// Extract tender from traditional table row
    /// </summary>
    private async Task<TenderDto?> ExtractTenderFromTableRowAsync(IElementHandle row)
    {
        try
        {
            // Check if row has enough cells
            var cells = await row.QuerySelectorAllAsync("td");
            if (cells.Count < 2)
            {
                return null; // Not a data row
            }

            var tender = new TenderDto();

            // Extract from table cells (typical Etimad layout)
            if (cells.Count >= 1)
            {
                var cell0Text = await cells[0].TextContentAsync();
                tender.TenderNumber = cell0Text?.Trim() ?? string.Empty;
            }

            if (cells.Count >= 2)
            {
                var cell1 = cells[1];
                var titleLink = await cell1.QuerySelectorAsync("a");
                if (titleLink != null)
                {
                    tender.Title = (await titleLink.TextContentAsync())?.Trim() ?? string.Empty;
                    var href = await titleLink.GetAttributeAsync("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        tender.DetailsUrl = href.StartsWith("http") ? href : $"https://tenders.etimad.sa{href}";
                    }
                }
                else
                {
                    tender.Title = (await cell1.TextContentAsync())?.Trim() ?? string.Empty;
                }
            }

            if (cells.Count >= 3)
            {
                tender.Organization = (await cells[2].TextContentAsync())?.Trim() ?? string.Empty;
            }

            if (cells.Count >= 4)
            {
                tender.PublishDate = (await cells[3].TextContentAsync())?.Trim() ?? string.Empty;
            }

            if (cells.Count >= 5)
            {
                tender.ClosingDate = (await cells[4].TextContentAsync())?.Trim() ?? string.Empty;
            }

            if (cells.Count >= 6)
            {
                tender.Status = (await cells[5].TextContentAsync())?.Trim() ?? string.Empty;
            }

            tender.ScrapedAt = DateTime.UtcNow;

            // Validation
            if (string.IsNullOrWhiteSpace(tender.Title) && string.IsNullOrWhiteSpace(tender.TenderNumber))
            {
                return null;
            }

            return tender;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract from table row");
            return null;
        }
    }

    /// <summary>
    /// Alternative extraction method - looks for any tender links or structured data
    /// </summary>
    private async Task<List<TenderDto>> ExtractTendersAlternativeAsync(IPage page)
    {
        var tenders = new List<TenderDto>();
        
        try
        {
            // Look for real tender details links only (avoid generic links)
            var links = await page.QuerySelectorAllAsync("a[href*='DetailsForVisitor'], a[href*='STenderId=']");
            
            _logger.LogDebug("Found {LinkCount} potential tender links", links.Count);
            
            foreach (var link in links)
            {
                try
                {
                    var href = await link.GetAttributeAsync("href");
                    var text = await link.TextContentAsync();
                    
                    if (!string.IsNullOrWhiteSpace(href))
                    {
                        var absoluteUrl = href.StartsWith("http") ? href : $"https://tenders.etimad.sa{href}";

                        // Keep only real tender details URLs
                        if (!(absoluteUrl.Contains("DetailsForVisitor", StringComparison.OrdinalIgnoreCase) ||
                              absoluteUrl.Contains("STenderId=", StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        var tender = new TenderDto
                        {
                            Title = text?.Trim() ?? string.Empty,
                            DetailsUrl = absoluteUrl,
                            ScrapedAt = DateTime.UtcNow
                        };
                        
                        // Try to find related information in parent elements
                        var parent = await link.EvaluateHandleAsync("el => el.closest('div, li, tr')");
                        if (parent != null)
                        {
                            var parentElement = parent.AsElement();
                            if (parentElement != null)
                            {
                                var parentText = await parentElement.TextContentAsync();
                                // Try to extract additional info from parent text
                                tender.AdditionalInfo = parentText?.Trim() ?? string.Empty;
                            }
                        }
                        
                        tenders.Add(tender);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to extract info from link");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alternative extraction failed");
        }
        
        return tenders;
    }

    private static List<TenderDto> DeduplicateByBusinessKey(List<TenderDto> tenders)
    {
        if (tenders.Count <= 1)
            return tenders;

        return tenders
            .Where(t => !string.IsNullOrWhiteSpace(t.DetailsUrl) || !string.IsNullOrWhiteSpace(t.TenderNumber))
            .GroupBy(
                t => !string.IsNullOrWhiteSpace(t.DetailsUrl)
                    ? t.DetailsUrl.Trim()
                    : t.TenderNumber.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Extract tender information from a card element (modern layout)
    /// </summary>
    private async Task<TenderDto?> ExtractTenderFromCardAsync(IElementHandle card, IPage page)
    {
        try
        {
            var tender = new TenderDto();

            // Extract title
            var titleElement = await card.QuerySelectorAsync(ScraperSelectors.TenderTitle);
            if (titleElement != null)
            {
                tender.Title = (await titleElement.TextContentAsync())?.Trim() ?? string.Empty;
            }

            // Extract details URL
            var linkElement = await card.QuerySelectorAsync(ScraperSelectors.DetailsLink);
            if (linkElement != null)
            {
                var href = await linkElement.GetAttributeAsync("href");
                if (!string.IsNullOrEmpty(href))
                {
                    tender.DetailsUrl = href.StartsWith("http") 
                        ? href 
                        : $"https://tenders.etimad.sa{href}";
                }
            }

            if (string.IsNullOrWhiteSpace(tender.DetailsUrl) ||
                !(tender.DetailsUrl.Contains("DetailsForVisitor", StringComparison.OrdinalIgnoreCase) ||
                  tender.DetailsUrl.Contains("STenderId=", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            // Extract all text content from card for additional parsing
            var cardText = await card.TextContentAsync();
            tender.AdditionalInfo = cardText?.Trim() ?? string.Empty;

            // Try to extract structured data
            tender.TenderNumber = await ExtractTextAsync(card, ScraperSelectors.TenderNumber) ?? string.Empty;
            tender.Organization = await ExtractTextAsync(card, ScraperSelectors.Organization) ?? string.Empty;
            tender.PublishDate = await ExtractTextAsync(card, ScraperSelectors.PublishDate) ?? string.Empty;
            tender.ClosingDate = await ExtractTextAsync(card, ScraperSelectors.ClosingDate) ?? string.Empty;
            tender.Status = await ExtractTextAsync(card, ScraperSelectors.Status) ?? string.Empty;
            
            // Extract category/department (blue badge) - try multiple strategies
            tender.Category = await ExtractCategoryDepartmentAsync(card, ScraperSelectors.Category) ?? string.Empty;
            tender.Department = await ExtractCategoryDepartmentAsync(card, ScraperSelectors.Department) ?? string.Empty;
            
            // If Category and Department are the same, keep both populated
            // (they often refer to the same blue badge element in Etimad's structure)
            if (string.IsNullOrEmpty(tender.Category) && !string.IsNullOrEmpty(tender.Department))
            {
                tender.Category = tender.Department;
            }
            else if (string.IsNullOrEmpty(tender.Department) && !string.IsNullOrEmpty(tender.Category))
            {
                tender.Department = tender.Category;
            }

            // Set scraped timestamp
            tender.ScrapedAt = DateTime.UtcNow;

            // Basic validation - must have at least title or URL
            if (string.IsNullOrWhiteSpace(tender.Title) && string.IsNullOrWhiteSpace(tender.DetailsUrl))
            {
                _logger.LogDebug("Skipping card with no title or URL");
                return null;
            }

            return tender;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract tender from card");
            return null;
        }
    }

    /// <summary>
    /// Helper method to extract text from an element using a selector
    /// </summary>
    private async Task<string?> ExtractTextAsync(IElementHandle parent, string selector)
    {
        try
        {
            var element = await parent.QuerySelectorAsync(selector);
            if (element != null)
            {
                var text = await element.TextContentAsync();
                return text?.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract text with selector: {Selector}", selector);
        }

        return null;
    }

    /// <summary>
    /// Enhanced method to extract category/department information (colored badge)
    /// Tries multiple strategies to find any highlighted badge (yellow, blue, or any color)
    /// </summary>
    private async Task<string?> ExtractCategoryDepartmentAsync(IElementHandle parent, string selector)
    {
        try
        {
            // Strategy 1: Try the provided selector
            var element = await parent.QuerySelectorAsync(selector);
            if (element != null)
            {
                var text = await element.TextContentAsync();
                var trimmedText = text?.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedText) && trimmedText.Length > 5)
                {
                    _logger.LogDebug("Found category/department with primary selector: {Text}", trimmedText);
                    return trimmedText;
                }
            }

            // Strategy 2: Look for any badge elements (most common in Bootstrap/modern sites)
            var badges = await parent.QuerySelectorAllAsync(".badge, [class*='badge'], span[class*='bg-'], div[class*='bg-']");
            foreach (var badge in badges)
            {
                var text = await badge.TextContentAsync();
                var trimmedText = text?.Trim();
                
                // Check if it contains meaningful Arabic text (likely the department)
                // Must be longer than 5 chars and contain Arabic characters
                if (!string.IsNullOrWhiteSpace(trimmedText) && 
                    trimmedText.Length > 10 && 
                    System.Text.RegularExpressions.Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]"))
                {
                    _logger.LogDebug("Found category/department in badge: {Text}", trimmedText);
                    return trimmedText;
                }
            }

            // Strategy 3: Look for ANY element with background color (yellow, blue, green, etc.)
            var styledElements = await parent.QuerySelectorAllAsync("[style*='background']");
            foreach (var styledElement in styledElements)
            {
                var style = await styledElement.GetAttributeAsync("style");
                
                // Check if the element has ANY background color
                if (style != null && (
                    style.Contains("background-color") || 
                    style.Contains("background:") ||
                    style.Contains("background ")
                ))
                {
                    var text = await styledElement.TextContentAsync();
                    var trimmedText = text?.Trim();
                    
                    // Must contain Arabic and be reasonably long
                    if (!string.IsNullOrWhiteSpace(trimmedText) && 
                        trimmedText.Length > 10 && 
                        System.Text.RegularExpressions.Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]"))
                    {
                        _logger.LogDebug("Found category/department in styled element: {Text}", trimmedText);
                        return trimmedText;
                    }
                }
            }

            // Strategy 4: Look for span elements with text containing department keywords
            var spans = await parent.QuerySelectorAllAsync("span, div");
            foreach (var span in spans)
            {
                var text = await span.TextContentAsync();
                var trimmedText = text?.Trim();
                
                // Look for text containing common department keywords
                if (!string.IsNullOrWhiteSpace(trimmedText) && 
                    trimmedText.Length > 10 &&
                    trimmedText.Length < 200 && // Not too long (not the whole card)
                    System.Text.RegularExpressions.Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]") &&
                    (trimmedText.Contains("?????") || // Administration
                     trimmedText.Contains("??????") || // Hospital
                     trimmedText.Contains("????") || // Authority
                     trimmedText.Contains("?????????") || // Purchases
                     trimmedText.Contains("???????"))) // Financial
                {
                    // Make sure this isn't just generic text - check if it has a special class or style
                    var className = await span.GetAttributeAsync("class");
                    var spanStyle = await span.GetAttributeAsync("style");
                    
                    if (!string.IsNullOrWhiteSpace(className) || !string.IsNullOrWhiteSpace(spanStyle))
                    {
                        _logger.LogDebug("Found category/department by keyword match: {Text}", trimmedText);
                        return trimmedText;
                    }
                }
            }

            _logger.LogDebug("No category/department found with selector: {Selector}", selector);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract category/department with selector: {Selector}", selector);
        }

        return null;
    }

    /// <summary>
    /// Capture screenshot for debugging when selectors fail
    /// </summary>
    private async Task CaptureDebugScreenshotAsync(IPage page, int pageNumber)
    {
        try
        {
            var screenshotPath = $"debug_page_{pageNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = true
            });
            _logger.LogInformation("Debug screenshot saved to: {Path}", screenshotPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture debug screenshot");
        }
    }

    /// <summary>
    /// Save scraped tenders to JSON file
    /// </summary>
    public async Task SaveToJsonAsync(List<TenderDto> tenders, string? filePath = null)
    {
        var outputPath = filePath ?? _config.OutputFilePath;

        try
        {
            _logger.LogInformation("Saving {Count} tenders to {FilePath}", tenders.Count, outputPath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(tenders, options);
            await File.WriteAllTextAsync(outputPath, json);

            _logger.LogInformation("Data saved successfully to {FilePath}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save data to JSON file: {FilePath}", outputPath);
            throw;
        }
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _browser?.DisposeAsync().AsTask().Wait();
            _playwright?.Dispose();
        }

        _disposed = true;
    }
}
