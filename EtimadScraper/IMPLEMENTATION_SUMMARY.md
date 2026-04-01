# Tender Details Service Implementation Summary

## What Was Created

### 1. New DTO: `TenderDetailsDto.cs`
**Location**: `EtimadScraper/Models/TenderDetailsDto.cs`

A comprehensive data transfer object that contains:
- Basic tender information (ID, title, reference number)
- Important dates (publish date, inquiry deadline, submission deadline, opening date)
- Detailed information (description, organization, status, category, value)
- Metadata (scraped timestamp, data source, success status, error messages)
- Optional raw HTML for debugging

### 2. New Service: `TenderDetailsScraperService.cs`
**Location**: `EtimadScraper/Services/TenderDetailsScraperService.cs`

Key features:
- ? Uses HttpClient with proper headers (User-Agent, Accept-Language, etc.)
- ? HTML parsing with HtmlAgilityPack
- ? Multiple XPath selector strategies for robust data extraction
- ? Detects WAF/firewall blocking (403, 401, 429 status codes)
- ? Detects CAPTCHA and anti-bot protection
- ? Handles network errors and timeouts
- ? Fallback to cached database data (placeholder for future implementation)
- ? Comprehensive error handling and logging
- ? Async/await pattern throughout

### 3. New Controller Endpoint
**Location**: `EtimadScraper/Controllers/TenderScraperController.cs`

New endpoint added:
```
GET /api/TenderScraper/details/{tenderId}
```

Query parameters:
- `includeRawHtml`: Optional boolean to include raw HTML in response

### 4. Updated Dependencies
**Location**: `EtimadScraper/EtimadScraper.csproj`

Added package:
- `HtmlAgilityPack` version 1.11.71

### 5. Updated Service Registration
**Location**: `EtimadScraper/Program.cs`

Added:
- HttpClient factory registration with proper configuration
- TenderDetailsScraperService registration as scoped service

### 6. Documentation
**Location**: `EtimadScraper/TENDER_DETAILS_API.md`

