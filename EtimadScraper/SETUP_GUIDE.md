# Etimad Tender Scraper - Complete Setup Guide

## ?? Quick Start (5 minutes)

### Step 1: Navigate to the project directory
```bash
cd EtimadScraper
```

### Step 2: Restore NuGet packages
```bash
dotnet restore
```

### Step 3: Build the project
```bash
dotnet build
```

### Step 4: Install Playwright browsers (REQUIRED)

**On Windows:**
```powershell
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

**On Linux/macOS:**
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

**If you don't have PowerShell Core:**
- Windows: Install from Microsoft Store or https://github.com/PowerShell/PowerShell/releases
- Linux: `sudo apt-get install -y powershell` (Ubuntu/Debian)
- macOS: `brew install powershell/tap/powershell`

### Step 5: Run the scraper
```bash
dotnet run
```

That's it! The scraper will:
1. Navigate to the Etimad tenders website
2. Scrape 5 pages by default
3. Save results to `tenders.json`
4. Display progress in the console

---

## ?? Package Installation Commands

If you need to manually install packages:

```bash
# Core Playwright package
dotnet add package Microsoft.Playwright --version 1.49.0

# Logging packages
dotnet add package Microsoft.Extensions.Logging --version 9.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 9.0.0

# Dependency Injection
dotnet add package Microsoft.Extensions.DependencyInjection --version 9.0.0

# JSON serialization (already in .NET 9)
# System.Text.Json is built-in
```

---

## ?? Configuration Options

Edit `Program.cs` to customize behavior:

```csharp
services.AddSingleton(new ScraperConfiguration
{
    // Website URL
    BaseUrl = "https://tenders.etimad.sa/Tender/AllTendersForVisitor",
    
    // Show browser window (useful for debugging)
    Headless = true,  // Change to false to see browser
    
    // Page range to scrape
    StartPage = 1,    // Start from page 1
    MaxPages = 5,     // Scrape 5 pages (1-5)
    
    // Timeouts and delays
    PageLoadTimeout = 30000,     // 30 seconds
    DelayBetweenPages = 2000,    // 2 seconds between pages
    
    // Retry settings
    MaxRetryAttempts = 3,        // Retry 3 times on failure
    
    // Output file
    OutputFilePath = "tenders.json"
});
```

### Common Configurations

**Debug mode (see what's happening):**
```csharp
Headless = false,
DelayBetweenPages = 3000  // Slower for observation
```

**Production mode (fast scraping):**
```csharp
Headless = true,
MaxPages = 50,            // More pages
DelayBetweenPages = 1000  // Faster
```

**Conservative mode (avoid rate limiting):**
```csharp
Headless = true,
MaxPages = 10,
DelayBetweenPages = 5000,  // 5 seconds between pages
MaxRetryAttempts = 5
```

---

## ?? Updating Selectors

If the website structure changes, update `Configuration/ScraperSelectors.cs`:

```csharp
public static class ScraperSelectors
{
    // Main container
    public const string TenderTableContainer = "table.table";
    
    // Individual tender rows
    public const string TenderRow = "tbody tr";
    
    // Data fields (use Chrome DevTools to find correct selectors)
    public const string TenderNumber = "td:nth-child(1)";
    public const string TenderTitle = "td:nth-child(2) a";
    public const string Organization = "td:nth-child(3)";
    public const string PublishDate = "td:nth-child(4)";
    public const string ClosingDate = "td:nth-child(5)";
    // ... etc
}
```

**How to find selectors:**
1. Open the website in Chrome
2. Right-click element ? Inspect
3. Right-click in DevTools ? Copy ? Copy selector
4. Update the constant in ScraperSelectors.cs

---

## ?? Output Format

The scraper generates `tenders.json` with this structure:

```json
[
  {
    "tenderNumber": "12345678",
    "title": "????? ????? ?????? ???????",
    "organization": "????? ?????? ??????? ????????",
    "publishDate": "14/03/1446 03:30 ?",
    "closingDate": "21/03/1446 03:30 ?",
    "detailsUrl": "https://tenders.etimad.sa/Tender/Details/12345678",
    "status": "Active",
    "additionalInfo": "",
    "scrapedAt": "2024-01-10T10:30:00Z"
  }
]
```

---

## ??? Troubleshooting

### Problem: "Playwright not found" error

**Solution:**
```bash
# Make sure you're in the EtimadScraper directory
cd EtimadScraper

