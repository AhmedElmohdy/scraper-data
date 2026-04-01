# ?? IIS Deployment - Playwright Browser Installation Guide

## ? Problem

Your app works locally but fails on IIS with:
```json
{
  "message": "Playwright browsers not installed. Please install chromium browser."
}
```

**Root Cause:** Playwright browsers are installed for your local user but not for the IIS Application Pool identity.

---

## ? Solution: Install Playwright Browsers for IIS

### **Method 1: Install for All Users (Recommended)**

#### Step 1: Build Your Project in Release Mode

```powershell
# On your development machine or server
cd C:\inetpub\wwwroot\EtimadScraper  # Or your deployment path
dotnet build -c Release
```

#### Step 2: Install Playwright Browsers with System-Wide Flag

Run PowerShell **as Administrator**:

```powershell
# Navigate to deployment directory
cd C:\inetpub\wwwroot\EtimadScraper

# Install Chromium for all users
$env:PLAYWRIGHT_BROWSERS_PATH="C:\playwright-browsers"
pwsh bin/Release/net9.0/playwright.ps1 install chromium
```

This installs browsers to a shared location accessible by all users.

#### Step 3: Set Environment Variable for IIS

**Option A: Application Pool Environment Variable**

1. Open IIS Manager
2. Go to Application Pools ? Select your app pool
3. Advanced Settings ? Environment Variables
4. Add new variable:
   - **Name:** `PLAYWRIGHT_BROWSERS_PATH`
   - **Value:** `C:\playwright-browsers`

**Option B: System Environment Variable**

Run as Administrator:
```powershell
[System.Environment]::SetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH', 'C:\playwright-browsers', 'Machine')
```

Then restart IIS:
```powershell
iisreset
```

---

### **Method 2: Install for Specific App Pool Identity**

#### Step 1: Find Your App Pool Identity

```powershell
# In PowerShell as Administrator
Import-Module WebAdministration
$appPool = Get-Item "IIS:\AppPools\YourAppPoolName"
$appPool.processModel.userName
```

Common identities:
- `ApplicationPoolIdentity` (default)
- `NetworkService`
- `LocalSystem`
- Custom account

#### Step 2: Run Installation as App Pool Identity

**For ApplicationPoolIdentity:**

```powershell
# Run as Administrator
cd C:\inetpub\wwwroot\EtimadScraper

# Use PSExec to run as app pool identity
# Download PSExec from: https://download.sysinternals.com/files/PSTools.zip

.\PsExec.exe -i -u "IIS APPPOOL\YourAppPoolName" powershell -Command "cd C:\inetpub\wwwroot\EtimadScraper; pwsh bin/Release/net9.0/playwright.ps1 install chromium"
```

**For NetworkService or LocalSystem:**

```powershell
# Run as Administrator
cd C:\inetpub\wwwroot\EtimadScraper

# Install under system account
pwsh bin/Release/net9.0/playwright.ps1 install chromium
```

---

### **Method 3: Automated Installation on App Start (Not Recommended for Production)**

Add this to your `Program.cs` (only for testing):

```csharp
// In Program.cs, before builder.Build()
var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
if (exitCode != 0)
{
    Console.WriteLine("Failed to install Playwright browsers");
}
```

?? **Warning:** This runs installation on every app start and requires write permissions.

---

## ??? Complete IIS Deployment Checklist

### **Pre-Deployment**

- [ ] Build in Release mode: `dotnet build -c Release`
- [ ] Publish to folder: `dotnet publish -c Release -o ./publish`
- [ ] Copy `publish` folder to IIS server

### **On IIS Server**

#### 1. Install .NET 9 Hosting Bundle
```powershell
# Download and install from:
# https://dotnet.microsoft.com/download/dotnet/9.0
```

#### 2. Install PowerShell Core
```powershell
winget install Microsoft.PowerShell
```

#### 3. Install Playwright Browsers
```powershell
# As Administrator
cd C:\inetpub\wwwroot\EtimadScraper
$env:PLAYWRIGHT_BROWSERS_PATH="C:\playwright-browsers"
pwsh bin/Release/net9.0/playwright.ps1 install chromium
```

#### 4. Set Environment Variable
```powershell
# System-wide
[System.Environment]::SetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH', 'C:\playwright-browsers', 'Machine')

# Restart IIS
iisreset
```

#### 5. Configure App Pool
- Identity: `ApplicationPoolIdentity` or `NetworkService`
- .NET CLR Version: `No Managed Code`
- Enable 32-bit Applications: `False`
- Start Mode: `AlwaysRunning` (optional)

#### 6. Grant Permissions
```powershell
# Grant app pool identity access to playwright browsers
icacls "C:\playwright-browsers" /grant "IIS APPPOOL\YourAppPoolName:(OI)(CI)RX" /T

# Grant access to app directory
icacls "C:\inetpub\wwwroot\EtimadScraper" /grant "IIS APPPOOL\YourAppPoolName:(OI)(CI)M" /T
```

#### 7. Configure web.config
Ensure these settings in `web.config`:
```xml
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
        <environmentVariable name="PLAYWRIGHT_BROWSERS_PATH" value="C:\playwright-browsers" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

---

## ?? Verification Steps

### Step 1: Check Browser Installation

```powershell
# Check if browsers are installed
dir C:\playwright-browsers\chromium_headless_shell-*

