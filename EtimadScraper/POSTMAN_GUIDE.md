# Postman API Testing Guide for Etimad Scraper

## Base URL
```
https://localhost:7xxx
http://localhost:5xxx
```
(Port numbers will be shown when you run the app)

---

## Available Endpoints

### 1. **Health Check / Status**

**GET** `/api/TenderScraper/status`

Get the current status and configuration of the scraper.

**Response:**
```json
{
  "status": "healthy",
  "service": "Etimad Tender Scraper API",
  "version": "1.0.0",
  "timestamp": "2024-01-10T10:30:00Z",
  "configuration": {
    "baseUrl": "https://tenders.etimad.sa/Tender/AllTendersForVisitor",
    "headless": true,
    "maxPages": 5,
    "startPage": 1
  }
}
```

---

### 2. **Get Configuration**

**GET** `/api/TenderScraper/config`

Get the current scraper configuration.

**Response:**
```json
{
  "baseUrl": "https://tenders.etimad.sa/Tender/AllTendersForVisitor",
  "headless": true,
  "maxPages": 5,
  "startPage": 1,
  "pageLoadTimeout": 30000,
  "delayBetweenPages": 2000,
  "maxRetryAttempts": 3,
  "outputFilePath": "tenders.json"
}
```

---

### 3. **Scrape Tenders (Default Configuration)**

**POST** `/api/TenderScraper/scrape`

Scrape tenders using default configuration (pages 1-5).

**Request:**
- No body required

**Response:**
```json
{
  "success": true,
  "message": "Successfully scraped 50 tenders",
  "totalCount": 50,
  "startPage": 1,
  "endPage": 5,
  "tenders": [
    {
      "tenderNumber": "12345678",
      "title": "????? ????? ?????? ???????",
      "organization": "????? ?????? ???????",
      "publishDate": "14/03/1446",
      "closingDate": "21/03/1446",
      "detailsUrl": "https://tenders.etimad.sa/Tender/Details/12345678",
      "status": "Active",
      "additionalInfo": "",
      "scrapedAt": "2024-01-10T10:30:00Z"
    }
  ],
  "scrapedAt": "2024-01-10T10:30:00Z"
}
```

---

### 4. **Scrape Single Page**

**GET** `/api/TenderScraper/scrape/page/{pageNumber}`

Scrape a specific page number.

**Example:**
```
GET /api/TenderScraper/scrape/page/3
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully scraped 10 tenders from page 3",
  "totalCount": 10,
  "startPage": 3,
  "endPage": 3,
  "tenders": [...],
  "scrapedAt": "2024-01-10T10:35:00Z"
}
```

---

### 5. **Scrape Custom Page Range**

**POST** `/api/TenderScraper/scrape/custom`

Scrape a custom range of pages with optional file saving.

**Request Body:**
```json
{
  "startPage": 1,
  "endPage": 10,
  "saveToFile": true,
  "outputFileName": "my_tenders.json"
}
```

**Request Body (Minimal):**
```json
{
  "startPage": 5,
  "endPage": 10
}
```

**Response:**
```json
{
  "success": true,
  "message": "Successfully scraped 60 tenders from pages 5 to 10",
  "totalCount": 60,
  "startPage": 5,
  "endPage": 10,
  "tenders": [...],
  "scrapedAt": "2024-01-10T10:40:00Z"
}
```

**Error Response (Invalid Range):**
```json
{
  "error": "Invalid page range. StartPage must be >= 1 and EndPage >= StartPage"
}
```

**Error Response (Too Many Pages):**
```json
{
  "error": "Maximum 50 pages allowed per request"
}
```

---

## Postman Collection Setup

### Step 1: Create a New Collection

1. Open Postman
2. Click "New" ? "Collection"
3. Name it: "Etimad Tender Scraper API"

### Step 2: Add Environment Variables

1. Click "Environments" ? "Create Environment"
2. Name: "Etimad Scraper Local"
3. Add variable:
   - `base_url` = `https://localhost:7xxx` (replace with actual port)

### Step 3: Add Requests

#### Request 1: Health Check
```
Method: GET
URL: {{base_url}}/api/TenderScraper/status
```

#### Request 2: Get Config
```
Method: GET
URL: {{base_url}}/api/TenderScraper/config
```

