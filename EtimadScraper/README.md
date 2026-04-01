# Etimad Tender Scraper

Production-ready .NET 9 console application for scraping tender data from the Saudi Etimad platform.

## Features

- ? Scrapes tender listings from https://tenders.etimad.sa
- ? Uses Playwright for JavaScript-rendered content
- ? Multi-page scraping with configurable page ranges
- ? Retry logic with exponential backoff
- ? Anti-bot/CAPTCHA detection
- ? Comprehensive logging
- ? JSON output
- ? Clean architecture ready for SQL Server integration
- ? Strongly typed DTOs
- ? Configurable headless/headed mode

## Prerequisites

- .NET 9 SDK
- PowerShell (for Playwright installation)

## Installation

### 1. Restore NuGet packages

```bash
cd EtimadScraper
dotnet restore
```

### 2. Build the project

```bash
dotnet build
```

### 3. Install Playwright browsers

**Windows (PowerShell):**
```powershell
pwsh bin/Debug/net9.0/playwright.ps1 install
```

**Linux/macOS:**
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

Or install chromium only:
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

## Usage

### Run the scraper

```bash
dotnet run
```

### Configuration

Edit `Program.cs` to modify scraper settings:

```csharp
services.AddSingleton(new ScraperConfiguration
{
    BaseUrl = "https://tenders.etimad.sa/Tender/AllTendersForVisitor",
    Headless = true,              // Set to false to see browser
    StartPage = 1,                 // Starting page number
    MaxPages = 5,                  // Number of pages to scrape
    PageLoadTimeout = 30000,       // Page load timeout (ms)
    DelayBetweenPages = 2000,      // Delay between pages (ms)
    MaxRetryAttempts = 3,          // Retry attempts
    OutputFilePath = "tenders.json" // Output file
});
```

### Output

The scraper generates a `tenders.json` file with the following structure:

```json
[
  {
    "tenderNumber": "12345",
    "title": "Tender title in Arabic",
    "organization": "Government Entity",
    "publishDate": "01/01/2024",
    "closingDate": "15/01/2024",
    "detailsUrl": "https://tenders.etimad.sa/Tender/Details/...",
    "status": "Open",
    "additionalInfo": "",
    "scrapedAt": "2024-01-10T10:30:00Z"
  }
]
```

## Project Structure

```
EtimadScraper/
??? Configuration/
?   ??? ScraperConfiguration.cs   # Scraper settings
?   ??? ScraperSelectors.cs       # CSS selectors (easy to update)
??? Models/
?   ??? TenderDto.cs              # Tender data model
??? Services/
?   ??? EtimadScraperService.cs   # Main scraping logic
??? Program.cs                     # Entry point with DI setup
??? EtimadScraper.csproj          # Project file
```

## Key Components

### EtimadScraperService

Main scraping service with methods:
- `ScrapeTendersAsync(startPage, endPage)` - Scrape multiple pages
- `SaveToJsonAsync(tenders, filePath)` - Save to JSON

### ScraperSelectors

Centralized CSS selectors that can be easily updated if the website structure changes:
- `TenderRow` - Selector for tender rows
- `TenderNumber` - Tender reference selector
- `TenderTitle` - Tender title selector
- And more...

### TenderDto

Strongly-typed model representing tender data, ready for SQL Server integration.

## Error Handling

The scraper includes:
- ? Retry logic with exponential backoff
- ? Anti-bot/CAPTCHA detection with graceful exit
- ? Comprehensive error logging
- ? Page-level error recovery (continues to next page)

## Anti-Bot Protection

If the scraper detects anti-bot protection (CAPTCHA, Cloudflare, etc.), it will:
1. Log a clear error message
2. Stop gracefully
3. Exit with appropriate error code

Common indicators detected:
- reCAPTCHA
- Cloudflare challenge
- PerimeterX
- "Access Denied" pages
- "Human Verification" pages

## Extending to SQL Server

The code is designed for easy SQL Server integration:

1. Add Entity Framework Core packages
2. Create `DbContext` with `TenderDto` as entity
3. Replace `SaveToJsonAsync` with `SaveToDatabaseAsync`
4. Add connection string to configuration

## Troubleshooting

### Playwright not found
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

### No tenders scraped
- Check if selectors need updating (edit `ScraperSelectors.cs`)
- Set `Headless = false` to see what's happening
- Check logs for anti-bot protection

### Anti-bot protection detected
- The website may require manual verification
- Consider using residential proxies
- Add longer delays between requests
- Respect robots.txt

## Dependencies

```xml
<PackageReference Include="Microsoft.Playwright" Version="1.49.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
<PackageReference Include="System.Text.Json" Version="9.0.0" />
```

## License

This is a sample project for educational purposes. Make sure to comply with the website's terms of service and robots.txt when scraping.

## Legal Notice

Web scraping should be done responsibly:
- ? Respect robots.txt
- ? Add delays between requests
- ? Don't overload servers
- ? Comply with terms of service
- ? Only scrape publicly available data

## Author

Created as a production-ready template for web scraping with .NET 9 and Playwright.