# Or check user profile
dir "$env:LOCALAPPDATA\ms-playwright\chromium_headless_shell-*"
```

### Step 2: Test API Endpoint

Open browser:
```
https://your-server/api/TenderScraper/status
```

Should return:
```json
{
  "status": "healthy",
  "service": "Etimad Tender Scraper API",
  ...
}
```

### Step 3: Test Scraping

```
POST https://your-server/api/TenderScraper/scrape/custom
Body:
{
  "startPage": 1,
  "endPage": 1,
  "saveToFile": false
}
```

### Step 4: Check IIS Logs

```powershell
# Check stdout logs
type C:\inetpub\wwwroot\EtimadScraper\logs\stdout_*.log | Select-Object -Last 50
```

---

## ?? Troubleshooting

### Issue 1: "Executable doesn't exist" Error

**Solution:**
```powershell
# Reinstall browsers in shared location
$env:PLAYWRIGHT_BROWSERS_PATH="C:\playwright-browsers"
pwsh bin/Release/net9.0/playwright.ps1 install chromium --force

# Verify installation
dir C:\playwright-browsers
```

### Issue 2: "Access Denied" Error

**Solution:**
```powershell
# Grant permissions to app pool
$appPoolName = "YourAppPoolName"
icacls "C:\playwright-browsers" /grant "IIS APPPOOL\$appPoolName:(OI)(CI)RX" /T
icacls "C:\inetpub\wwwroot\EtimadScraper" /grant "IIS APPPOOL\$appPoolName:(OI)(CI)M" /T
```

### Issue 3: Environment Variable Not Recognized

**Solution:**
```powershell
# Add to web.config instead
# See web.config example above

# Or restart IIS after setting system variable
iisreset
```

### Issue 4: App Pool Crashes

**Solution:**
```powershell
# Check event viewer
eventvwr.msc
# Navigate to: Windows Logs ? Application

# Enable detailed logging in web.config
<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" />

# Check logs
type C:\inetpub\wwwroot\EtimadScraper\logs\stdout_*.log
```

---

## ?? Quick Fix Script

Save this as `install-iis-playwright.ps1` and run as Administrator:

```powershell
#Requires -RunAsAdministrator

param(
    [string]$AppPath = "C:\inetpub\wwwroot\EtimadScraper",
    [string]$BrowserPath = "C:\playwright-browsers",
    [string]$AppPoolName = "EtimadScraperAppPool"
)

Write-Host "Installing Playwright for IIS..." -ForegroundColor Cyan

# Step 1: Set browser path
$env:PLAYWRIGHT_BROWSERS_PATH = $BrowserPath
Write-Host "? Browser path set to: $BrowserPath" -ForegroundColor Green

# Step 2: Install browsers
cd $AppPath
if (Test-Path "bin/Release/net9.0/playwright.ps1") {
    Write-Host "Installing Chromium..." -ForegroundColor Yellow
    pwsh bin/Release/net9.0/playwright.ps1 install chromium
    Write-Host "? Chromium installed" -ForegroundColor Green
} else {
    Write-Host "? playwright.ps1 not found. Build project first." -ForegroundColor Red
    exit 1
}

# Step 3: Set system environment variable
[System.Environment]::SetEnvironmentVariable('PLAYWRIGHT_BROWSERS_PATH', $BrowserPath, 'Machine')
Write-Host "? Environment variable set" -ForegroundColor Green

# Step 4: Grant permissions
Write-Host "Granting permissions..." -ForegroundColor Yellow
icacls $BrowserPath /grant "IIS APPPOOL\$AppPoolName:(OI)(CI)RX" /T | Out-Null
icacls $AppPath /grant "IIS APPPOOL\$AppPoolName:(OI)(CI)M" /T | Out-Null
Write-Host "? Permissions granted" -ForegroundColor Green

# Step 5: Restart IIS
Write-Host "Restarting IIS..." -ForegroundColor Yellow
iisreset
Write-Host "? IIS restarted" -ForegroundColor Green

Write-Host ""
Write-Host "? Installation complete!" -ForegroundColor Green
Write-Host "Test your API at: https://your-server/api/TenderScraper/status" -ForegroundColor Cyan
```

**Usage:**
```powershell
.\install-iis-playwright.ps1 -AppPath "C:\inetpub\wwwroot\EtimadScraper" -AppPoolName "YourAppPoolName"
```

---

## ?? Production Deployment Checklist

- [ ] .NET 9 Hosting Bundle installed
- [ ] PowerShell Core installed
- [ ] Project published to IIS directory
- [ ] Playwright browsers installed to shared location
- [ ] `PLAYWRIGHT_BROWSERS_PATH` environment variable set
- [ ] App pool permissions granted
- [ ] web.config configured with environment variable
- [ ] IIS restarted
- [ ] API status endpoint tested
- [ ] Scraping endpoint tested

---

## ?? Expected Paths

| Component | Local Path | IIS Path |
|-----------|-----------|----------|
| **App** | `C:\...\EtimadScraper\` | `C:\inetpub\wwwroot\EtimadScraper\` |
| **Browsers (User)** | `%LOCALAPPDATA%\ms-playwright\` | N/A (use shared) |
| **Browsers (Shared)** | N/A | `C:\playwright-browsers\` |
| **Playwright Script** | `bin\Debug\net9.0\playwright.ps1` | `bin\Release\net9.0\playwright.ps1` |

---

## ?? Success Indicators

After following these steps, you should see:

1. ? API returns healthy status
2. ? Scraping endpoint works
3. ? No "browsers not installed" error
4. ? Console logs show "Browser initialized successfully"

---

**File:** `IIS_PLAYWRIGHT_DEPLOYMENT.md`  
**Date:** March 31, 2024  
**Status:** Ready for IIS Deployment
