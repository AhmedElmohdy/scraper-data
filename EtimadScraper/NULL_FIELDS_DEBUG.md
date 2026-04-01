# ?? Debugging Null TenderNumber and Organization Fields

## ? Problem

Fields like `tenderNumber` and `organization` are returning null when scraping.

**Why:** The extraction code was assuming a fixed table column order, but the actual website structure is different.

---

## ? Solution Applied

### **1. Smart Pattern-Based Extraction**

Instead of assuming column positions, the code now:
- **Identifies tender numbers** by pattern (numeric or alphanumeric)
- **Finds organization names** by characteristics (Arabic text, multiple words, length)
- **Detects dates** by format (DD/MM/YYYY or DD-MM-YYYY)
- **Locates titles** by finding links in cells

### **2. Enhanced Debug Logging**

Added detailed logging to see exactly what's in each table cell:
```
Cell[0]: 12345678
Cell[1]: ????? ????? ????...
Cell[2]: ????? ?????...
Cell[3]: 14/03/1446
```

### **3. Fallback Mechanism**

If smart extraction doesn't work, falls back to position-based extraction.

---

## ?? How to Debug

### **Step 1: Enable Debug Logging**

Already done in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "EtimadScraper.Services": "Debug"
    }
  }
}
```

### **Step 2: Run a Test Scrape**

```powershell
# Local testing
dotnet run

# Then in Swagger or Postman:
GET /api/TenderScraper/scrape/page/1
```

### **Step 3: Check Console Logs**

Look for these debug messages:
```
dbug: Processing table row with 7 cells
dbug: Cell[0]: 12345678
dbug: Cell[1]: ????? ????? ???? ??????????
dbug: Cell[2]: ????? ?????
dbug: Cell[3]: 14/03/1446
dbug: Cell[4]: 21/03/1446
dbug: Found tender number in cell[0]: 12345678
dbug: Found title in cell[1]: ????? ????? ???? ??????????
dbug: Found organization in cell[2]: ????? ?????
dbug: Extracted tender - Number: 12345678, Title: ?????..., Org: ?????...
```

---

## ?? What the New Code Does

### **Pattern Detection**

1. **Tender Number Detection:**
```csharp
// Looks for numeric or alphanumeric patterns in first 3 columns
Regex.IsMatch(cellText, @"^\d+") ||  // Starts with digits
Regex.IsMatch(cellText, @"^[A-Z0-9\-/]+") // Alphanumeric with dashes/slashes
```

2. **Organization Detection:**
```csharp
// Looks for Arabic text or multi-word names in columns 2-4
Regex.IsMatch(cellText, @"[\u0600-\u06FF]") ||  // Contains Arabic
cellText.Split(' ').Length >= 2  // Has multiple words
```

3. **Date Detection:**
```csharp
// Looks for date patterns
Regex.IsMatch(cellText, @"\d{1,2}[/-]\d{1,2}[/-]\d{4}")
```

4. **Title Detection:**
```csharp
// Looks for cells containing links
var linkInCell = await cells[i].QuerySelectorAsync("a");
```

---

## ?? If Still Getting Nulls

### **Check Console Logs**

Look for the cell content logs. They'll show you exactly what's in each column:

```
dbug: Cell[0]: [content here]
dbug: Cell[1]: [content here]
dbug: Cell[2]: [content here]
```

### **Understand the Structure**

Based on the logs, you can see which column has which data:
- If tender number is in Cell[1] instead of Cell[0], the smart detection should find it
- If organization is in Cell[3], the pattern matching will catch it
- Fallback extraction will use positional data as last resort

---

## ?? Expected Behavior

### **Scenario 1: Standard Table**
```
Column 0: Tender Number ? "12345678"
Column 1: Title (with link) ? "Project name"
Column 2: Organization ? "Ministry of Health"
Column 3: Publish Date ? "14/03/1446"
Column 4: Closing Date ? "21/03/1446"
```

**Result:** All fields extracted correctly ?

### **Scenario 2: Different Order**
```
Column 0: Status ? "Active"
Column 1: Tender Number ? "12345678"
Column 2: Title (with link) ? "Project name"
Column 3: Organization ? "Ministry"
```

**Result:** Smart detection finds correct fields ?

### **Scenario 3: Arabic Content**
```
Column 0: ??? ???????? ? "12345678"
Column 1: ??? ??????? ? "????? ?????"
Column 2: ????? ???????? ? "????? ?????"
```

**Result:** Pattern matching identifies organization by Arabic text ?

---

## ?? Sample Debug Output

### **Good Extraction:**
```
dbug: Processing table row with 6 cells
dbug: Cell[0]: 12345678
dbug: Cell[1]: ????? ????? ???? ??????????
dbug: Cell[2]: ????? ?????
dbug: Cell[3]: 14/03/1446
dbug: Cell[4]: 21/03/1446
dbug: Cell[5]: ???
dbug: Found tender number in cell[0]: 12345678
dbug: Found title in cell[1]: ????? ????? ???? ??????????
dbug: Found organization in cell[2]: ????? ?????
dbug: Found publish date in cell[3]: 14/03/1446
dbug: Found closing date in cell[4]: 21/03/1446
dbug: Extracted tender - Number: 12345678, Title: ?????..., Org: ?????...
```

### **Fallback Extraction:**
```
dbug: Processing table row with 4 cells
dbug: Cell[0]: ABC-123
dbug: Cell[1]: Some project
dbug: Cell[2]: Some organization
dbug: Cell[3]: Active
dbug: Found tender number in cell[0]: ABC-123
dbug: Found title in cell[1]: Some project
dbug: Fallback: Using cell[2] as organization: Some organization
dbug: Extracted tender - Number: ABC-123, Title: Some project, Org: Some organization
```

---

## ?? Next Steps

### **1. Test with Debug Logging**

Run your API and watch the console output:
```powershell
dotnet run
```

### **2. Check What's Being Extracted**

Look at the `dbug:` messages to see:
- How many cells each row has
- What's in each cell
- Which fields are being found
- Which are using fallback

### **3. Adjust if Needed**

If the patterns don't match your specific case:
- Share the debug output showing cell contents
- We can adjust the pattern matching logic
- Or create custom selectors for specific columns

---

## ?? Key Improvements

1. ? **Smart pattern detection** - Finds data regardless of column order
2. ? **Arabic text support** - Identifies organization names in Arabic
3. ? **Date recognition** - Automatically finds date fields
4. ? **Link detection** - Finds title by looking for links
5. ? **Detailed logging** - See exactly what's in each cell
6. ? **Fallback mechanism** - Uses position if patterns don't match

---

## ?? Testing Checklist

- [ ] Run API with debug logging enabled
- [ ] Check console for `Cell[X]:` messages
- [ ] Verify extracted tender numbers are not null
- [ ] Verify organizations are not null
- [ ] Check if titles have URLs
- [ ] Verify dates are in correct format

---

## ?? Still Having Issues?

**Share this information:**

1. Console log output showing the `Cell[X]:` messages
2. What you expect vs what you're getting
3. Sample of the actual response

**Example:**
```
Expected:
- tenderNumber: "12345678"
- organization: "????? ?????"

Actual:
- tenderNumber: null
- organization: null

Console shows:
- Cell[0]: [actual content]
- Cell[1]: [actual content]
- Cell[2]: [actual content]
```

With this info, we can fine-tune the extraction logic!

---

**File:** `NULL_FIELDS_DEBUG.md`  
**Date:** March 31, 2024  
**Status:** ? Enhanced extraction with debug logging
