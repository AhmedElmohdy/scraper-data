#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Install Playwright browsers for IIS deployment

.DESCRIPTION
    This script installs Playwright Chromium browser in a shared location
    and configures IIS to use it. Run this on your IIS server.

.PARAMETER AppPath
    Path to your deployed application (default: C:\inetpub\wwwroot\EtimadScraper)

.PARAMETER BrowserPath
    Shared location for Playwright browsers (default: C:\playwright-browsers)

.PARAMETER AppPoolName
    Name of your IIS Application Pool (default: EtimadScraperAppPool)

.EXAMPLE
    .\install-iis-playwright.ps1

.EXAMPLE
    .\install-iis-playwright.ps1 -AppPath "C:\inetpub\wwwroot\MyApp" -AppPoolName "MyAppPool"
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$AppPath = "C:\inetpub\wwwroot\EtimadScraper",
    
    [Parameter(Mandatory=$false)]
    [string]$BrowserPath = "C:\playwright-browsers",
    
    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = "EtimadScraperAppPool"
)

Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?   Playwright IIS Installation Script                      ?" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

# Verify running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "? This script must be run as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  App Path: $AppPath" -ForegroundColor Gray
Write-Host "  Browser Path: $BrowserPath" -ForegroundColor Gray
Write-Host "  App Pool: $AppPoolName" -ForegroundColor Gray
Write-Host ""

# Step 1: Verify app path exists
Write-Host "[1/7] Verifying application path..." -ForegroundColor Yellow
if (-not (Test-Path $AppPath)) {
    Write-Host "? Application path not found: $AppPath" -ForegroundColor Red
    Write-Host "Please deploy your application first or provide correct path." -ForegroundColor Yellow
    exit 1
}
Write-Host "? Application path verified" -ForegroundColor Green

# Step 2: Check for playwright.ps1
Write-Host "[2/7] Checking for Playwright installation script..." -ForegroundColor Yellow
$playwrightScript = Join-Path $AppPath "bin\Release\net9.0\playwright.ps1"
if (-not (Test-Path $playwrightScript)) {
    $playwrightScript = Join-Path $AppPath "bin\Debug\net9.0\playwright.ps1"
    if (-not (Test-Path $playwrightScript)) {
        Write-Host "? playwright.ps1 not found in bin folder" -ForegroundColor Red
        Write-Host "Please build/publish your project first:" -ForegroundColor Yellow
        Write-Host "  dotnet publish -c Release -o $AppPath" -ForegroundColor Gray
        exit 1
    }
}
Write-Host "? Playwright script found: $playwrightScript" -ForegroundColor Green

# Step 3: Check PowerShell Core
Write-Host "[3/7] Checking for PowerShell Core..." -ForegroundColor Yellow
try {
    $pwshVersion = pwsh --version
    Write-Host "? PowerShell Core installed: $pwshVersion" -ForegroundColor Green
}
catch {
    Write-Host "? PowerShell Core not installed" -ForegroundColor Red
    Write-Host "Install it using: winget install Microsoft.PowerShell" -ForegroundColor Yellow
    exit 1
}

# Step 4: Create browser directory
Write-Host "[4/7] Creating browser directory..." -ForegroundColor Yellow
if (-not (Test-Path $BrowserPath)) {
    New-Item -ItemType Directory -Path $BrowserPath -Force | Out-Null
    Write-Host "? Created directory: $BrowserPath" -ForegroundColor Green
} else {
    Write-Host "? Directory already exists: $BrowserPath" -ForegroundColor Green
}

# Step 5: Install Playwright browsers
Write-Host "[5/7] Installing Chromium browser..." -ForegroundColor Yellow
Write-Host "    This may take a few minutes..." -ForegroundColor Gray

$env:PLAYWRIGHT_BROWSERS_PATH = $BrowserPath
Push-Location $AppPath

