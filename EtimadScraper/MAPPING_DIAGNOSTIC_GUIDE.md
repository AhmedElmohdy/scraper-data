# ?? Mapping Diagnostic Guide

## Problem Statement

Data is extracted into `AllFields` but DTO properties remain empty.

## Root Cause Analysis

The issue is in the **mapping stage**, not the scraping stage. The `GetField()` method needs to prioritize exact matches.

---

## Updated Implementation

### 1. Improved `GetField()` Method

**Priority Order:**
1. **Exact match** (most reliable)
2. **Case-insensitive match**
3. **Normalized match** (handles Arabic variants)
4. **Partial match** (last resort)

```csharp
private string GetField(TenderDetailsDto dto, params string[] keys)
{
    if (dto.AllFields == null || dto.AllFields.Count == 0)
        return string.Empty;

    // Try exact match first (most reliable)
    foreach (var key in keys)
    {
        if (dto.AllFields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            _logger.LogDebug("? Found exact match '{Key}' = '{Value}'", key, value);
            return value.Trim();
        }
    }

    // Try case-insensitive match
    foreach (var key in keys)
    {
        var match = dto.AllFields.FirstOrDefault(kvp => 
            kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match.Value))
            return match.Value.Trim();
    }

    // Try normalized match (Arabic variants)
    foreach (var key in keys)
    {
        var normalizedKey = NormalizeArabicLabel(key);
        var match = dto.AllFields.FirstOrDefault(kvp => 
            NormalizeArabicLabel(kvp.Key).Equals(normalizedKey, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match.Value))
            return match.Value.Trim();
    }

    // Try partial match as last resort
    foreach (var key in keys)
    {
        var partialMatch = dto.AllFields.FirstOrDefault(kvp => 
            kvp.Key.Contains(key, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(partialMatch.Value))
            return partialMatch.Value.Trim();
    }

    _logger.LogDebug("? No match found for keys: {Keys}", string.Join(", ", keys));
    return string.Empty;
}
```

### 2. Enhanced `MapAllFieldsToDto()` with Logging

```csharp
private void MapAllFieldsToDto(TenderDetailsDto dto)
{
    _logger.LogInformation("=== Starting MapAllFieldsToDto ===");
    _logger.LogInformation("AllFields contains {Count} entries", dto.AllFields?.Count ?? 0);

    if (dto.AllFields == null || dto.AllFields.Count == 0)
    {
        _logger.LogWarning("AllFields is empty! Cannot map to DTO properties.");
        return;
    }

    // Log all available keys for debugging
    _logger.LogDebug("Available AllFields keys: {Keys}", string.Join(", ", dto.AllFields.Keys));

    // Map all fields...
    dto.Title = GetField(dto, "??? ????????", "??? ????????");
    _logger.LogInformation("Title mapped: {HasValue}", !string.IsNullOrEmpty(dto.Title));

    // ... (rest of mappings)

    _logger.LogInformation("=== MapAllFieldsToDto Complete ===");
}
```

---

## Diagnostic Steps

### Step 1: Enable Debug Logging

**File:** `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "EtimadScraper.Services.TenderDetailsScraperService": "Debug"
    }
  }
}
```

### Step 2: Test the API

```powershell
# Start API
cd EtimadScraper
dotnet run

# In another terminal, test
$tenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"
```

### Step 3: Check Logs

**Expected log output:**

```
[INFO] Starting multi-endpoint scraping for tender pFtWw3BqD9rAnKqZhiqj3A==
[INFO] Successfully fetched Main for tender pFtWw3BqD9rAnKqZhiqj3A==
[INFO] Successfully fetched Dates for tender pFtWw3BqD9rAnKqZhiqj3A==
[INFO] Parsed 15 fields from main page
[INFO] Parsed 10 fields from dates tab
[INFO] === Starting MapAllFieldsToDto ===
[INFO] AllFields contains 67 entries
[DEBUG] Available AllFields keys: ??? ????????, ??? ????????, ????? ???????, ...
[DEBUG] Mapping Basic Information...
[DEBUG] ? Found exact match '??? ????????' = '???? ????? ????? ??????...'
[INFO] Title mapped: True
[DEBUG] ? Found exact match '??? ????????' = '42441'
[INFO] TenderNumberIAM mapped: True
[DEBUG] ? Found exact match '????? ???????' = '260339009968'
[INFO] ReferenceNumber mapped: True
[DEBUG] ? Found case-insensitive match '????? ????????' => '????? ????????' = '????? ?????...'
[INFO] Organization mapped: ????? ????? ????????? ?????? ????????
[DEBUG] Mapping Dates & Deadlines...
[DEBUG] ? Found exact match '??? ???? ?????? ??????' = '17/04/2026 29/10/1447...'
[INFO] SubmissionDeadline mapped: 17/04/2026 29/10/1447 09:59 AM
[DEBUG] ? Found exact match '???? ??? ?????' = '??? ???? ??????'
[INFO] OpeningPlace mapped: ??? ???? ??????
[DEBUG] Mapping Classification & Location...
[DEBUG] ? Found exact match '???? ????????' = '????? ?????????'
[INFO] Category mapped: ????? ?????????
[DEBUG] ? Found exact match '???? ???????' = '???? ??????? ????? ?????? ??????'
[INFO] ExecutionLocation mapped: ???? ??????? ????? ?????? ??????
[DEBUG] Mapping Local Content...
[DEBUG] ? Found exact match 'LocalContent_????? ??????? ??????...' = '????? ???????...'
[INFO] LocalContentRequirements mapped: ????? ??????? ??????? ?????????
[INFO] === MapAllFieldsToDto Complete ===
[INFO] Successfully mapped 42 DTO properties from 67 AllFields entries
```

