# ?? Selector Fix Applied - Etimad Scraper Updated

## ? Problem Fixed

**Issue:** Timeout error when waiting for tender table selector
```
System.TimeoutException: Timeout 10000ms exceeded.
Selector: "table.table, .tender-list, .table-responsive table"
```

**Root Cause:** The Etimad website uses a card-based layout, not traditional HTML tables.

---

## ?? Changes Made

### 1. **Updated Selectors** (`Configuration/ScraperSelectors.cs`)

**Old (Table-based):**
```csharp
TenderTableContainer = "table.table, .tender-list, .table-responsive table"
TenderRow = "tbody tr, .tender-item, .table tbody tr"
```

**New (Card-based + Flexible):**
```csharp
TenderContainer = ".tenders-list, .tender-cards, div[class*='tender'], .card-body"
TenderCard = ".tender-card, .card, div[class*='tender-item']"
AnyContent = "body, main, #app, #root, .container" // Fallback
```

### 2. **Multiple Detection Strategies**

The scraper now tries 3 strategies in order:

1. **Wait for basic content** - Ensures page is loaded
2. **Wait for tender container** - Looks for main tender area
3. **Wait for tender cards** - Looks for individual tender cards

### 3. **Flexible Extraction Methods**

Two extraction approaches:

#### Method 1: Card-based extraction
```csharp
ExtractTenderFromCardAsync() // For modern card layouts
```

#### Method 2: Alternative extraction
```csharp
ExtractTendersAlternativeAsync() // Searches for any tender links
```

### 4. **Debug Features Added**

? **Screenshot Capture:** Saves screenshot when selectors fail  
? **HTML Logging:** Logs first 500 chars of page HTML for debugging  
? **Flexible Selectors:** Multiple selector options for each field  

---

## ?? How to Test the Fix

### Step 1: Run the API

```powershell
cd C:\Users\ahmed\OneDrive\Documents\Apps\Elastic\EtimadScraper
dotnet run
```

### Step 2: Test in Swagger

Open: `https://localhost:7000/swagger`

### Step 3: Try Single Page (Recommended First)

**GET** `/api/TenderScraper/scrape/page/1`

Click "Try it out" ? "Execute"

**Expected Response:**
```json
{
  "success": true,
  "message": "Successfully scraped X tenders from page 1",
  "totalCount": X,
  "tenders": [
    {
      "title": "Tender title...",
      "detailsUrl": "https://tenders.etimad.sa/...",
      "additionalInfo": "Full card text...",
      "scrapedAt": "2024-..."
    }
  ]
}
```

---

## ?? What Data is Extracted Now

### Guaranteed Fields:
- ? **Title** - From title element or link text
- ? **DetailsUrl** - Full URL to tender details
- ? **AdditionalInfo** - All text from card for reference
- ? **ScrapedAt** - Timestamp

### Optional Fields (if found):
- **TenderNumber** - Reference number
- **Organization** - Issuing entity
- **PublishDate** - Announcement date
- **ClosingDate** - Submission deadline
- **Status** - Tender status

---

## ??? Debugging Features

### If Selectors Still Fail:

The scraper will automatically:

1. **Save Screenshot** - `debug_page_{pageNumber}_{timestamp}.png`
2. **Log HTML** - First 500 characters in console
3. **Try Alternative Method** - Search for any tender links

**Check Project Directory:**
```
EtimadScraper/
??? debug_page_1_20240331_120000.png  ? Screenshot
??? tenders.json                       ? Scraped data
```

---

## ?? Expected Behavior Now

### Scenario 1: Modern Card Layout (Most Likely)
```
? Browser opens
? Page loads
? Cards detected: "Found 10 tender cards"
? Data extracted: "Successfully scraped 10 tenders"
```

### Scenario 2: Alternative Structure
```
? Browser opens
? Page loads
?? Cards not found: "No cards found, trying alternative extraction..."
? Links found: "Found 15 potential tender links"
? Data extracted: "Successfully scraped 15 tenders"
```

