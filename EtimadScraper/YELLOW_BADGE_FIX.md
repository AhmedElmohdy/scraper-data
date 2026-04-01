# ?? Fix: Yellow Badge Extraction (Category/Department Fields)

## ? Problem

The `category` and `department` fields were returning **empty** even though the tender cards display a **yellow-highlighted badge** containing department information like:

- "?????? ???? ????? ??????? - ????? ?????????"
- "???? ??????? ?????????? - ??????? ??????? - ?????????"

**Root Cause:** The extraction code was looking specifically for **blue badges** (`#0d6efd`, `rgb(13, 110, 253)`) but the actual badges are **yellow/warning colored**.

---

## ? Solution Applied

### **1. Updated Selectors to be Color-Agnostic**

**File:** `Configuration/ScraperSelectors.cs`

**Before:**
```csharp
// Only looked for blue badges
public const string Category = ".badge-primary, .bg-primary, ...";
```

**After:**
```csharp
// Looks for badges of ANY color
public const string Category = ".badge, [class*='badge'], .bg-primary, .bg-warning, .bg-info, .bg-success, span[class*='bg-'], div[class*='bg-'], [style*='background-color'], [style*='background:'], [class*='highlight'], [class*='category'], [class*='department']";
```

### **2. Enhanced Extraction Logic with 4 Fallback Strategies**

**File:** `Services/EtimadScraperService.cs`

The `ExtractCategoryDepartmentAsync()` method now uses **4 intelligent strategies**:

#### **Strategy 1: CSS Selector Match**
```csharp
// Try the provided selector first (fastest)
var element = await parent.QuerySelectorAsync(selector);
```

#### **Strategy 2: Badge Element Search**
```csharp
// Look for ANY badge element (.badge, [class*='badge'], etc.)
var badges = await parent.QuerySelectorAllAsync(".badge, [class*='badge'], span[class*='bg-'], div[class*='bg-']");

// Filter for Arabic text longer than 10 characters
if (trimmedText.Length > 10 && 
    System.Text.RegularExpressions.Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]"))
{
    return trimmedText; // ? Found it!
}
```

#### **Strategy 3: Background Color Detection**
```csharp
// Look for ANY element with background color (yellow, blue, green, etc.)
var styledElements = await parent.QuerySelectorAllAsync("[style*='background']");

// Check if it has ANY background-color style
if (style.Contains("background-color") || 
    style.Contains("background:") ||
    style.Contains("background "))
{
    // Extract Arabic text longer than 10 chars
    if (trimmedText.Length > 10 && 
        Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]"))
    {
        return trimmedText; // ? Found it!
    }
}
```

#### **Strategy 4: Keyword-Based Search**
```csharp
// Look for span/div elements containing department keywords
var spans = await parent.QuerySelectorAllAsync("span, div");

// Check for common Arabic keywords:
if (trimmedText.Contains("?????") ||      // Administration
    trimmedText.Contains("??????") ||     // Hospital
    trimmedText.Contains("????") ||       // Authority
    trimmedText.Contains("?????????") ||  // Purchases
    trimmedText.Contains("???????"))      // Financial
{
    // Must have a class or style (not just plain text)
    var className = await span.GetAttributeAsync("class");
    var spanStyle = await span.GetAttributeAsync("style");
    
    if (!string.IsNullOrWhiteSpace(className) || !string.IsNullOrWhiteSpace(spanStyle))
    {
        return trimmedText; // ? Found it!
    }
}
```

### **3. Smart Filtering Logic**

The extraction method includes several filters to ensure quality:

```csharp
// ? Must contain Arabic characters
System.Text.RegularExpressions.Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]")

// ? Must be reasonably long (10+ characters)
trimmedText.Length > 10

// ? Must not be too long (< 200 characters to avoid capturing entire card)
trimmedText.Length < 200

// ? Must have styling or special class (not just plain text)
!string.IsNullOrWhiteSpace(className) || !string.IsNullOrWhiteSpace(spanStyle)
```

---

## ?? Expected Results

### **Before Fix:**
```json
{
  "tenderNumber": "260339010723",
  "title": "????? ?????",
  "category": "",     // ? Empty
  "department": "",   // ? Empty
  "publishDate": "31-03-2026"
}
```

### **After Fix:**
```json
{
  "tenderNumber": "260339010723",
  "title": "????? ?????",
  "category": "?????? ???? ????? ??????? - ????? ?????????",     // ? Populated
  "department": "?????? ???? ????? ??????? - ????? ?????????",   // ? Populated
  "publishDate": "31-03-2026"
}
```

---

## ?? Testing Steps

### **Step 1: Run the Application**
```powershell
cd EtimadScraper
dotnet run
```

### **Step 2: Test the API**
```bash
GET http://localhost:5000/api/TenderScraper/scrape/page/1
```

### **Step 3: Check Debug Logs**

Look for these log messages indicating which strategy worked:

```
dbug: Found category/department with primary selector: ?????? ???? ????? ??????? - ????? ?????????
```

