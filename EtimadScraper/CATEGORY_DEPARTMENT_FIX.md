# ?? Fix: Category/Department Field Extraction

## ? Problem

The scraper was returning **empty values** for `category` and `department` fields:

```json
{
  "category": "",
  "department": "",
  ...
}
```

This is because the blue badge containing department information (e.g., "?????? ???? ????? ??????? - ????? ?????????") was not being extracted properly.

---

## ? Solution Applied

### **1. Added New Properties to TenderDto**

Updated `Models/TenderDto.cs`:

```csharp
/// <summary>
/// Department or sub-category badge shown in blue
/// Example: "?????? ???? ????? ??????? - ????? ?????????" 
/// or "???? ??????? ?????????? - ??????? ??????? - ?????????"
/// </summary>
public string Department { get; set; } = string.Empty;
```

### **2. Enhanced Selectors**

Updated `Configuration/ScraperSelectors.cs` with multiple selector strategies:

```csharp
/// <summary>
/// Selector for category/department information (blue badge)
/// The blue badge typically uses Bootstrap classes or custom styling
/// </summary>
public const string Category = ".badge-primary, .bg-primary, .badge.text-bg-primary, span[class*='primary'], div[class*='highlight'], [style*='background-color: #0d6efd'], [style*='background-color: rgb(13, 110, 253)'], [class*='category']";

/// <summary>
/// Selector for department/sub-category (blue highlighted section)
/// </summary>
public const string Department = ".badge-primary, .bg-primary, .badge.text-bg-primary, span[class*='department'], [class*='dept'], [class*='sub-category'], .badge-info, .bg-info";
```

### **3. Smart Extraction Method**

Added new `ExtractCategoryDepartmentAsync()` method with **3 fallback strategies**:

#### **Strategy 1: CSS Selector Match**
Tries the provided CSS selector first (fastest)

#### **Strategy 2: Badge Element Search**
```csharp
var badges = await parent.QuerySelectorAllAsync(".badge, [class*='badge'], span[class*='bg-']");
// Looks for badges with Arabic text longer than 5 characters
```

#### **Strategy 3: Inline Style Detection**
```csharp
// Finds elements with blue background using inline styles
if (style.Contains("#0d6efd") || style.Contains("rgb(13, 110, 253)") || ...)
```

### **4. Automatic Fallback Logic**

```csharp
// If Category is empty but Department has value, copy it
if (string.IsNullOrEmpty(tender.Category) && !string.IsNullOrEmpty(tender.Department))
{
    tender.Category = tender.Department;
}
// Vice versa
else if (string.IsNullOrEmpty(tender.Department) && !string.IsNullOrEmpty(tender.Category))
{
    tender.Department = tender.Category;
}
```

This ensures that if either field is found, both are populated (since they often refer to the same blue badge).

---

## ?? Expected Results

### **Before Fix:**
```json
{
  "tenderNumber": "260339010723",
  "title": "????? ?????",
  "organization": "",
  "category": "",  // ? Empty
  "department": "", // ? Empty
  ...
}
```

### **After Fix:**
```json
{
  "tenderNumber": "260339010723",
  "title": "????? ?????",
  "organization": "",
  "category": "?????? ???? ????? ??????? - ????? ?????????",  // ? Populated
  "department": "?????? ???? ????? ??????? - ????? ?????????", // ? Populated
  ...
}
```

---

## ?? Testing

### **Step 1: Rebuild the Project**
```powershell
dotnet build
```

### **Step 2: Run the API**
```powershell
dotnet run
```

### **Step 3: Test Scraping**
```
GET http://localhost:5000/api/TenderScraper/scrape/page/1
```

### **Step 4: Check Results**

Look for populated `category` and `department` fields in the response:

```json
{
  "success": true,
  "message": "Successfully scraped 6 tenders from page 1",
  "tenders": [
    {
      "category": "?????? ???? ????? ??????? - ????? ?????????",
      "department": "?????? ???? ????? ??????? - ????? ?????????"
    }
  ]
}
```

---

## ?? Debug Logging

The enhanced extraction method includes debug logging:

```
dbug: Found category/department with primary selector: ?????? ???? ????? ??????? - ????? ?????????
```

Or if fallback strategies are used:

```
dbug: Found category/department in badge: ???? ??????? ?????????? - ??????? ???????
dbug: Found category/department in styled element: ?????????
```

If nothing is found:

```
dbug: No category/department found with selector: .badge-primary, .bg-primary...
```

---

## ?? Extraction Strategies Explained

### **Why Multiple Strategies?**

Different pages may use different HTML structures:

1. **Bootstrap Classes**: `.badge-primary`, `.bg-primary`
2. **Custom Classes**: `[class*='category']`, `[class*='department']`
3. **Inline Styles**: `style="background-color: #0d6efd"`
4. **Generic Badges**: `.badge`, `span[class*='bg-']`

The scraper tries all of these to ensure maximum compatibility.

### **Arabic Text Validation**

```csharp
System.Text.RegularExpressions.Regex.IsMatch(trimmedText, @"[\u0600-\u06FF]")
```

This ensures we only capture elements with Arabic text (the department names), not random badge elements with icons or numbers.

### **Length Validation**

```csharp
trimmedText.Length > 5
```

Filters out single-character or very short badge content that isn't the department name.

---

## ?? Files Changed

| File | Changes |
|------|---------|
| `Models/TenderDto.cs` | ? Added `Department` property with XML documentation |
| `Configuration/ScraperSelectors.cs` | ? Added comprehensive `Category` and `Department` selectors |
| `Services/EtimadScraperService.cs` | ? Added `ExtractCategoryDepartmentAsync()` method<br>? Updated card extraction to use new method<br>? Added fallback logic |

---

## ? Verification Checklist

- [x] Build successful
- [ ] API runs without errors
- [ ] Category field populated in response
- [ ] Department field populated in response
- [ ] Debug logs show extraction working
- [ ] Multiple pages tested

---

## ?? If Still Not Working

### **Check Console Logs**

Look for debug messages:
```
dbug: Found category/department with primary selector: ...
dbug: Found category/department in badge: ...
dbug: No category/department found with selector: ...
```

### **Inspect Page Structure**

1. Open browser developer tools on Etimad page
2. Find the blue badge element
3. Note its **class names** and **styles**
4. Update selectors in `ScraperSelectors.cs` if needed

### **Example Inspection**

```html
<!-- If the badge looks like this: -->
<span class="custom-badge-blue" style="background: #0066cc">
  ?????? ???? ????? ??????? - ????? ?????????
</span>

<!-- Add this selector: -->
public const string Category = "..., .custom-badge-blue, [style*='background: #0066cc'], ...";
```

---

## ?? Next Steps

1. **Test the scraper** with the updated code
2. **Verify** that category/department fields are populated
3. **Check debug logs** to see which extraction strategy works
4. **Share results** if still having issues

---

**File:** `CATEGORY_DEPARTMENT_FIX.md`  
**Date:** January 2025  
**Status:** ? Fix Applied - Ready for Testing