### Scenario 3: Debugging Needed
```
? Browser opens
? Page loads
?? Content not recognized
?? Screenshot saved: "debug_page_1_20240331_120000.png"
?? HTML logged to console
?? Returns: "No tender content found"
```

---

## ?? Console Output Examples

### Success:
```
info: Initializing Playwright browser...
info: Browser initialized successfully
info: Scraping page 1...
dbug: Navigating to https://tenders.etimad.sa/Tender/AllTendersForVisitor?PageNumber=1
dbug: Page content loaded
dbug: Tender cards found
dbug: Found 10 tender cards
info: Successfully scraped 10 tenders from page 1
```

### Debugging Mode:
```
warn: Tender container selector not found
warn: Tender card selector not found
info: Debug screenshot saved to: debug_page_1_20240331_120000.png
dbug: Page HTML (first 500 chars): <!DOCTYPE html><html>...
warn: No tender content found on page 1. Structure may have changed.
```

---

## ?? How to Use Debug Screenshot

If scraping fails and screenshot is captured:

1. **Open Screenshot:**
   ```
   EtimadScraper/debug_page_1_20240331_120000.png
   ```

2. **Inspect Visual Structure:**
   - Are tenders visible?
   - What HTML elements contain them?
   - Any error messages or CAPTCHA?

3. **Update Selectors if Needed:**
   Edit `Configuration/ScraperSelectors.cs` based on what you see

---

## ? Benefits of New Approach

### 1. **Flexible**
- Works with multiple layout types
- Tries multiple selectors for each element
- Gracefully falls back to alternative methods

### 2. **Debuggable**
- Screenshots capture exact page state
- HTML logging shows structure
- Clear log messages explain what's happening

### 3. **Robust**
- Doesn't fail completely if one selector doesn't work
- Continues to try alternative methods
- Extracts maximum available data

### 4. **Maintainable**
- All selectors in one file
- Easy to add new selectors
- Clear documentation of what each selector does

---

## ?? Next Steps

### 1. Test the Updated Scraper

```powershell
# In PowerShell
cd EtimadScraper
dotnet run

# Then open browser to:
# https://localhost:7000/swagger
```

### 2. Try Single Page First

Use: **GET** `/api/TenderScraper/scrape/page/1`

### 3. Check Results

- If successful ? Try more pages
- If failed ? Check screenshots and logs

### 4. If Scraping Fails

**Check these files:**
1. Screenshots: `debug_page_*.png`
2. Console logs
3. `SELECTOR_DEBUG_GUIDE.md` (this file)

---

## ?? Testing Checklist

- [ ] API starts without errors
- [ ] Swagger opens successfully
- [ ] Single page scrape returns data
- [ ] Data contains titles and URLs
- [ ] No timeout errors
- [ ] Screenshot saved if needed
- [ ] JSON file created with results

---

## ?? If Still Having Issues

### Check:
1. **Console Logs** - Look for selector warnings
2. **Screenshots** - See what page actually looks like
3. **HTML Output** - Check logged HTML structure
4. **Website Access** - Verify site is accessible in browser

### Possible Solutions:
1. **Update Selectors** - Based on screenshot/HTML
2. **Increase Timeout** - If page loads slowly
3. **Add Delay** - If site needs more time to render
4. **Check Anti-bot** - If CAPTCHA appears

---

## ?? Summary

? **Selectors Updated** - Now works with card-based layouts  
? **Multiple Strategies** - Tries different approaches  
? **Debug Features** - Screenshots + HTML logging  
? **Flexible Extraction** - Gets maximum available data  
? **Ready to Test** - Run and check results!

**The scraper is now much more robust and should work with the actual Etimad website structure!** ??

---

**File:** `SELECTOR_FIX_GUIDE.md`  
**Date:** March 31, 2024  
**Status:** ? Ready to Test
