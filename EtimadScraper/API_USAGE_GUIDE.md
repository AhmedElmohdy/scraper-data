# ?? Tender Details API - Quick Start

## Endpoint

```
GET /api/TenderScraper/details/{tenderId}
```

**Parameters:**
- `tenderId` (path, required) - Tender ID (e.g., `pFtWw3BqD9rAnKqZhiqj3A==`)
- `includeRawHtml` (query, optional) - Return raw HTML for debugging (default: `false`)

---

## Usage Examples

### PowerShell
```powershell
# Basic usage
$tenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"

# With raw HTML
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId`?includeRawHtml=true"

# Save to file
$response | ConvertTo-Json -Depth 10 | Out-File "tender.json"
```

### cURL
```bash
# Basic usage
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A%3D%3D"

# With raw HTML
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A%3D%3D?includeRawHtml=true"

# Pretty print with jq
curl -X GET "https://localhost:7000/api/TenderScraper/details/pFtWw3BqD9rAnKqZhiqj3A%3D%3D" | jq .
```

### JavaScript (Fetch)
```javascript
const tenderId = "pFtWw3BqD9rAnKqZhiqj3A==";
const url = `https://localhost:7000/api/TenderScraper/details/${encodeURIComponent(tenderId)}`;

const response = await fetch(url);
const data = await response.json();

console.log(`Title: ${data.title}`);
console.log(`Organization: ${data.organization}`);
console.log(`Total fields: ${Object.keys(data.allFields).length}`);
```

### C#
```csharp
using HttpClient httpClient = new();
var tenderId = "pFtWw3BqD9rAnKqZhiqj3A==";
var url = $"https://localhost:7000/api/TenderScraper/details/{Uri.EscapeDataString(tenderId)}";

var response = await httpClient.GetFromJsonAsync<TenderDetailsDto>(url);