Or:

```
dbug: Found category/department in badge: ???? ??????? ?????????? - ??????? ???????
```

Or:

```
dbug: Found category/department in styled element: ?????? ????? - ?????? ???????
```

Or:

```
dbug: Found category/department by keyword match: ???? ??????? - ?????????
```

### **Step 4: Verify Response**

Check that both `category` and `department` fields are populated:

```json
{
  "success": true,
  "message": "Successfully scraped 6 tenders from page 1",
  "tenders": [
    {
      "tenderNumber": "260339010723",
      "title": "????? ?????",
      "category": "?????? ???? ????? ??????? - ????? ?????????",    // ?
      "department": "?????? ???? ????? ??????? - ????? ?????????",  // ?
      "publishDate": "31-03-2026",
      "closingDate": "2026-04-06"
    }
  ]
}
```

---

## ?? Why This Fix Works

### **Problem 1: Color Assumption**
- **Before:** Code looked for **blue** badges only
- **After:** Looks for badges of **any color** (yellow, blue, green, etc.)

### **Problem 2: Rigid Selectors**
- **Before:** Used specific Bootstrap color classes
- **After:** Uses generic selectors + fallback strategies

### **Problem 3: No Keyword Matching**
- **Before:** Only relied on CSS classes
- **After:** Also searches for common Arabic keywords like "?????", "??????", "?????????"

### **Problem 4: Length Filtering**
- **Before:** Accepted any text length (could capture garbage)
- **After:** Requires 10+ characters, < 200 characters (reasonable department name length)

---

## ?? Debug Features

### **1. Strategy Logging**

Each strategy logs when it finds a match:

```csharp
_logger.LogDebug("Found category/department with primary selector: {Text}", trimmedText);
_logger.LogDebug("Found category/department in badge: {Text}", trimmedText);
_logger.LogDebug("Found category/department in styled element: {Text}", trimmedText);
_logger.LogDebug("Found category/department by keyword match: {Text}", trimmedText);
```

### **2. Failure Logging**

If no strategy works:

```csharp
_logger.LogDebug("No category/department found with selector: {Selector}", selector);
```

### **3. Error Logging**

If extraction throws an exception:

```csharp
_logger.LogDebug(ex, "Failed to extract category/department with selector: {Selector}", selector);
```

---

## ?? Files Modified

| File | Changes |
|------|---------|
| `Configuration/ScraperSelectors.cs` | ? Updated `Category` and `Department` selectors to include all badge colors |
| `Services/EtimadScraperService.cs` | ? Enhanced `ExtractCategoryDepartmentAsync()` with 4 fallback strategies<br>? Added keyword-based search<br>? Added length filtering<br>? Added Arabic text validation |

---

## ?? Troubleshooting

### **If Still Returning Empty**

1. **Check Debug Logs**
   ```
   dbug: No category/department found with selector: ...
   ```
   This means all 4 strategies failed.

2. **Inspect Page HTML**
   - Open browser DevTools on Etimad page
   - Find the yellow badge element
   - Note its **class names**, **styles**, and **HTML structure**

3. **Example HTML**
   ```html
   <!-- If the badge looks like this: -->
   <span class="custom-badge" style="background-color: #ffc107">
     ?????? ???? ????? ??????? - ????? ?????????
   </span>
   ```

4. **Update Selectors** (if needed)
   Add the custom class to `ScraperSelectors.cs`:
   ```csharp
   public const string Category = "..., .custom-badge, ...";
   ```

### **If Wrong Text is Extracted**

The length filter might be too lenient. Adjust in `ExtractCategoryDepartmentAsync()`:

```csharp
// Make it stricter
if (trimmedText.Length > 15 &&  // Increase minimum
    trimmedText.Length < 150)   // Decrease maximum
```

---

## ? Verification Checklist

- [x] Build successful
- [ ] API runs without errors
- [ ] Debug logs show "Found category/department in..."
- [ ] `category` field populated in response
- [ ] `department` field populated in response
- [ ] Multiple pages tested successfully

---

## ?? Next Steps

1. **Test the updated scraper**
2. **Check console logs** to see which extraction strategy works
3. **Verify JSON response** contains populated `category` and `department` fields
4. **Share results** if still having issues

---

## ?? Technical Notes

### **Arabic Text Regex**
```csharp
@"[\u0600-\u06FF]"
```
This matches Arabic Unicode characters (U+0600 to U+06FF).

### **Background Color Detection**
```csharp
style.Contains("background-color") || 
style.Contains("background:") ||
style.Contains("background ")
```
This matches various CSS background syntax styles.

### **Common Arabic Keywords**
```csharp
"?????"      // Administration
"??????"     // Hospital
"????"       // Authority
"?????????"  // Purchases
"???????"    // Financial
```
These are common words in department names on Etimad.

---

**File:** `YELLOW_BADGE_FIX.md`  
**Date:** January 2025  
**Status:** ? Fix Applied - Ready for Testing