#### Request 3: Scrape Default
```
Method: POST
URL: {{base_url}}/api/TenderScraper/scrape
```

#### Request 4: Scrape Page 1
```
Method: GET
URL: {{base_url}}/api/TenderScraper/scrape/page/1
```

#### Request 5: Scrape Custom Range
```
Method: POST
URL: {{base_url}}/api/TenderScraper/scrape/custom
Headers:
  Content-Type: application/json
Body (raw JSON):
{
  "startPage": 1,
  "endPage": 3,
  "saveToFile": true,
  "outputFileName": "tenders_custom.json"
}
```

---

## Testing Scenarios

### Scenario 1: Quick Test (Single Page)
```
GET /api/TenderScraper/scrape/page/1
```
Expected: Fast response with ~10 tenders

### Scenario 2: Default Scraping
```
POST /api/TenderScraper/scrape
```
Expected: 5 pages scraped, ~50 tenders

### Scenario 3: Custom Range
```
POST /api/TenderScraper/scrape/custom
Body:
{
  "startPage": 1,
  "endPage": 2,
  "saveToFile": false
}
```
Expected: 2 pages scraped, no file saved

### Scenario 4: Error Handling (Invalid Range)
```
POST /api/TenderScraper/scrape/custom
Body:
{
  "startPage": 10,
  "endPage": 5
}
```
Expected: 400 Bad Request with error message

---

## Response Codes

| Code | Meaning |
|------|---------|
| 200 | Success - Scraping completed |
| 400 | Bad Request - Invalid parameters |
| 500 | Server Error - Scraping failed |

---

## Tips for Postman Testing

### 1. Save Response to Variable
```javascript
// In Tests tab
var jsonData = pm.response.json();
pm.environment.set("totalTenders", jsonData.totalCount);
```

### 2. Assert Success
```javascript
// In Tests tab
pm.test("Scraping was successful", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.eql(true);
    pm.expect(jsonData.totalCount).to.be.above(0);
});
```

### 3. Check Response Time
```javascript
// In Tests tab
pm.test("Response time is acceptable", function () {
    pm.expect(pm.response.responseTime).to.be.below(60000); // 60 seconds
});
```

---

## Running the API

### 1. Start the API
```bash
cd EtimadScraper
dotnet run
```

### 2. Note the URL
Look for output like:
```
Now listening on: https://localhost:7001
Now listening on: http://localhost:5001
```

### 3. Access Swagger UI
Open browser:
```
https://localhost:7001/swagger
```

### 4. Test in Postman
Use the URLs from step 2 as your `base_url`

---

## Swagger UI

The API includes Swagger for interactive testing:

1. Run the app: `dotnet run`
2. Open: `https://localhost:xxxx/swagger`
3. Try out endpoints directly in browser

---

## Example cURL Commands

### Get Status
```bash
curl -X GET "https://localhost:7001/api/TenderScraper/status"
```

### Scrape Default
```bash
curl -X POST "https://localhost:7001/api/TenderScraper/scrape"
```

### Scrape Custom Range
```bash
curl -X POST "https://localhost:7001/api/TenderScraper/scrape/custom" \
  -H "Content-Type: application/json" \
  -d '{
    "startPage": 1,
    "endPage": 3,
    "saveToFile": true
  }'
```

### Scrape Single Page
```bash
curl -X GET "https://localhost:7001/api/TenderScraper/scrape/page/1"
```

---

## Performance Notes

- **Single page**: ~5-10 seconds
- **5 pages (default)**: ~30-60 seconds
- **10 pages**: ~1-2 minutes
- **50 pages (max)**: ~5-10 minutes

Times depend on network speed and website response time.

---

## Troubleshooting

### Issue: Connection Refused
- Make sure the API is running (`dotnet run`)
- Check the correct port number
- Try HTTP instead of HTTPS

### Issue: 500 Error
- Check API logs in console
- Common causes:
  - Playwright not installed
  - Anti-bot detection
  - Website structure changed

### Issue: Timeout
- Increase timeout in Postman settings
- Large page ranges may take several minutes
- Consider scraping fewer pages per request

---

## Next Steps

1. Import this collection into Postman
2. Set up environment variables
3. Run health check to verify API is working
4. Test single page scraping
5. Proceed with larger scraping jobs

Happy testing! ??
