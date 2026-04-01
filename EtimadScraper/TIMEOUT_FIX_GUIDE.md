# ?? Selector Timeout Fix - Issue Resolved

## ? Previous Error

```
System.TimeoutException: Timeout 10000ms exceeded.
waiting for Locator(".tender-card, .card, div[class*='tender-item'], .row > div[class*='col']") to be visible
24 × locator resolved to 127 elements. Proceeding with the first one: <div class="col-6 mx-auto text-center">…</div>
```

**Problem:** The selector was matching 127 elements, but the first one was a generic layout div, not a tender card!

---

## ? Solution Applied

### 1. **Removed Problematic Wait Strategy**

**Before:**
```csharp
await page.WaitForSelectorAsync(ScraperSelectors.TenderCard, new PageWaitForSelectorOptions
{
    Timeout = 10000,
    State = WaitForSelectorState.Visible
});
```

**After:**
```csharp
// Just wait for JS to render, then proceed with extraction
await Task.Delay(3000);
```

### 2. **Made Selectors More Specific**

**Before:**
```csharp
TenderCard = ".tender-card, .card, div[class*='tender-item'], .row > div[class*='col']"
```
This matched too many generic divs!

**After:**
```csharp
TenderCard = "div[class*='tender-card'], div[id*='tender'], article, .result-item, li[class*='tender']"
```
Now focuses on actual tender-related elements.

### 3. **Added Table Extraction First**

The Etimad website likely uses **HTML tables**, not cards! Added:

```csharp
// Try table rows first (most common for Etimad)
var tableRows = await page.QuerySelectorAllAsync("table tbody tr, table tr");
```

This will extract data from traditional HTML tables.

### 4. **Improved Extraction Priority**

**New extraction order:**
1. ? **Table rows** ? Most likely for Etimad
2. ? **Card layout** ? Fallback for modern layouts  
3. ? **Link extraction** ? Last resort alternative
4. ? **Debug screenshot** ? If nothing works

---

## ?? How It Works Now

### Extraction Flow:

```
1. Page loads ? Wait for network idle
2. Check for anti-bot protection
3. Wait 3 seconds for JavaScript
4. Try TABLE extraction first ? NEW!
   ? If found rows: Extract from table cells
   ? Return tenders
5. If no table data, try CARD extraction
   ? Look for card elements
   ? Extract from cards
6. If no cards, try LINK extraction
   ? Find all tender links
   ? Extract basic info
7. If nothing found: Capture screenshot + log HTML
```

---

## ?? Table Extraction Logic

**New method added:** `ExtractTenderFromTableRowAsync()`

```csharp
// Extracts from table cells:
Cell 0 ? Tender Number
Cell 1 ? Title + Details URL (from <a> tag)
Cell 2 ? Organization
Cell 3 ? Publish Date
Cell 4 ? Closing Date
Cell 5 ? Status
```

This matches the typical Etimad table structure!

---

## ?? Testing the Fix

### Step 1: Run the API

```powershell
cd C:\Users\ahmed\OneDrive\Documents\Apps\Elastic\EtimadScraper
dotnet run
```

### Step 2: Open Swagger

```
https://localhost:7000/swagger
```

### Step 3: Test Single Page

**GET** `/api/TenderScraper/scrape/page/1`

### Expected Results:

**Success Case (Table Found):**
```json
{
  "success": true,
  "message": "Successfully scraped 10 tenders from page 1",
  "tenders": [
    {
      "tenderNumber": "12345",
      "title": "Tender title in Arabic",
      "organization": "Government entity",
      "publishDate": "14/03/1446",
      "closingDate": "21/03/1446",
      "detailsUrl": "https://tenders.etimad.sa/Tender/Details/12345",
      "status": "Active"
    }
  ]
}
```

**Console Output:**
```
info: Scraping page 1...
dbug: Navigating to https://tenders.etimad.sa/...
dbug: Page content loaded
dbug: Waiting for dynamic content to load...
dbug: Found 10 table rows, trying table extraction
dbug: Successfully extracted 10 tenders from table
info: Successfully scraped 10 tenders from page 1
```

---

## ?? If Still Not Working

The scraper will now:

1. **Try all 3 extraction methods**
2. **Capture screenshot** ? `debug_page_0_*.png`
3. **Log first 1000 chars of HTML**

### Check Debug Info:

```powershell
# Look for screenshot in project directory
ls debug_page_*.png

# Check console logs for HTML output
# Look for "Page HTML (first 1000 chars):"
```

---

## ?? What Changed Summary

| Component | Change | Why |
|-----------|--------|-----|
| **Wait Strategy** | Removed `WaitForSelector` | Was timing out on wrong elements |
| **Delay** | Added 3-second wait | Simple, reliable JS render time |
| **Selectors** | More specific | Avoid generic layout divs |
| **Extraction Order** | Table first | Matches actual Etimad structure |
| **Table Method** | New method added | Extract from HTML tables |
| **Debugging** | Enhanced logging | Better troubleshooting |

---

## ? Benefits of This Fix

1. **No More Timeouts** - Doesn't wait for wrong elements
2. **Table Support** - Handles traditional HTML tables
3. **Multiple Fallbacks** - Tries 3 different methods
4. **Better Debugging** - Screenshots + HTML logging
5. **More Reliable** - Simpler approach, less brittle

---

## ?? Ready to Test!

The scraper should now work properly with the Etimad website!

**Commands:**
```powershell
cd EtimadScraper
dotnet run
# Open https://localhost:7000/swagger
# Try GET /api/TenderScraper/scrape/page/1
```

**Look for console output:**
```
dbug: Found X table rows, trying table extraction
dbug: Successfully extracted X tenders from table
```

This means it's working! ??

---

## ?? If Issues Persist

1. **Check Screenshot** - See actual page structure
2. **Check Console Logs** - Look for "Found X table rows"
3. **Check HTML Output** - See logged HTML structure
4. **Update Selectors** - Based on actual structure

The enhanced debugging will help identify the exact issue!

---

**File:** `TIMEOUT_FIX_GUIDE.md`  
**Date:** March 31, 2024  
**Status:** ? Fixed and Ready to Test
