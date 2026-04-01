# Quick Commands Reference

## Initial Setup (One-time)

```bash
# Navigate to project
cd EtimadScraper

# Restore packages
dotnet restore

# Build project
dotnet build

# Install Playwright browsers (REQUIRED - do this after first build)
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

## Run the Scraper

```bash
# Run with default settings (5 pages)
dotnet run

# Or run the compiled executable directly
dotnet bin/Debug/net9.0/EtimadScraper.dll
```

## Package Installation (if needed manually)

```bash
dotnet add package Microsoft.Playwright --version 1.49.0
dotnet add package Microsoft.Extensions.Logging --version 9.0.0
dotnet add package Microsoft.Extensions.Logging.Console --version 9.0.0
dotnet add package Microsoft.Extensions.DependencyInjection --version 9.0.0
```

## Playwright Browser Installation

### Windows (PowerShell)
```powershell
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

### Linux/macOS
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

### Install All Browsers (chromium, firefox, webkit)
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

## Troubleshooting Commands

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Check installed packages
dotnet list package

# Verify .NET version
dotnet --version

# Check if PowerShell is installed
pwsh --version
```

## Install PowerShell Core (if needed)

### Windows
```powershell
winget install Microsoft.PowerShell
```

### Ubuntu/Debian
```bash
sudo apt-get update
sudo apt-get install -y powershell
```

### macOS
```bash
brew install powershell/tap/powershell
```

## Output

- Scraped data: `tenders.json` (in project root)
- Logs: Console output

## Configuration

Edit in `Program.cs`:
- `Headless`: true/false (show browser)
- `MaxPages`: number of pages to scrape
- `StartPage`: starting page number
- `DelayBetweenPages`: delay in milliseconds
- `OutputFilePath`: output file name

## Selectors

Update in `Configuration/ScraperSelectors.cs` if website structure changes.