Console.WriteLine($"Title: {response.Title}");
Console.WriteLine($"Organization: {response.Organization}");
Console.WriteLine($"Total fields: {response.AllFields.Count}");
```

---

## Response Structure

```json
{
  "tenderId": "pFtWw3BqD9rAnKqZhiqj3A==",
  
  // Basic Information (Main Page)
  "title": "???? ????? ????? ?????? ?????????? (IAM)",
  "tenderNumberIAM": "42441",
  "referenceNumber": "260339009968",
  "purpose": "????? ???? ????? ??????...",
  "documentsValue": "1000.00 ?",
  "status": "??????",
  "tenderCondition": "??????",
  "contractDuration": "1 ???",
  "maintenanceInsurance": "???",
  "competitionType": "?????? ????",
  "organization": "????? ????? ????????? ?????? ????????",
  "submissionMethod": "??? ???? ????? ????? ??????? ???",
  "initialGuaranteeRequirements": "???? ???????",
  "initialGuaranteeTitle": "????? ?????? ?????????",
  "initialGuaranteeValue": "...",
  "finalGuarantee": "5.00",
  "publishDate": "16/10/1447",
  
  // Dates & Deadlines (Dates Tab)
  "inquiryDeadline": "29/10/1447 AM 09:59",
  "submissionDeadline": "29/10/1447 AM 10.00",
  "offerOpeningDate": "04/04/2026",
  "technicalOfferOpeningDate": "17/04/2026",
  "stopPeriod": "5",
  "expectedAwardDate": "02/02/1448 16/07/2026",
  "actionStartDate": "03/03/1448 16/08/2026",
  "questionSubmissionStartDate": "13/10/1447 01/04/2026",
  "maxQuestionResponseTime": "3",
  "openingDate": "...",
  
  // Classification & Details (Relations Tab)
  "category": "???? ????????...",
  "description": "????????...",
  "estimatedDuration": "37 ????? 21 ???? 15 ???",
  "tenderValue": "...",
  
  // All Extracted Fields (from all 5 endpoints)
  "allFields": {
    // Main page
    "??? ????????": "???? ????? ????? ??????...",
    "??? ????????": "42441",
    "????? ???????": "260339009968",
    
    // Dates tab
    "??? ???? ??????? ???????????": "29/10/1447 AM 09:59",
    "??? ???? ?????? ??????": "29/10/1447 AM 10.00",
    
    // Relations tab
    "ExecutionLocation": "??????",
    "SupplyItemsIncluded": "???",
    "ConstructionWork": "???",
    "MaintenanceWork": "??",
    
    // Awarding tab
    "Awarding_WinnerName": "...",
    "Awarding_Amount": "...",
    
    // Local content tab
    "LocalContent_Mechanisms": "...",
    "LocalContentRequirements": "..."
    
    // ... 50-70 total fields
  },
  
  // Metadata
  "scrapedAt": "2026-04-01T10:30:00Z",
  "dataSource": "Scraped",
  "rawHtml": null,  // or combined HTML if includeRawHtml=true
  "isSuccess": true,
  "errorMessage": null
}
```

---

## Field Categories

### ?? Basic Information
- `title` - ??? ????????
- `tenderNumberIAM` - ??? ????????
- `referenceNumber` - ????? ???????
- `organization` - ????? ????????
- `competitionType` - ??? ????????
- `status` - ???? ????????
- `purpose` - ????? ?? ????????
- `documentsValue` - ???? ????? ????????

### ?? Dates & Deadlines
- `publishDate` - ????? ?????
- `inquiryDeadline` - ??? ???? ???????????
- `submissionDeadline` - ??? ???? ?????? ??????
- `offerOpeningDate` - ????? ??? ??????
- `technicalOfferOpeningDate` - ????? ??? ??????
- `expectedAwardDate` - ??????? ??????? ???????
- `actionStartDate` - ????? ??? ???????
- `questionSubmissionStartDate` - ????? ??????? ???????????
- `maxQuestionResponseTime` - ???? ??? ????

### ?? Financial & Guarantees
- `documentsValue` - ???? ????? ????????
- `tenderValue` - ?????? ?????????
- `initialGuaranteeRequirements` - ??????? ???? ???????
- `initialGuaranteeTitle` - ????? ?????? ?????????
- `initialGuaranteeValue` - ???? ?????? ?????????
- `finalGuarantee` - ?????? ???????

### ??? Contract & Requirements
- `contractDuration` - ??? ?????
- `submissionMethod` - ????? ????? ??????
- `maintenanceInsurance` - ??????? ???????
- `stopPeriod` - ???? ??????

### ?? Classification & Location
- `category` - ???? ????????
- `description` - ????????
- `allFields["ExecutionLocation"]` - ???? ???????
- `allFields["SupplyItemsIncluded"]` - ???? ?????
- `allFields["ConstructionWork"]` - ????? ???????

### ?? Awarding Results
- `allFields["Awarding_*"]` - All awarding fields

### ???? Local Content
- `allFields["LocalContent_*"]` - All local content fields

---

## Common Use Cases

### 1. Get Basic Info
```javascript
const response = await fetch(`/api/TenderScraper/details/${tenderId}`);
const data = await response.json();

console.log(`
  Tender: ${data.title}
  Number: ${data.tenderNumberIAM}
  Organization: ${data.organization}
  Status: ${data.status}
`);
```

### 2. Check Deadlines
```javascript
const deadlines = {
  inquiry: data.inquiryDeadline,
  submission: data.submissionDeadline,
  opening: data.offerOpeningDate
};

console.log('Important Dates:', deadlines);
```

### 3. Get All Fields
```javascript
// Access any field by Arabic label
const allData = data.allFields;

console.log('Execution Location:', allData['ExecutionLocation']);
console.log('Supply Items:', allData['SupplyItemsIncluded']);
console.log('Construction Work:', allData['ConstructionWork']);
```

### 4. Export to CSV
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"

# Create CSV row
$csv = [PSCustomObject]@{
    TenderId = $response.tenderId
    Title = $response.title
    Organization = $response.organization
    SubmissionDeadline = $response.submissionDeadline
    Status = $response.status
}

$csv | Export-Csv -Path "tender.csv" -NoTypeInformation
```

### 5. Batch Processing
```powershell
$tenderIds = @(
    "pFtWw3BqD9rAnKqZhiqj3A==",
    "anotherTenderId123==",
    "yetAnotherTender456=="
)

$results = @()
foreach ($id in $tenderIds) {
    Write-Host "Fetching tender $id..."
    $response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$id"
    $results += $response
    Start-Sleep -Seconds 2  # Rate limiting
}

$results | ConvertTo-Json -Depth 10 | Out-File "all_tenders.json"
```

