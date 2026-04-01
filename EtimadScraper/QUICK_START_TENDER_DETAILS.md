# Quick Start: Tender Details API

## ?? Quick Test

### 1. Start the Application
```powershell
cd EtimadScraper
dotnet run
```

### 2. Test via Swagger
Open browser: `https://localhost:7000/swagger` (or your configured port)

### 3. Test via PowerShell
```powershell
# Replace with an actual tender ID
$tenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"
$response | ConvertTo-Json -Depth 10
```

## ?? API Endpoint

```
GET /api/TenderScraper/details/{tenderId}?includeRawHtml=false
```

**Example:**
```
GET /api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A==
```

## ?? Response Fields

| Field | Description |
|-------|-------------|
| `tenderId` | Input tender ID |
| `title` | Tender title |
| `referenceNumber` | Official reference number |
| `publishDate` | Publication date |
| `inquiryDeadline` | Last date for inquiries |
| `submissionDeadline` | Last date for submissions |
| `openingDate` | Offer opening date |
| `description` | Full description |
| `organization` | Issuing organization |
| `status` | Current status |
| `category` | Tender category |
| `tenderValue` | Estimated value |
| `isSuccess` | `true` if successful |
| `dataSource` | `"Scraped"`, `"Cached"`, or `"Error"` |
| `errorMessage` | Error details if failed |

## ? Success Response
```json
{
  "tenderId": "...",
  "title": "...",
  "referenceNumber": "...",
  "isSuccess": true,
  "dataSource": "Scraped"
}
```

## ? Error Response
```json
{
  "tenderId": "...",
  "isSuccess": false,
  "dataSource": "Error",
  "errorMessage": "Request blocked..."
}
```

## ?? Common Issues

### WAF Blocking (403 Error)
- Website is blocking automated requests
- **Solution**: Wait and retry, or use cached data

### CAPTCHA Detected
- Anti-bot protection triggered
- **Solution**: Reduce request frequency

### Empty Fields
- HTML structure may have changed
- **Solution**: Use `includeRawHtml=true` to debug

### Timeout
- Network is slow or server not responding
- **Solution**: Check connectivity, retry later

## ??? Debugging

### Include Raw HTML
```
GET /api/TenderScraper/details/{tenderId}?includeRawHtml=true
```

This returns the raw HTML in the `rawHtml` field for inspection.

### Check Logs
Monitor application logs for detailed error information:
```powershell
dotnet run
# Watch console output
```

## ?? Database Cache (TODO)

Currently not implemented. To add:
1. Add Entity Framework Core
2. Create `TenderDetails` entity
3. Update `GetCachedTenderDetailsAsync()` method
4. Update `SaveToCacheAsync()` method

## ?? Full Documentation

See `TENDER_DETAILS_API.md` for:
- Detailed examples
- All error scenarios
- Integration guides
- Best practices

## ?? Key Files

| File | Purpose |
|------|---------|
| `Models/TenderDetailsDto.cs` | Response model |
| `Services/TenderDetailsScraperService.cs` | Scraping logic |
| `Controllers/TenderScraperController.cs` | API endpoint |

## ?? Next Steps

1. ? Test the endpoint
2. ? Implement database caching
3. ? Adjust XPath selectors if needed
4. ? Add rate limiting
5. ? Monitor and optimize

## ?? Support

- Check `TENDER_DETAILS_API.md` for detailed docs
- Review `IMPLEMENTATION_SUMMARY.md` for technical details
- Use Swagger UI for interactive testing
