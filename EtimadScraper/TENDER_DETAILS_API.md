# Tender Details Scraping API

## Overview
This API endpoint allows you to scrape detailed tender information from the Etimad platform by providing a tender ID. The service uses `HttpClient` and `HtmlAgilityPack` for web scraping, with fallback to cached data from the database when available.

## Features
- ? Scrape tender details by tender ID
- ? Use HttpClient for HTTP requests
- ? Parse HTML with HtmlAgilityPack
- ? Handle WAF/CAPTCHA detection
- ? Fallback to cached data when scraping fails
- ? Comprehensive error handling
- ? Request timeout handling
- ? Anti-bot protection detection

## API Endpoint

### Get Tender Details
```
GET /api/TenderScraper/details/{tenderId}
```

**Parameters:**
- `tenderId` (required): The tender ID (e.g., `pFtWw3BqD9rAnKqZhiqj3A==`)
- `includeRawHtml` (optional, query parameter): Whether to include raw HTML in response for debugging (default: `false`)

**Example Request:**
```bash
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A=="
```

**Example Request with Raw HTML:**
```bash
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A==?includeRawHtml=true"
```

## Response Structure

### Success Response (200 OK)
```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  "title": "????? ????? ????",
  "referenceNumber": "12345678",
  "publishDate": "2024-01-15",
  "inquiryDeadline": "2024-01-20",
  "submissionDeadline": "2024-02-01",
  "openingDate": "2024-02-02",
  "description": "????? ????? ???? ?????? ??????????",
  "organization": "????? ?????",
  "status": "?????",
  "category": "??????? ????",
  "tenderValue": "500,000 ????",
  "additionalInfo": "??????? ??????...",
  "scrapedAt": "2024-01-15T10:30:00Z",
  "dataSource": "Scraped",
  "isSuccess": true,
  "errorMessage": null
}
```

### Error Response (Data Source: Scraped)
```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  "title": "",
  "referenceNumber": "",
  "publishDate": "",
  "inquiryDeadline": "",
  "submissionDeadline": "",
  "openingDate": "",
  "description": "",
  "organization": "",
  "status": "",
  "category": "",
  "tenderValue": "",
  "additionalInfo": "",
  "scrapedAt": "2024-01-15T10:30:00Z",
  "dataSource": "Error",
  "isSuccess": false,
  "errorMessage": "Request blocked (Status: 403). This may be due to WAF protection or rate limiting."
}
```

### Cached Response (Fallback)
```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  "title": "????? ????? ????",
  "referenceNumber": "12345678",
  "publishDate": "2024-01-15",
  "inquiryDeadline": "2024-01-20",
  "submissionDeadline": "2024-02-01",
  "openingDate": "2024-02-02",
  "description": "????? ????? ???? ?????? ??????????",
  "organization": "????? ?????",
  "status": "?????",
  "category": "??????? ????",
  "tenderValue": "500,000 ????",
  "additionalInfo": "??????? ??????...",
  "scrapedAt": "2024-01-10T08:00:00Z",
  "dataSource": "Cached",
  "isSuccess": true,
  "errorMessage": null
}
```

## Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `tenderId` | string | The tender ID used in the request |
| `title` | string | Tender title or name |
| `referenceNumber` | string | Official tender reference number |
| `publishDate` | string | Date when tender was published |
| `inquiryDeadline` | string | Last date for inquiries |
| `submissionDeadline` | string | Last date for submission |
| `openingDate` | string | Date when offers will be opened |
| `description` | string | Detailed tender description |
| `organization` | string | Issuing organization |
| `status` | string | Current tender status |
| `category` | string | Tender category or type |
| `tenderValue` | string | Estimated tender value |
| `additionalInfo` | string | Additional information |
| `scrapedAt` | DateTime | When data was scraped |
| `dataSource` | string | Source: "Scraped", "Cached", or "Error" |
| `rawHtml` | string? | Raw HTML (only if `includeRawHtml=true`) |
| `isSuccess` | boolean | Whether scraping was successful |
| `errorMessage` | string? | Error message if failed |

## Error Handling

The service handles various error scenarios:

### 1. WAF/Firewall Blocking
```json
{
  "isSuccess": false,
  "errorMessage": "Request blocked (Status: 403). This may be due to WAF protection or rate limiting.",
  "dataSource": "Error"
}
```

### 2. Anti-Bot Protection/CAPTCHA
```json
{
  "isSuccess": false,
  "errorMessage": "Anti-bot protection or CAPTCHA detected. Unable to scrape data.",
  "dataSource": "Error"
}
```

### 3. Network Errors
```json
{
  "isSuccess": false,
  "errorMessage": "Network error: No connection could be made...",
  "dataSource": "Error"
}
```

### 4. Timeout
```json
{
  "isSuccess": false,
  "errorMessage": "Request timeout. The server took too long to respond.",
  "dataSource": "Error"
}
```

### 5. Parsing Errors
```json
{
  "isSuccess": false,
  "errorMessage": "Failed to extract tender data. The page structure may have changed.",
  "dataSource": "Error"
}
```

## Usage Examples

### PowerShell
```powershell
# Basic request
$tenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId" -Method GET
$response | ConvertTo-Json -Depth 10

# With raw HTML
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId`?includeRawHtml=true" -Method GET
$response.rawHtml | Out-File "tender.html"
```

### cURL
```bash
# Basic request
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A==" \
  -H "Accept: application/json"

