# Install Playwright Browsers for EtimadScraper

Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?     Playwright Browser Installation Script                ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the EtimadScraper directory
if (-not (Test-Path "EtimadScraper.csproj")) {
    Write-Host "? Error: Not in EtimadScraper directory" -ForegroundColor Red
    Write-Host "Please run this script from the EtimadScraper directory:" -ForegroundColor Yellow
    Write-Host "   cd EtimadScraper" -ForegroundColor Gray
    Write-Host "   .\install-playwright.ps1" -ForegroundColor Gray
    exit 1
}

Write-Host "?? Checking for Playwright installation script..." -ForegroundColor Yellow

# Check Debug folder first
$debugScript = "bin\Debug\net9.0\playwright.ps1"
$releaseScript = "bin\Release\net9.0\playwright.ps1"

$scriptToUse = $null

if (Test-Path $debugScript) {
    $scriptToUse = $debugScript
    Write-Host "? Found Playwright script in Debug folder" -ForegroundColor Green
}
elseif (Test-Path $releaseScript) {
    $scriptToUse = $releaseScript
    Write-Host "? Found Playwright script in Release folder" -ForegroundColor Green
}
else {
    Write-Host "??  Playwright script not found. Building project first..." -ForegroundColor Yellow
    Write-Host ""
    
    # Build the project
    Write-Host "?? Building project..." -ForegroundColor Yellow
    dotnet build
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Build failed. Please fix build errors first." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "? Build successful" -ForegroundColor Green
    
    # Check again after build
    if (Test-Path $debugScript) {
        $scriptToUse = $debugScript
    }
    else {
        Write-Host "? Playwright script still not found after build" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "?? Installing Playwright Chromium browser..." -ForegroundColor Yellow
Write-Host "   This may take a few minutes..." -ForegroundColor Gray
Write-Host ""

# Run the installation
pwsh $scriptToUse install chromium

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
    Write-Host "?              ? INSTALLATION SUCCESSFUL                    ?" -ForegroundColor Green
    Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
    Write-Host ""
    Write-Host "? Playwright Chromium browser installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now run the scraper:" -ForegroundColor Yellow
    Write-Host "   dotnet run" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or test the API:" -ForegroundColor Yellow
    Write-Host "   1. Start API: dotnet run" -ForegroundColor Gray
    Write-Host "   2. Open Swagger: https://localhost:7000/swagger" -ForegroundColor Gray
    Write-Host "   3. Test endpoint: GET /api/TenderScraper/status" -ForegroundColor Gray
}
else {
    Write-Host ""
    Write-Host "? Installation failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try manual installation:" -ForegroundColor Yellow
    Write-Host "   pwsh $scriptToUse install chromium" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or install all browsers:" -ForegroundColor Yellow
    Write-Host "   pwsh $scriptToUse install" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