Comprehensive documentation including:
- API endpoint details
- Request/response examples
- Error handling scenarios
- Usage examples in multiple languages (PowerShell, cURL, JavaScript, C#)
- Troubleshooting guide
- Best practices

## How It Works

### Request Flow
```
1. Client ? GET /api/TenderScraper/details/{tenderId}
2. Controller ? TenderDetailsScraperService.ScrapeTenderDetailsAsync()
3. Service ? Try to scrape from Etimad website
4. Service ? If fails, try to get cached data from database
5. Service ? Return result (either scraped, cached, or error)
6. Controller ? Return JSON response to client
```

### Scraping Process
```
1. Build URL: https://tenders.etimad.sa/Tender/DetailsForVisitor?STenderId={tenderId}
2. Send HTTP GET request with browser-like headers
3. Check for blocking/errors (WAF, CAPTCHA, timeouts)
4. Parse HTML using HtmlAgilityPack
5. Extract data using multiple XPath selectors
6. Validate extracted data
7. Return TenderDetailsDto
```

### Error Handling
The service handles multiple error scenarios:
- **WAF Blocking**: Returns error with data source "Error"
- **CAPTCHA Detection**: Returns error message
- **Network Issues**: Returns network error details
- **Timeout**: Returns timeout message
- **Parsing Errors**: Returns parsing error with suggestions
- **Fallback**: If scraping fails, attempts to retrieve cached data

## Testing

### Using Swagger
1. Run the application
2. Navigate to `/swagger`
3. Find the new endpoint: `GET /api/TenderScraper/details/{tenderId}`
4. Try it out with a sample tender ID: `pFtWw3BqD9rAnKqZhiqj3A==`

### Using PowerShell
```powershell
$tenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"
$response | ConvertTo-Json -Depth 10
```

### Using cURL
```bash
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A=="
```

## Database Integration (TODO)

The service includes placeholder methods for database caching:
- `GetCachedTenderDetailsAsync()`: Retrieve from cache
- `SaveToCacheAsync()`: Save to cache

To implement:
1. Add Entity Framework Core or your preferred ORM
2. Create a `TenderDetails` entity/table
3. Update the placeholder methods with actual database code
4. Call `SaveToCacheAsync()` after successful scrapes

Example Entity Framework implementation:
```csharp
// In GetCachedTenderDetailsAsync
using var dbContext = _dbContextFactory.CreateDbContext();
var cached = await dbContext.TenderDetails
    .Where(t => t.TenderId == tenderId)
    .OrderByDescending(t => t.ScrapedAt)
    .FirstOrDefaultAsync();

if (cached != null)
{
    cached.DataSource = "Cached";
    return cached;
}
return null;

// In SaveToCacheAsync
using var dbContext = _dbContextFactory.CreateDbContext();
dbContext.TenderDetails.Add(tenderDetails);
await dbContext.SaveChangesAsync();
```

## Response Examples

### Success
```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  "title": "????? ????? ????",
  "referenceNumber": "12345678",
  "publishDate": "2024-01-15",
  "submissionDeadline": "2024-02-01",
  "description": "????? ????? ???? ??????",
  "organization": "????? ?????",
  "isSuccess": true,
  "dataSource": "Scraped"
}
```

### Error (with fallback to cache)
```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  "title": "????? ????? ????",
  "isSuccess": true,
  "dataSource": "Cached",
  "errorMessage": null
}
```

### Error (no cache available)
```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  "isSuccess": false,
  "dataSource": "Error",
  "errorMessage": "Request blocked (Status: 403). This may be due to WAF protection."
}
```

## Key Features

1. **Resilient Scraping**: Multiple XPath selectors ensure data extraction even if HTML structure varies
2. **WAF Detection**: Identifies when requests are blocked by firewalls or rate limiters
3. **Fallback Strategy**: Uses cached data when live scraping fails
4. **Comprehensive Logging**: All operations are logged for debugging
5. **Error Transparency**: Errors are returned in a structured format with actionable messages
6. **Debugging Support**: Optional raw HTML output for troubleshooting
7. **Async/Await**: Non-blocking operations for better performance

## Files Modified

1. ? `EtimadScraper.csproj` - Added HtmlAgilityPack package
2. ? `Program.cs` - Registered HttpClient and TenderDetailsScraperService
3. ? `Controllers/TenderScraperController.cs` - Added new endpoint

## Files Created

1. ? `Models/TenderDetailsDto.cs` - DTO for tender details
2. ? `Services/TenderDetailsScraperService.cs` - Main scraping service
3. ? `TENDER_DETAILS_API.md` - Comprehensive API documentation
4. ? `IMPLEMENTATION_SUMMARY.md` - This file

## Next Steps

1. **Test the API**: Use Swagger or Postman to test the new endpoint
2. **Implement Database Caching**: Add Entity Framework and create the cache layer
3. **Fine-tune Selectors**: Adjust XPath selectors based on actual Etimad HTML structure
4. **Add Rate Limiting**: Implement rate limiting to avoid being blocked
5. **Monitor Performance**: Watch logs to identify any issues
6. **Extend Functionality**: Add more fields or features as needed

## Important Notes

?? **WAF/Anti-Bot Protection**: The Etimad website may have protection mechanisms. If you get blocked:
- Reduce request frequency
- Use the cached data fallback
- Consider using Playwright instead of HttpClient for JavaScript-rendered content

?? **HTML Structure Changes**: If the website changes its HTML structure:
- Update XPath selectors in `TenderDetailsScraperService.cs`
- Use `includeRawHtml=true` to inspect the actual HTML
- Test with multiple tender IDs to ensure consistency

?? **Database Not Implemented**: The cache layer is currently a placeholder. Implement it using:
- Entity Framework Core
- Dapper
- Any other ORM of your choice

## Support

For questions or issues:
1. Check the logs (ILogger output)
2. Test with `includeRawHtml=true` to see the actual HTML
3. Review `TENDER_DETAILS_API.md` for usage examples
4. Check the Swagger UI for interactive testing