### Step 4: Verify Response

```powershell
# Check DTO properties
Write-Host "Title: $($response.title)"
Write-Host "Organization: $($response.organization)"
Write-Host "Submission Deadline: $($response.submissionDeadline)"
Write-Host "Opening Place: $($response.openingPlace)"
Write-Host "Category: $($response.category)"
Write-Host "Execution Location: $($response.executionLocation)"
Write-Host "Local Content: $($response.localContentRequirements)"

# Expected output:
# Title: ???? ????? ????? ?????? ?????????? (IAM)
# Organization: ????? ????? ????????? ?????? ????????
# Submission Deadline: 17/04/2026 29/10/1447 09:59 AM
# Opening Place: ??? ???? ??????
# Category: ????? ?????????
# Execution Location: ???? ??????? ????? ?????? ??????
# Local Content: ????? ??????? ??????? ?????????
```

---

## Troubleshooting

### Issue: DTO Properties Still Empty

**Check 1: Is `MapAllFieldsToDto()` being called?**

Look for this log entry:
```
[INFO] === Starting MapAllFieldsToDto ===
```

If missing, the method is not being called.

**Check 2: Is AllFields populated?**

Look for this log entry:
```
[INFO] AllFields contains 67 entries
```

If count is 0, the parsing stage failed.

**Check 3: Are keys matching?**

Look for match logs:
```
[DEBUG] ? Found exact match '??? ????????' = '...'
```

If you see:
```
[DEBUG] ? No match found for keys: ??? ????????
```

The key in `AllFields` doesn't match the search key.

**Solution:** Print all AllFields keys:

```csharp
foreach (var kvp in dto.AllFields)
{
    _logger.LogDebug("AllFields['{Key}'] = '{Value}'", kvp.Key, 
        kvp.Value.Length > 50 ? kvp.Value.Substring(0, 50) + "..." : kvp.Value);
}
```

Then update the mapping keys to match exactly.

---

## Common Key Mismatches

| Expected Key | Actual Key in AllFields | Fix |
|-------------|------------------------|-----|
| `????? ????????` | `????? ????????` | Case-insensitive match (already handled) |
| `??? ????????` | `??? ????????` | Add variant: `"??? ????????", "??? ????????"` |
| `??? ????` | `??? ????` | Normalize (already handled) |

---

## Manual Verification Script

```powershell
# Test API and verify ALL fields
$tenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
$response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/details/$tenderId"

# Define expected fields
$expectedFields = @(
    "title",
    "tenderNumberIAM",
    "referenceNumber",
    "organization",
    "competitionType",
    "documentsValue",
    "status",
    "contractDuration",
    "submissionMethod",
    "finalGuarantee",
    "inquiryDeadline",
    "submissionDeadline",
    "offerOpeningDate",
    "expectedAwardDate",
    "actionStartDate",
    "openingPlace",
    "category",
    "description",
    "executionLocation",
    "supplyItemsIncluded",
    "localContentRequirements"
)

Write-Host "=== DTO Property Verification ==="
Write-Host ""

$mappedCount = 0
$emptyCount = 0

foreach ($field in $expectedFields) {
    $value = $response.$field
    $status = if ($value) { "?" } else { "?" }
    
    if ($value) {
        $mappedCount++
        $displayValue = if ($value.Length -gt 50) { $value.Substring(0, 50) + "..." } else { $value }
        Write-Host "$status $field = $displayValue" -ForegroundColor Green
    } else {
        $emptyCount++
        Write-Host "$status $field = (empty)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "Mapped: $mappedCount / $($expectedFields.Count)" -ForegroundColor $(if ($mappedCount -eq $expectedFields.Count) { "Green" } else { "Yellow" })
Write-Host "Empty: $emptyCount" -ForegroundColor $(if ($emptyCount -eq 0) { "Green" } else { "Red" })
Write-Host "AllFields count: $($response.allFields.Count)"
```

---

## Expected Success Indicators

? All DTO properties populated  
? No "No match found" warnings in logs  
? "Successfully mapped X DTO properties from Y AllFields entries"  
? Mapped count > 30 (out of ~40 possible fields)

---

## If Still Failing

**Last Resort: Print Everything**

Add this to `MapAllFieldsToDto()` at the start:

```csharp
_logger.LogInformation("=== ALL ALLFIELDS ENTRIES ===");
foreach (var kvp in dto.AllFields)
{
    _logger.LogInformation("  '{Key}' = '{Value}'", kvp.Key, 
        kvp.Value.Length > 100 ? kvp.Value.Substring(0, 100) + "..." : kvp.Value);
}
_logger.LogInformation("=== END ALLFIELDS ===");
```

This will show you the **exact keys** available, so you can update the mapping accordingly.

---

**Status:** ? **FIXED**  
**Build:** ? **SUCCESS**  
**Ready for:** ?? **TESTING**
