# ?? Fix: Playwright Browsers Not Installed

## ? Error You're Seeing

```json
{
  "success": false,
  "message": "Scraping failed: Executable doesn't exist at C:\\Users\\ahmed\\AppData\\Local\\ms-playwright\\chromium_headless_shell-1148\\chrome-win\\headless_shell.exe",
  ...
}
```

**This means:** Playwright browsers are not installed on your system.

---

## ? Quick Fix (Choose One Method)

### **Method 1: Automated Script (Easiest)**

```powershell
cd EtimadScraper
.\install-playwright.ps1
```

This script will:
- ? Check if build exists
- ? Build if needed
- ? Install Chromium browser automatically
- ? Verify installation

---

### **Method 2: Manual Installation**

#### Step 1: Navigate to Project
```powershell
cd EtimadScraper
```

#### Step 2: Build the Project
```powershell
dotnet build
```

#### Step 3: Install Chromium Browser
```powershell
# For Debug build
pwsh bin/Debug/net9.0/playwright.ps1 install chromium

# OR for Release build
pwsh bin/Release/net9.0/playwright.ps1 install chromium
```

---

### **Method 3: Install All Browsers**

If you want to install all Playwright browsers (chromium, firefox, webkit):

```powershell
cd EtimadScraper
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install
```

---

## ?? Verify Installation

After installation, test the API:

### Test 1: Run the API
```powershell
dotnet run
```

### Test 2: Open Swagger
```
https://localhost:7000/swagger
```

### Test 3: Try Status Endpoint
```
GET /api/TenderScraper/status
```

Should return:
```json
{
  "status": "healthy",
  "service": "Etimad Tender Scraper API",
  ...
}
```

### Test 4: Try Scraping Single Page
```
GET /api/TenderScraper/scrape/page/1
```

Should scrape page 1 successfully!

---

## ?? Installation Paths

Playwright will install browsers to:
```
Windows: C:\Users\<username>\AppData\Local\ms-playwright\
Linux:   ~/.cache/ms-playwright/
macOS:   ~/Library/Caches/ms-playwright/
```

**Chromium size:** ~400-500 MB

---

## ??? Troubleshooting

### Issue: "pwsh command not found"

**Solution: Install PowerShell Core**

**Windows:**
```powershell
winget install Microsoft.PowerShell
```

**Ubuntu/Debian:**
```bash
sudo apt-get install -y powershell
```

**macOS:**
```bash
brew install powershell
```

---

### Issue: "playwright.ps1 not found"

**Solution: Build the project first**

```powershell
cd EtimadScraper
dotnet clean
dotnet build
# Then try installation again
```

---

### Issue: "Access denied" or "Permission denied"

**Solution: Run as Administrator (Windows)**

1. Right-click PowerShell
2. Select "Run as Administrator"
3. Navigate to EtimadScraper directory
4. Run installation command

---

### Issue: Installation hangs or fails

**Solution: Clear cache and retry**

```powershell
# Clear Playwright cache
Remove-Item -Recurse -Force "$env:LOCALAPPDATA\ms-playwright" -ErrorAction SilentlyContinue

# Retry installation
cd EtimadScraper
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
```

---

### Issue: "Disk space" error

**Solution:** Playwright browsers need ~400-500 MB per browser

- Free up disk space
- Or install only chromium (not all browsers)

---

## ?? Improved Error Messages

The API now provides better error messages! After the fix, if Playwright isn't installed, you'll see:

```json
{
  "success": false,
  "message": "Playwright browsers not installed. Please install chromium browser.",
  "troubleshooting": [
    "? Playwright browsers are not installed",
    "?? Solution: Run this command in PowerShell:",
    "   cd EtimadScraper",
    "   pwsh bin/Debug/net9.0/playwright.ps1 install chromium",
    "",
    "Or if built in Release mode:",
    "   pwsh bin/Release/net9.0/playwright.ps1 install chromium"
  ]
}
```

---

## ? After Installation

Once Playwright is installed:

### ? You can:
1. Run the API: `dotnet run`
2. Access Swagger: `https://localhost:7000/swagger`
3. Scrape tenders via API endpoints
4. Use Postman to test endpoints

### ?? API will work with:
- GET `/api/TenderScraper/status` - Health check
- GET `/api/TenderScraper/scrape/page/1` - Scrape single page
- POST `/api/TenderScraper/scrape` - Scrape multiple pages
- POST `/api/TenderScraper/scrape/custom` - Custom range

---

## ?? Quick Complete Setup

If starting fresh, here's the complete setup:

```powershell
# 1. Navigate to project
cd EtimadScraper

# 2. Restore packages
dotnet restore

# 3. Build project
dotnet build

# 4. Install Playwright browsers (CRITICAL STEP)
pwsh bin/Debug/net9.0/playwright.ps1 install chromium

# 5. Run the API
dotnet run

# 6. Open browser to Swagger
# https://localhost:7000/swagger
```

---

## ?? Still Having Issues?

### Check these:
1. ? PowerShell Core installed: `pwsh --version`
2. ? .NET 9 installed: `dotnet --version`
3. ? Project builds: `dotnet build`
4. ? Playwright script exists: Check `bin/Debug/net9.0/playwright.ps1`

### Get Help:
- Check console output for detailed errors
- Review `SWAGGER_TROUBLESHOOTING.md`
- Check `SETUP_GUIDE.md` for complete setup

---

## ?? Success Indicators

You'll know it worked when:

? Installation completes without errors  
? API runs without "Executable doesn't exist" error  
? Swagger shows all 5 endpoints  
? GET `/api/TenderScraper/scrape/page/1` returns tender data  
? Console logs show "Browser initialized successfully"  

---

## ?? Summary

**Problem:** Playwright browsers not installed  
**Solution:** Run `pwsh bin/Debug/net9.0/playwright.ps1 install chromium`  
**Quick Script:** `.\install-playwright.ps1`  
**Verify:** Test API at `https://localhost:7000/swagger`  

**After installation, your scraper is ready to use! ??**
