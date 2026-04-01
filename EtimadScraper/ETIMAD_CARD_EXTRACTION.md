# ?? Fix: Etimad Card-Based Layout Not Being Scraped

## ? Problem

Based on your screenshot, the Etimad website uses a **card-based layout**, not HTML tables. The data structure looks like:

```
???????????????????????????????????????
? ?? 10                                ?
? ????? ?????: 31-03-2026              ?
?                                     ?
? ???? ?????                          ?
?                                     ?
? BP Apparatus                        ?
? ????????                            ?
?                                     ?
? ?????? ????? - ?????? ???????      ?
? ?????? ???????                      ?
? 10 ??? 19 ????                      ?
???????????????????????????????????????
```

This is NOT a table - it's divs/cards with text content.

---

## ? Solution Applied

### **1. Card Detection Strategy**

Updated extraction to look for cards with these characteristics:
- Contains Arabic text (`[\u0600-\u06FF]`)
- Has links or tender-related keywords ("????????", "??????", "?????")
- Uses div elements with classes like `col-12`, `col-md-6`, `col-lg-4`

### **2. Etimad-Specific Extraction**

New method: `ExtractTenderFromEtimadCardAsync()`

**Extracts:**
- **Title**: From `h1`-`h6` tags or first `<a>` link
- **Organization**: Text containing "BP Apparatus", "??????", "?????"
- **Tender Number**: Numeric patterns (6+ digits)
- **Dates**: Format matching `DD-MM-YYYY`
- **Status/Activity**: Text after "?????? ???????"
- **Additional Info**: Category like "???? ????? - ?????? ???????"

### **3. Smart Text Parsing**

The code now:
1. Splits card text into lines
2. Looks for Arabic keywords to identify fields
3. Extracts data by context, not position
4. Stores full card text in `AdditionalInfo` for reference

---

## ?? Expected Results

Based on your screenshot, a card like this:

```
???? ?????
????? ?????: 31-03-2026

BP Apparatus

????????
?????? ????? - ?????? ???????

?????? ???????
10 ??? 19 ????
```

Should extract to:

```json
{
  "tenderNumber": "[extracted if present]",
  "title": "???? ?????" or "BP Apparatus",
  "organization": "?????? ????? - ?????? ???????",
  "publishDate": "31-03-2026",
  "status": "?????? ???????",
  "additionalInfo": "???? ????? - ?????? ???????",
  "detailsUrl": "[extracted from link]"
}
```

---

## ?? Testing

### **Step 1: Run with Debug Logging**

```powershell
dotnet run
```

### **Step 2: Test Scraping**

```
GET /api/TenderScraper/scrape/page/1
```

### **Step 3: Check Console Output**

Look for these messages:
```
dbug: Found 20 potential cards on page
dbug: Processing card with text: ???? ?????...
dbug: Found title: ???? ?????
dbug: Found organization: BP Apparatus
dbug: Found date: 31-03-2026
dbug: Found tender number: 12345678
dbug: Extracted tender: ???? ?????...
dbug: Card extraction complete - found 10 tenders
```

---

## ?? What Changed

### **Before:**
```csharp
// Assumed table structure
var tableRows = await page.QuerySelectorAllAsync("table tbody tr");
// ? No tables on Etimad!
```

### **After:**
```csharp
// Looks for card containers
var allCards = await page.QuerySelectorAllAsync(
    "div.col-12, div.col-md-6, div.col-lg-4, article, .card"
);

// Checks if card has tender content
var hasArabicContent = Regex.IsMatch(cardText, @"[\u0600-\u06FF]");
var hasTenderInfo = cardText.Contains("????????");

// Extracts from card structure
var tender = await ExtractTenderFromEtimadCardAsync(card);
```

---

## ?? Extraction Logic

### **Title Extraction:**
```csharp
// Looks for headings or links
var titleElements = await card.QuerySelectorAllAsync("h1, h2, h3, h4, h5, h6, a");
tender.Title = await titleElements[0].TextContentAsync();
```

### **Organization Extraction:**
```csharp
// Searches for patterns
if (line.Contains("BP Apparatus") || 
    line.Contains("??????") || 
    line.Contains("?????"))
{
    tender.Organization = line;
}
```

### **Date Extraction:**
```csharp
// Regex pattern for dates
if (Regex.IsMatch(line, @"\d{1,2}-\d{1,2}-\d{4}"))
{
    tender.PublishDate = line;
}
```

### **Number Extraction:**
```csharp
// Finds 6+ digit numbers
var numberMatches = Regex.Matches(allText, @"\b\d{6,}\b");
tender.TenderNumber = numberMatches[0].Value;
```

---

## ?? Debug Features

### **1. Card Content Logging**
```
dbug: Processing card with text: ???? ????? BP Apparatus ????????...
```

### **2. Field Discovery Logging**
```
dbug: Found title: ???? ?????
dbug: Found organization: BP Apparatus
dbug: Found date: 31-03-2026
dbug: Found tender number: 12345678
```

### **3. Screenshot on Failure**
If no data found, screenshot saved:
```
debug_page_0_20240331_120000.png
```

### **4. HTML Output**
First 2000 chars of HTML logged for inspection.

---

## ? Verification

After running, check that extracted data includes:

```json
{
  "tenders": [
    {
      "title": "???? ?????",  // ? Found
      "organization": "BP Apparatus", // ? Found
      "publishDate": "31-03-2026", // ? Found
      "additionalInfo": "?????? ????? - ?????? ???????", // ? Found
      "detailsUrl": "https://tenders.etimad.sa/..." // ? Found
    }
  ]
}
```

---

## ?? Expected Behavior

### **Success Case:**
```
dbug: Found 15 potential cards on page
dbug: Card extraction complete - found 10 tenders
info: Successfully scraped 10 tenders from page 1
```

### **If Still No Data:**
```
warn: No tenders extracted with any method, capturing debug info
info: Debug screenshot saved to: debug_page_0_20240331_120000.png
dbug: Page HTML (first 2000 chars): <html>...
```

Check the screenshot to see what the page actually looks like.

---

## ?? Key Improvements

1. ? **Card-based extraction** - Works with div layout, not tables
2. ? **Arabic text detection** - Identifies Arabic content
3. ? **Context-aware parsing** - Finds fields by keywords, not position
4. ? **Flexible structure** - Works with various card layouts
5. ? **Better logging** - See exactly what's being extracted
6. ? **Full text capture** - Stores all card text in `additionalInfo`

---

## ?? Next Steps

1. **Run the API:** `dotnet run`
2. **Test scraping:** `GET /api/TenderScraper/scrape/page/1`
3. **Check console:** Look for "Found X tenders" message
4. **Verify data:** Check that title, organization, dates are populated
5. **Check screenshot:** If no data, look at `debug_page_*.png`

---

## ?? If Still Not Working

**Share this info:**

1. **Console output** showing:
   - "Found X potential cards on page"
   - "Processing card with text: ..."
   - What was found/not found

2. **Screenshot** from debug output

3. **Sample response** showing what data is returned

This will help fine-tune the extraction for your specific page structure!

---

**File:** `ETIMAD_CARD_EXTRACTION.md`  
**Date:** March 31, 2024  
**Status:** ? Updated for card-based layout