---

## Error Handling

### Success Response (200 OK)
```json
{
  "tenderId": "...",
  "title": "...",
  "isSuccess": true,
  "errorMessage": null
}
```

### Partial Success (200 OK with warnings)
```json
{
  "tenderId": "...",
  "title": "...",
  "isSuccess": true,
  "errorMessage": "Partial success: Failed to fetch Awarding endpoint; Failed to fetch LocalContent endpoint"
}
```

### Not Found (400 Bad Request)
```json
{
  "error": "Tender ID is required"
}
```

### Scraping Failed (200 OK with error)
```json
{
  "tenderId": "...",
  "isSuccess": false,
  "errorMessage": "Failed to fetch main tender page. HTTP Status: 404",
  "dataSource": "Error"
}
```

### Server Error (500 Internal Server Error)
```json
{
  "tenderId": "...",
  "isSuccess": false,
  "errorMessage": "Unexpected error: Connection timeout",
  "dataSource": "Error"
}
```

---

## Best Practices

### 1. Rate Limiting
```csharp
foreach (var tenderId in tenderIds)
{
    var data = await GetTenderDetails(tenderId);
    await Task.Delay(TimeSpan.FromSeconds(2)); // 2 second delay
}
```

### 2. Error Handling
```csharp
try
{
    var response = await httpClient.GetFromJsonAsync<TenderDetailsDto>(url);
    
    if (!response.IsSuccess)
    {
        Console.WriteLine($"Warning: {response.ErrorMessage}");
    }
    
    return response;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
    return null;
}
```

### 3. Null Checking
```csharp
var title = response.Title ?? "N/A";
var org = response.Organization ?? "Unknown";
var deadline = response.SubmissionDeadline ?? "Not specified";
```

### 4. Date Parsing
```csharp
// Etimad uses Hijri dates
var publishDate = response.PublishDate; // "16/10/1447"

// May need conversion to Gregorian
// Use System.Globalization.HijriCalendar if needed
```

### 5. Caching
```csharp
// Cache responses to reduce API calls
var cachedData = await GetFromCache(tenderId);
if (cachedData != null && cachedData.ScrapedAt > DateTime.UtcNow.AddHours(-24))
{
    return cachedData;
}

var freshData = await FetchFromAPI(tenderId);
await SaveToCache(freshData);
return freshData;
```

---

## Debugging

### Enable Raw HTML
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId`?includeRawHtml=true"

# Save each endpoint HTML
$response.rawHtml -split "<!-- ===== " | ForEach-Object {
    if ($_ -match "^(\w+) Endpoint =====") {
        $name = $matches[1]
        $_ | Out-File "${name}_endpoint.html"
    }
}
```

### Check Extraction Quality
```powershell
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"

Write-Host "Total fields extracted: $($response.allFields.Count)"
Write-Host "Empty DTO fields:"

$response.PSObject.Properties | Where-Object {
    $_.Name -ne 'allFields' -and 
    $_.Name -ne 'rawHtml' -and 
    [string]::IsNullOrEmpty($_.Value)
} | ForEach-Object {
    Write-Host "  - $($_.Name)"
}
```

---

## Performance

- **Single tender:** ~2-3 seconds
- **Parallel fetching:** All 5 endpoints fetched simultaneously
- **Total fields:** ~50-70 per tender
- **Response size:** ~20-50 KB (without raw HTML)
- **With raw HTML:** ~200-500 KB

---

## Production Tips

1. **Use HTTPS** in production
2. **Implement caching** to reduce load
3. **Add rate limiting** to avoid being blocked
4. **Monitor logs** for failed endpoints
5. **Handle Arabic encoding** properly (UTF-8)
6. **Validate tender IDs** before calling API
7. **Set appropriate timeouts** (default: 30 seconds)

---

## Quick Reference

| Task | Command |
|------|---------|
| Get tender details | `GET /api/TenderScraper/details/{tenderId}` |
| Include raw HTML | `?includeRawHtml=true` |
| Check if successful | `response.isSuccess === true` |
| Get all fields | `response.allFields` |
| Get specific field | `response.title` |
| Check extraction count | `Object.keys(response.allFields).length` |

---

**Ready to use! The API now returns complete tender details from all 5 Etimad endpoints.** ??