try {
    $installOutput = pwsh $playwrightScript install chromium 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Chromium browser installed successfully" -ForegroundColor Green
        
        # Verify installation
        $chromiumPath = Get-ChildItem -Path $BrowserPath -Recurse -Filter "chrome*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($chromiumPath) {
            Write-Host "  Browser executable: $($chromiumPath.FullName)" -ForegroundColor Gray
        }
    } else {
        Write-Host "? Failed to install Chromium browser" -ForegroundColor Red
        Write-Host $installOutput -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "? Error during browser installation: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}

# Step 6: Set system environment variable
Write-Host "[6/7] Setting system environment variable..." -ForegroundColor Yellow
[System.Environment]::SetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH', $BrowserPath, 'Machine')
Write-Host "? Environment variable set: PLAYWRIGHT_BROWSERS_PATH=$BrowserPath" -ForegroundColor Green

# Step 7: Grant permissions
Write-Host "[7/7] Granting permissions..." -ForegroundColor Yellow

# Grant permissions to browser directory
try {
    icacls $BrowserPath /grant "IIS APPPOOL\$AppPoolName:(OI)(CI)RX" /T 2>&1 | Out-Null
    Write-Host "? Granted read/execute permissions to browser directory" -ForegroundColor Green
}
catch {
    Write-Host "??  Warning: Could not grant permissions to IIS APPPOOL\$AppPoolName" -ForegroundColor Yellow
    Write-Host "   The app pool might not exist yet. Grant permissions manually if needed:" -ForegroundColor Gray
    Write-Host "   icacls $BrowserPath /grant `"IIS APPPOOL\$AppPoolName:(OI)(CI)RX`" /T" -ForegroundColor Gray
}

# Grant permissions to app directory
try {
    icacls $AppPath /grant "IIS APPPOOL\$AppPoolName:(OI)(CI)M" /T 2>&1 | Out-Null
    Write-Host "? Granted modify permissions to application directory" -ForegroundColor Green
}
catch {
    Write-Host "??  Warning: Could not grant permissions to application directory" -ForegroundColor Yellow
}

# Step 8: Update web.config
Write-Host ""
Write-Host "Creating/updating web.config..." -ForegroundColor Yellow

$webConfigPath = Join-Path $AppPath "web.config"
$webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\EtimadScraper.dll" 
                stdoutLogEnabled="true" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="InProcess">
      <environmentVariables>
        <environmentVariable name="PLAYWRIGHT_BROWSERS_PATH" value="$BrowserPath" />
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
"@

try {
    $webConfigContent | Out-File -FilePath $webConfigPath -Encoding UTF8 -Force
    Write-Host "? web.config created/updated" -ForegroundColor Green
}
catch {
    Write-Host "??  Warning: Could not create web.config" -ForegroundColor Yellow
}

# Step 9: Restart IIS
Write-Host ""
Write-Host "Restarting IIS..." -ForegroundColor Yellow
try {
    iisreset /noforce
    Write-Host "? IIS restarted successfully" -ForegroundColor Green
}
catch {
    Write-Host "??  Warning: Could not restart IIS automatically" -ForegroundColor Yellow
    Write-Host "   Please restart IIS manually: iisreset" -ForegroundColor Gray
}

Write-Host ""
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host "?              ? INSTALLATION COMPLETE                      ?" -ForegroundColor Green
Write-Host "??????????????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Create IIS Application Pool: $AppPoolName" -ForegroundColor Yellow
Write-Host "2. Create IIS Website/Application pointing to: $AppPath" -ForegroundColor Yellow
Write-Host "3. Test API endpoint: https://your-server/api/TenderScraper/status" -ForegroundColor Yellow
Write-Host "4. Test scraping: https://your-server/swagger" -ForegroundColor Yellow
Write-Host ""

Write-Host "Verification Commands:" -ForegroundColor Cyan
Write-Host "  # Check browser installation" -ForegroundColor Gray
Write-Host "  dir $BrowserPath" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Check environment variable" -ForegroundColor Gray
Write-Host "  [System.Environment]::GetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH', 'Machine')" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Check IIS logs" -ForegroundColor Gray
Write-Host "  type $AppPath\logs\stdout_*.log" -ForegroundColor Gray
Write-Host ""

Write-Host "Troubleshooting:" -ForegroundColor Cyan
Write-Host "  If scraping still fails, check:" -ForegroundColor Yellow
Write-Host "  - IIS logs in: $AppPath\logs\" -ForegroundColor Gray
Write-Host "  - Event Viewer: Windows Logs ? Application" -ForegroundColor Gray
Write-Host "  - Ensure app pool identity has permissions" -ForegroundColor Gray
Write-Host ""

Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
