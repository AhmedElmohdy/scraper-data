# ?? Etimad Scraper API - Quick Start Guide

## ? Project Converted to Web API

Your Etimad Scraper is now a **Web API** that can be tested with **Postman**!

---

## ?? Quick Start

### 1. Run the API
```bash
cd EtimadScraper
dotnet run
```

### 2. Open Swagger UI
```
https://localhost:7000/swagger
```

### 3. Test with Postman
Base URL: `https://localhost:7000` or `http://localhost:5000`

---

## ?? API Endpoints

### **GET** `/api/TenderScraper/status`
Health check and current configuration

### **GET** `/api/TenderScraper/config`
Get scraper configuration details

### **POST** `/api/TenderScraper/scrape`
Scrape tenders with default settings (pages 1-5)

### **GET** `/api/TenderScraper/scrape/page/{pageNumber}`
Scrape a single page (e.g., `/scrape/page/3`)

### **POST** `/api/TenderScraper/scrape/custom`
Scrape custom page range with options

---

## ?? Postman Quick Test

### Test 1: Health Check
```
GET https://localhost:7000/api/TenderScraper/status
```

### Test 2: Scrape Single Page
```
GET https://localhost:7000/api/TenderScraper/scrape/page/1
```

### Test 3: Scrape Default (5 pages)
```
POST https://localhost:7000/api/TenderScraper/scrape
```

### Test 4: Scrape Custom Range
```
POST https://localhost:7000/api/TenderScraper/scrape/custom
Content-Type: application/json

{
  "startPage": 1,
  "endPage": 3,
  "saveToFile": true,
  "outputFileName": "my_tenders.json"
}
```

---

## ?? What Changed

### ? Files Modified
- `EtimadScraper.csproj` - Converted to Web SDK
- `Program.cs` - Now uses ASP.NET Core Web API
- `appsettings.json` - Updated with web logging

### ? Files Added
- `Controllers/TenderScraperController.cs` - API Controller
- `Properties/launchSettings.json` - Launch configuration
- `POSTMAN_GUIDE.md` - Complete Postman documentation
- `API_QUICK_START.md` - This file

---

## ?? Request/Response Examples

### Scrape Custom Range Request
```json
{
  "startPage": 1,
  "endPage": 10,
  "saveToFile": true,
  "outputFileName": "tenders_2024.json"
}
```

### Success Response
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

### Error Response
```json
{
  "success": false,
  "message": "Scraping failed: Anti-bot protection detected",
  "totalCount": 0,
  "startPage": 1,
  "endPage": 5,
  "tenders": [],
  "scrapedAt": "2024-01-10T10:30:00Z"
}
```

---

## ?? Configuration

Edit configuration in `Program.cs`:

```csharp
builder.Services.AddSingleton(new ScraperConfiguration
{
    BaseUrl = "https://tenders.etimad.sa/Tender/AllTendersForVisitor",
    Headless = true,      // Set to false to see browser
    StartPage = 1,
    MaxPages = 5,
    PageLoadTimeout = 30000,
    DelayBetweenPages = 2000,
    MaxRetryAttempts = 3,
    OutputFilePath = "tenders.json"
});
```

---

## ?? API Features

? **5 Endpoints** - Health, Config, Default, Single Page, Custom Range  
? **Swagger UI** - Interactive API documentation  
? **Error Handling** - Proper HTTP status codes  
? **Validation** - Request validation and limits  
? **Logging** - Console logging for debugging  
? **Flexible** - Custom page ranges and file output  

---

## ?? Limits

- **Maximum pages per request**: 50
- **Minimum page number**: 1
- **Response timeout**: Based on configuration
- **File saving**: Optional per request

---

## ??? Troubleshooting

### Issue: Port Already in Use
Change ports in `Properties/launchSettings.json`:
```json
"applicationUrl": "https://localhost:7001;http://localhost:5001"
```

### Issue: Swagger Not Loading
Navigate directly to:
```
https://localhost:7000/swagger/index.html
```

### Issue: HTTPS Certificate Error
Use HTTP for testing:
```
http://localhost:5000/swagger
```

Or trust the development certificate:
```bash
dotnet dev-certs https --trust
```

---

## ?? Documentation Files

- **POSTMAN_GUIDE.md** - Complete Postman testing guide
- **API_QUICK_START.md** - This file
- **README.md** - Original project documentation
- **SETUP_GUIDE.md** - Detailed setup instructions

---

## ?? You're Ready!

1. Run: `dotnet run`
2. Open: `https://localhost:7000/swagger`
3. Test endpoints in Swagger or Postman
4. Check console for scraping progress
5. Find scraped data in JSON files

**Happy API testing! ??**