# Build first
dotnet build

# Then install Playwright browsers
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

### Problem: "No tenders scraped"

**Possible causes:**
1. **Website structure changed** ? Update selectors in `ScraperSelectors.cs`
2. **Anti-bot protection** ? Check logs for "Anti-bot protection detected"
3. **Network issues** ? Check internet connection

**Debug steps:**
```csharp
// In Program.cs, change:
Headless = false,  // See what's happening in browser
```

Run again and watch the browser window.

### Problem: "Anti-bot protection detected"

**What this means:**
The website is blocking automated access (CAPTCHA, Cloudflare, etc.)

**Solutions:**
1. Increase delays between pages
2. Use residential proxies (advanced)
3. Manually verify you can access the site in a normal browser
4. Contact website owner for API access

### Problem: Build errors

**Solution:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Problem: PowerShell not found

**Windows:**
```powershell
# Install PowerShell Core
winget install Microsoft.PowerShell
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt-get update
sudo apt-get install -y powershell
```

**macOS:**
```bash
brew install powershell/tap/powershell
```

---

## ?? Performance Tips

### Faster scraping
```csharp
Headless = true,              // No UI overhead
DelayBetweenPages = 1000,     // Minimum delay
MaxRetryAttempts = 2          // Fewer retries
```

### More reliable scraping
```csharp
Headless = true,
DelayBetweenPages = 3000,     // Respectful delay
MaxRetryAttempts = 5,         // More retries
PageLoadTimeout = 60000       // Longer timeout
```

### Debug scraping issues
```csharp
Headless = false,             // See browser
DelayBetweenPages = 5000      // Slow down
```

---

## ??? Next Steps: SQL Server Integration

### 1. Add Entity Framework Core packages
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
```

### 2. Create DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using EtimadScraper.Models;

public class TenderDbContext : DbContext
{
    public DbSet<TenderDto> Tenders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlServer(
            "Server=localhost;Database=EtimadTenders;Integrated Security=true;TrustServerCertificate=true;"
        );
    }
}
```

### 3. Update TenderDto for EF Core

```csharp
public class TenderDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    // ... rest of properties
}
```

### 4. Create migration and database

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 5. Save to database instead of JSON

```csharp
using (var context = new TenderDbContext())
{
    await context.Tenders.AddRangeAsync(tenders);
    await context.SaveChangesAsync();
}
```

---

## ?? Legal & Ethical Considerations

**Before running this scraper:**

? **DO:**
- Check the website's `robots.txt` file
- Add reasonable delays between requests
- Respect rate limits
- Only scrape publicly available data
- Use the data responsibly

? **DON'T:**
- Overload the server with requests
- Bypass authentication or paywalls
- Scrape personal or sensitive data
- Violate terms of service
- Use scraped data for harmful purposes

**robots.txt:** Check https://tenders.etimad.sa/robots.txt

---

## ?? Support

**Common issues:**
1. Playwright installation ? See troubleshooting above
2. Selector updates ? Use Chrome DevTools
3. Anti-bot protection ? Add delays, use proxies
4. Performance ? Adjust configuration

**Logs location:**
- Console output shows all operations
- Errors are clearly marked with ?
- Success messages marked with ?

---

## ?? Sample Commands

### Scrape first 10 pages
```csharp
// In Program.cs
MaxPages = 10,
```

### Scrape pages 5-15
```csharp
// In Program.cs
StartPage = 5,
MaxPages = 11,  // Will scrape pages 5-15 (11 pages total)
```

### Save to different file
```csharp
// In Program.cs
OutputFilePath = "tenders_2024.json"
```

### Run with verbose logging
```csharp
// In ConfigureServices method
configure.SetMinimumLevel(LogLevel.Debug);  // Shows more details
```

---

## ? Verification Checklist

Before running in production:

- [ ] Playwright browsers installed
- [ ] Configuration reviewed
- [ ] Output file path set
- [ ] Delays configured appropriately
- [ ] Tested with Headless = false first
- [ ] Selectors verified against current website
- [ ] Error handling tested
- [ ] Legal considerations reviewed
- [ ] Rate limiting respected

---

## ?? Ready to Run!

```bash
cd EtimadScraper
dotnet run
```

Watch the console for progress. Results will be in `tenders.json`.

Happy scraping! ??
