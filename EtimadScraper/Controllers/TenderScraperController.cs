using Microsoft.AspNetCore.Mvc;
using EtimadScraper.Configuration;
using EtimadScraper.Models;
using EtimadScraper.Services;

namespace EtimadScraper.Controllers;

/// <summary>
/// API Controller for managing tender scraping operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TenderScraperController : ControllerBase
{
    private readonly ILogger<TenderScraperController> _logger;
    private readonly EtimadScraperService _scraperService;
    private readonly ScraperConfiguration _config;

    public TenderScraperController(
        ILogger<TenderScraperController> logger,
        EtimadScraperService scraperService,
        ScraperConfiguration config)
    {
        _logger = logger;
        _scraperService = scraperService;
        _config = config;
    }

    /// <summary>
    /// Get scraper configuration
    /// </summary>
    /// <returns>Current scraper configuration</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ScraperConfiguration), StatusCodes.Status200OK)]
    public IActionResult GetConfiguration()
    {
        return Ok(_config);
    }

    /// <summary>
    /// Start scraping tenders with default configuration
    /// </summary>
    /// <returns>List of scraped tenders</returns>
    [HttpPost("scrape")]
    [ProducesResponseType(typeof(ScraperResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ScrapeTenders()
    {
        try
        {
            _logger.LogInformation("Scraping request received via API");

            var endPage = _config.StartPage + _config.MaxPages - 1;
            var tenders = await _scraperService.ScrapeTendersAsync(_config.StartPage, endPage);

            // Save to JSON file
            await _scraperService.SaveToJsonAsync(tenders);

            var response = new ScraperResponse
            {
                Success = true,
                Message = $"Successfully scraped {tenders.Count} tenders",
                TotalCount = tenders.Count,
                StartPage = _config.StartPage,
                EndPage = endPage,
                Tenders = tenders,
                ScrapedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during scraping");
            
            var errorResponse = CreateErrorResponse(ex, _config.StartPage, _config.StartPage + _config.MaxPages - 1);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Start scraping tenders with custom page range
    /// </summary>
    /// <param name="request">Scraping request parameters</param>
    /// <returns>List of scraped tenders</returns>
    [HttpPost("scrape/custom")]
    [ProducesResponseType(typeof(ScraperResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ScrapeCustomRange([FromBody] ScraperRequest request)
    {
        if (request.StartPage < 1 || request.EndPage < request.StartPage)
        {
            return BadRequest(new { error = "Invalid page range. StartPage must be >= 1 and EndPage >= StartPage" });
        }

        if (request.EndPage - request.StartPage + 1 > 50)
        {
            return BadRequest(new { error = "Maximum 50 pages allowed per request" });
        }

        try
        {
            _logger.LogInformation("Custom scraping request: Pages {Start} to {End}", request.StartPage, request.EndPage);

            var tenders = await _scraperService.ScrapeTendersAsync(request.StartPage, request.EndPage);

            // Save to JSON file if requested
            if (request.SaveToFile)
            {
                var fileName = request.OutputFileName ?? $"tenders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                await _scraperService.SaveToJsonAsync(tenders, fileName);
            }

            var response = new ScraperResponse
            {
                Success = true,
                Message = $"Successfully scraped {tenders.Count} tenders from pages {request.StartPage} to {request.EndPage}",
                TotalCount = tenders.Count,
                StartPage = request.StartPage,
                EndPage = request.EndPage,
                Tenders = tenders,
                ScrapedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during custom range scraping");
            
            var errorResponse = CreateErrorResponse(ex, request.StartPage, request.EndPage);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Get scraping status (health check)
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "healthy",
            service = "Etimad Tender Scraper API",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            configuration = new
            {
                baseUrl = _config.BaseUrl,
                headless = _config.Headless,
                maxPages = _config.MaxPages,
                startPage = _config.StartPage
            }
        });
    }

    /// <summary>
    /// Scrape a single page
    /// </summary>
    /// <param name="pageNumber">Page number to scrape</param>
    [HttpGet("scrape/page/{pageNumber}")]
    [ProducesResponseType(typeof(ScraperResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ScrapeSinglePage(int pageNumber)
    {
        if (pageNumber < 1)
        {
            return BadRequest(new { error = "Page number must be >= 1" });
        }

        try
        {
            _logger.LogInformation("Scraping single page: {PageNumber}", pageNumber);

            var tenders = await _scraperService.ScrapeTendersAsync(pageNumber, pageNumber);

            var response = new ScraperResponse
            {
                Success = true,
                Message = $"Successfully scraped {tenders.Count} tenders from page {pageNumber}",
                TotalCount = tenders.Count,
                StartPage = pageNumber,
                EndPage = pageNumber,
                Tenders = tenders,
                ScrapedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping page {PageNumber}", pageNumber);
            
            var errorResponse = CreateErrorResponse(ex, pageNumber, pageNumber);
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Create a detailed error response with helpful troubleshooting information
    /// </summary>
    private ScraperResponse CreateErrorResponse(Exception ex, int startPage, int endPage)
    {
        var errorMessage = ex.Message;
        var troubleshooting = new List<string>();

        // Check for Playwright installation issues
        if (errorMessage.Contains("Executable doesn't exist") || 
            errorMessage.Contains("playwright.ps1 install"))
        {
            troubleshooting.Add("? Playwright browsers are not installed");
            troubleshooting.Add("?? Solution: Run this command in PowerShell:");
            troubleshooting.Add("   cd EtimadScraper");
            troubleshooting.Add("   pwsh bin/Debug/net9.0/playwright.ps1 install chromium");
            troubleshooting.Add("");
            troubleshooting.Add("Or if built in Release mode:");
            troubleshooting.Add("   pwsh bin/Release/net9.0/playwright.ps1 install chromium");
            
            errorMessage = "Playwright browsers not installed. Please install chromium browser.";
        }
        // Check for anti-bot protection
        else if (errorMessage.Contains("Anti-bot protection") || 
                 errorMessage.Contains("CAPTCHA"))
        {
            troubleshooting.Add("? Anti-bot protection detected on the website");
            troubleshooting.Add("?? Possible solutions:");
            troubleshooting.Add("   1. Increase delay between pages (currently: " + _config.DelayBetweenPages + "ms)");
            troubleshooting.Add("   2. Reduce number of pages to scrape");
            troubleshooting.Add("   3. Try again later");
            troubleshooting.Add("   4. Check if website is accessible in browser");
        }
        // Check for network issues
        else if (errorMessage.Contains("timeout") || 
                 errorMessage.Contains("network") ||
                 errorMessage.Contains("Failed to load page"))
        {
            troubleshooting.Add("? Network or timeout issue");
            troubleshooting.Add("?? Possible solutions:");
            troubleshooting.Add("   1. Check internet connection");
            troubleshooting.Add("   2. Verify website is accessible: " + _config.BaseUrl);
            troubleshooting.Add("   3. Increase timeout (currently: " + _config.PageLoadTimeout + "ms)");
        }
        // Website structure changed
        else if (errorMessage.Contains("selector") || 
                 errorMessage.Contains("not found"))
        {
            troubleshooting.Add("? Website structure may have changed");
            troubleshooting.Add("?? Possible solutions:");
            troubleshooting.Add("   1. Verify website is still using same structure");
            troubleshooting.Add("   2. Update selectors in Configuration/ScraperSelectors.cs");
            troubleshooting.Add("   3. Check the website manually: " + _config.BaseUrl);
        }
        else
        {
            troubleshooting.Add("? Unexpected error occurred");
            troubleshooting.Add("?? General troubleshooting:");
            troubleshooting.Add("   1. Check console logs for detailed error");
            troubleshooting.Add("   2. Verify configuration in Program.cs");
            troubleshooting.Add("   3. Try scraping a single page first");
        }

        return new ScraperResponse
        {
            Success = false,
            Message = errorMessage,
            TotalCount = 0,
            StartPage = startPage,
            EndPage = endPage,
            Tenders = new List<TenderDto>(),
            ScrapedAt = DateTime.UtcNow,
            Troubleshooting = troubleshooting
        };
    }
}

/// <summary>
/// Request model for custom scraping
/// </summary>
public class ScraperRequest
{
    /// <summary>
    /// Starting page number (minimum: 1)
    /// </summary>
    public int StartPage { get; set; } = 1;

    /// <summary>
    /// Ending page number
    /// </summary>
    public int EndPage { get; set; } = 5;

    /// <summary>
    /// Whether to save results to file
    /// </summary>
    public bool SaveToFile { get; set; } = true;

    /// <summary>
    /// Output file name (optional)
    /// </summary>
    public string? OutputFileName { get; set; }
}

/// <summary>
/// Response model for scraping operations
/// </summary>
public class ScraperResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Total number of tenders scraped
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Starting page number
    /// </summary>
    public int StartPage { get; set; }

    /// <summary>
    /// Ending page number
    /// </summary>
    public int EndPage { get; set; }

    /// <summary>
    /// List of scraped tenders
    /// </summary>
    public List<TenderDto> Tenders { get; set; } = new();

    /// <summary>
    /// Timestamp when scraping was performed
    /// </summary>
    public DateTime ScrapedAt { get; set; }

    /// <summary>
    /// Troubleshooting steps for errors (only populated on failure)
    /// </summary>
    public List<string>? Troubleshooting { get; set; }
}