# With raw HTML
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A==?includeRawHtml=true" \
  -H "Accept: application/json" | jq -r '.rawHtml' > tender.html
```

### JavaScript/TypeScript
```javascript
// Using fetch
async function getTenderDetails(tenderId) {
  const response = await fetch(
    `https://localhost:7000/api/TenderScraper/details/${encodeURIComponent(tenderId)}`
  );
  
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  
  const data = await response.json();
  
  if (!data.isSuccess) {
    console.error('Scraping failed:', data.errorMessage);
    // Check if cached data is available
    if (data.dataSource === 'Cached') {
      console.log('Using cached data');
    }
  }
  
  return data;
}

// Usage
getTenderDetails('pFtWw3BqD9rAnKqZhiqj3A==')
  .then(tender => console.log(tender))
  .catch(error => console.error(error));
```

### C#
```csharp
using System.Net.Http;
using System.Text.Json;

public async Task<TenderDetailsDto> GetTenderDetails(string tenderId)
{
    using var client = new HttpClient();
    var encodedId = Uri.EscapeDataString(tenderId);
    var url = $"https://localhost:7000/api/TenderScraper/details/{encodedId}";
    
    var response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    var tender = JsonSerializer.Deserialize<TenderDetailsDto>(json, 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    
    if (!tender.IsSuccess)
    {
        Console.WriteLine($"Scraping failed: {tender.ErrorMessage}");
        if (tender.DataSource == "Cached")
        {
            Console.WriteLine("Using cached data");
        }
    }
    
    return tender;
}
```

## Implementation Details

### Service Architecture
```
TenderDetailsScraperService
??? HttpClient (with retry logic)
??? HtmlAgilityPack (HTML parsing)
??? Cache Layer (database fallback)
??? Error Handling
```

### Scraping Strategy
1. **Primary**: Scrape from live website using HttpClient
2. **Fallback**: Retrieve from database cache if scraping fails
3. **Error**: Return error details if both methods fail

### HTML Parsing
The service uses multiple XPath selectors to extract data, trying different patterns to handle variations in the HTML structure:

```csharp
// Example: Title extraction with multiple selectors
var title = TryExtractText(doc, new[]
{
    "//h1[contains(@class, 'tender-title')]",
    "//h1",
    "//div[contains(@class, 'tender-name')]",
    "//span[contains(text(), '??? ????????')]/following-sibling::*"
});
```

### User Agent & Headers
The service uses realistic browser headers to avoid detection:
- User-Agent: Chrome 120 on Windows
- Accept-Language: ar-SA (Arabic - Saudi Arabia)
- Accept-Encoding: gzip, deflate, br

## Database Integration (TODO)

Currently, the cache layer is a placeholder. To implement:

1. Create a database context and entity:
```csharp
public class TenderDetailsEntity
{
    public int Id { get; set; }
    public string TenderId { get; set; }
    public string Title { get; set; }
    // ... other fields
    public DateTime ScrapedAt { get; set; }
}
```

2. Update `GetCachedTenderDetailsAsync`:
```csharp
private async Task<TenderDetailsDto?> GetCachedTenderDetailsAsync(string tenderId)
{
    using var dbContext = _dbContextFactory.CreateDbContext();
    var cached = await dbContext.TenderDetails
        .Where(t => t.TenderId == tenderId)
        .OrderByDescending(t => t.ScrapedAt)
        .FirstOrDefaultAsync();
    
    return cached != null ? MapToDto(cached) : null;
}
```

3. Implement `SaveToCacheAsync` to persist successful scrapes

## Testing

### Swagger UI
1. Navigate to `/swagger`
2. Find `GET /api/TenderScraper/details/{tenderId}`
3. Click "Try it out"
4. Enter a tender ID
5. Execute

### Postman Collection
Import the provided Postman collection (`EtimadScraperAPI.postman_collection.json`) which includes:
- Get Tender Details request
- Pre-configured environment variables
- Example responses

## Troubleshooting

### Issue: Empty Response Fields
**Cause**: HTML structure changed or selectors don't match
**Solution**: 
- Use `includeRawHtml=true` to inspect the HTML
- Update XPath selectors in `TenderDetailsScraperService`

### Issue: Request Blocked (403)
**Cause**: WAF or rate limiting
**Solution**:
- Wait before retrying
- Add delays between requests
- Consider using a proxy

### Issue: Timeout Errors
**Cause**: Slow network or server
**Solution**:
- Increase timeout in service configuration
- Check network connectivity

### Issue: CAPTCHA/Anti-Bot
**Cause**: Website detected automated scraping
**Solution**:
- Reduce request frequency
- Use more realistic headers
- Consider using Playwright instead of HttpClient

## Best Practices

1. **Rate Limiting**: Don't make too many requests in a short time
2. **Caching**: Utilize cached data when available
3. **Error Handling**: Always check `isSuccess` field
4. **Logging**: Monitor logs for scraping issues
5. **Testing**: Test with `includeRawHtml=true` when debugging

## Dependencies

- **HtmlAgilityPack**: HTML parsing library
- **Microsoft.Extensions.Http**: HttpClient factory
- **Microsoft.Extensions.Logging**: Logging infrastructure

## License

Same as the main EtimadScraper project.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review service logs
3. Test with Swagger UI
4. Inspect raw HTML response
