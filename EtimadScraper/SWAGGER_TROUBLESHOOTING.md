# Swagger Troubleshooting Guide

## ? Problem: API Not Showing in Swagger

### Solution Applied:
Updated `Program.cs` to:
1. Enable Swagger in **all environments** (not just Development)
2. Added proper OpenAPI configuration
3. Set explicit Swagger endpoint and route prefix

---

## ?? How to Access Swagger

### Step 1: Run the Application
```bash
cd EtimadScraper
dotnet run
```

### Step 2: Check the Console Output
Look for URLs like:
```
Now listening on: https://localhost:7000
Now listening on: http://localhost:5000
```

### Step 3: Open Swagger UI
Navigate to **one of these URLs**:
- `https://localhost:7000/swagger`
- `https://localhost:7000/swagger/index.html`
- `http://localhost:5000/swagger` (if HTTPS has certificate issues)

---

## ?? Verification Checklist

### ? Check 1: Build Success
```bash
dotnet build
```
Should show: **Build succeeded**

### ? Check 2: Controller Exists
File should exist: `EtimadScraper/Controllers/TenderScraperController.cs`

### ? Check 3: Correct URL
Make sure you're using:
- `/swagger` (lowercase)
- NOT `/Swagger` or `/SWAGGER`

### ? Check 4: Correct Port
Use the port shown in console output, **not** hardcoded 7000/5000

---

## ??? Common Issues & Solutions

### Issue 1: "This site can't be reached"
**Solution:**
- Make sure the app is running (`dotnet run`)
- Check you're using the correct port from console output
- Try HTTP instead of HTTPS: `http://localhost:5000/swagger`

### Issue 2: "HTTPS Certificate Error"
**Solution:**
```bash
# Trust the development certificate
dotnet dev-certs https --trust
```
Or use HTTP: `http://localhost:5000/swagger`

### Issue 3: Blank Swagger Page
**Solution:**
- Clear browser cache
- Try incognito/private mode
- Check browser console for errors (F12)
- Navigate directly to: `https://localhost:7000/swagger/index.html`

### Issue 4: 404 Not Found
**Solution:**
- Make sure you're using `/swagger` not `/api/swagger`
- Check the app is running in the correct directory
- Rebuild the project: `dotnet clean && dotnet build`

### Issue 5: Controller Not Showing
**Solution:**
1. Make sure controller has `[ApiController]` attribute
2. Make sure controller has `[Route("api/[controller]")]` attribute
3. Rebuild: `dotnet build`
4. Restart the app

---

## ?? Expected Swagger UI

You should see:
- **Title**: "Etimad Tender Scraper API"
- **Version**: "v1"
- **5 Endpoints** under `TenderScraper`:
  1. `GET /api/TenderScraper/status`
  2. `GET /api/TenderScraper/config`
  3. `POST /api/TenderScraper/scrape`
  4. `GET /api/TenderScraper/scrape/page/{pageNumber}`
  5. `POST /api/TenderScraper/scrape/custom`

---

## ?? Quick Test in Swagger

### Test 1: Health Check
1. Click on `GET /api/TenderScraper/status`
2. Click **"Try it out"**
3. Click **"Execute"**
4. Should return status 200 with API info

### Test 2: Get Configuration
1. Click on `GET /api/TenderScraper/config`
2. Click **"Try it out"**
3. Click **"Execute"**
4. Should return scraper configuration

---

## ?? Step-by-Step Access Guide

### Method 1: Using dotnet run
```bash
cd EtimadScraper
dotnet run --launch-profile https
```
Browser should open automatically to Swagger

### Method 2: Manual Access
1. Run: `dotnet run`
2. Note the HTTPS URL from console
3. Open browser to: `{URL}/swagger`
   - Example: `https://localhost:7000/swagger`

### Method 3: Using Visual Studio
1. Open project in Visual Studio
2. Press F5 or click "Run"
3. Browser opens automatically to Swagger

---

## ?? Alternative: Test Without Swagger

If Swagger still doesn't work, test the API directly:

### Using curl:
```bash
curl https://localhost:7000/api/TenderScraper/status
```

### Using PowerShell:
```powershell
Invoke-WebRequest -Uri "https://localhost:7000/api/TenderScraper/status" -SkipCertificateCheck
```

### Using Postman:
1. Import: `Postman/EtimadScraperAPI.postman_collection.json`
2. Set base URL: `https://localhost:7000`
3. Test endpoints

---

## ? Verification Script

Run this to verify everything is set up correctly:

```bash
# Navigate to project
cd EtimadScraper

# Clean and rebuild
dotnet clean
dotnet build

# Run the app
dotnet run
```

Then in another terminal:
```bash
# Test the API
curl https://localhost:7000/api/TenderScraper/status
```

---

## ?? Still Not Working?

### Check These Files:

#### 1. Program.cs
Should have:
```csharp
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Later...
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
```

#### 2. TenderScraperController.cs
Should have:
```csharp
[ApiController]
[Route("api/[controller]")]
public class TenderScraperController : ControllerBase
{
    // ... endpoints
}
```

#### 3. EtimadScraper.csproj
Should have:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```
NOT `Microsoft.NET.Sdk`

---

## ?? Quick Fix Summary

1. ? Updated Program.cs to enable Swagger in all environments
2. ? Build successful
3. ? Run: `dotnet run`
4. ? Navigate to: `https://localhost:7000/swagger`

**The API should now be visible in Swagger!** ??

---

## ?? Additional Resources

- **Postman Collection**: `Postman/EtimadScraperAPI.postman_collection.json`
- **API Guide**: `POSTMAN_GUIDE.md`
- **Quick Start**: `API_QUICK_START.md`

If you still have issues, check the console output for errors when running `dotnet run`.
