# Etimad Scraper - Quick Setup Script
# Run this script to set up and run the scraper automatically

Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?     Etimad Tender Scraper - Automated Setup Script        ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Function to check command success
function Test-LastCommand {
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Error occurred. Exiting..." -ForegroundColor Red
        exit 1
    }
}

# Step 1: Check .NET installation
Write-Host "?? Step 1: Checking .NET installation..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "? .NET version $dotnetVersion found" -ForegroundColor Green
} catch {
    Write-Host "? .NET 9 not found. Please install from: https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# Step 2: Check PowerShell version
Write-Host ""
Write-Host "?? Step 2: Checking PowerShell installation..." -ForegroundColor Yellow
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 7) {
    Write-Host "? PowerShell Core $($psVersion.Major).$($psVersion.Minor) found" -ForegroundColor Green
} else {
    Write-Host "??  Using Windows PowerShell $($psVersion.Major).$($psVersion.Minor)" -ForegroundColor Yellow
    Write-Host "   Recommend upgrading to PowerShell Core 7+" -ForegroundColor Yellow
}

# Step 3: Restore packages
Write-Host ""
Write-Host "?? Step 3: Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
Test-LastCommand
Write-Host "? Packages restored successfully" -ForegroundColor Green

# Step 4: Build project
Write-Host ""
Write-Host "?? Step 4: Building project..." -ForegroundColor Yellow
dotnet build --configuration Release
Test-LastCommand
Write-Host "? Project built successfully" -ForegroundColor Green

# Step 5: Install Playwright browsers
Write-Host ""
Write-Host "?? Step 5: Installing Playwright browsers..." -ForegroundColor Yellow
Write-Host "   This may take a few minutes..." -ForegroundColor Gray

$playwrightScript = "bin/Release/net9.0/playwright.ps1"
if (Test-Path $playwrightScript) {
    pwsh $playwrightScript install chromium
    if ($LASTEXITCODE -ne 0) {
        Write-Host "??  Playwright installation failed. Try manually:" -ForegroundColor Yellow
        Write-Host "   pwsh bin/Release/net9.0/playwright.ps1 install chromium" -ForegroundColor Gray
    } else {
        Write-Host "? Playwright browsers installed successfully" -ForegroundColor Green
    }
} else {
    Write-Host "??  Playwright script not found at: $playwrightScript" -ForegroundColor Yellow
    Write-Host "   Build might have failed or path is different" -ForegroundColor Gray
}

# Setup complete
Write-Host ""
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?              ? SETUP COMPLETED SUCCESSFULLY               ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""

# Ask user if they want to run the scraper now
Write-Host "Would you like to run the scraper now? (Y/N): " -ForegroundColor Cyan -NoNewline
$response = Read-Host

if ($response -eq 'Y' -or $response -eq 'y') {
    Write-Host ""
    Write-Host "?? Starting Etimad Scraper..." -ForegroundColor Cyan
    Write-Host ""
    dotnet run --configuration Release
} else {
    Write-Host ""
    Write-Host "?? To run the scraper later, use:" -ForegroundColor Yellow
    Write-Host "   dotnet run" -ForegroundColor Gray
    Write-Host ""
    Write-Host "?? For more information, check:" -ForegroundColor Yellow
    Write-Host "   • README.md - Full documentation" -ForegroundColor Gray
    Write-Host "   • SETUP_GUIDE.md - Detailed setup guide" -ForegroundColor Gray
    Write-Host "   • COMMANDS.md - Quick command reference" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Happy scraping! ??" -ForegroundColor Green
