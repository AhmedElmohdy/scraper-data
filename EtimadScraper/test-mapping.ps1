# Test Tender Details Mapping
# This script verifies that DTO properties are properly mapped from AllFields

param(
    [string]$BaseUrl = "https://localhost:7000",
    [string]$TenderId = "pFtWw3BqD9rAnKqZhiqj3A=="
)

Write-Host "=== Tender Details Mapping Test ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Testing tender ID: $TenderId" -ForegroundColor Yellow
Write-Host "API URL: $BaseUrl" -ForegroundColor Yellow
Write-Host ""

try {
    # Fetch tender details
    Write-Host "Fetching tender details..." -ForegroundColor Gray
    $url = "$BaseUrl/api/TenderScraper/details/$([System.Uri]::EscapeDataString($TenderId))"
    $response = Invoke-RestMethod -Uri $url -ErrorAction Stop
    
    Write-Host "? API call successful" -ForegroundColor Green
    Write-Host ""

    # Check basic metadata
    Write-Host "=== Metadata ===" -ForegroundColor Cyan
    Write-Host "Tender ID: $($response.tenderId)"
    Write-Host "Success: $($response.isSuccess)"
    Write-Host "Data Source: $($response.dataSource)"
    Write-Host "Scraped At: $($response.scrapedAt)"
    Write-Host "AllFields Count: $($response.allFields.Count)" -ForegroundColor Yellow
    
    if ($response.errorMessage) {
        Write-Host "Error Message: $($response.errorMessage)" -ForegroundColor Yellow
    }
    
    Write-Host ""

    # Define critical fields to check
    $criticalFields = @(
        @{ Name = "title"; Label = "Title (??? ????????)" },
        @{ Name = "tenderNumberIAM"; Label = "Tender Number (??? ????????)" },
        @{ Name = "referenceNumber"; Label = "Reference Number (????? ???????)" },
        @{ Name = "organization"; Label = "Organization (????? ????????)" },
        @{ Name = "competitionType"; Label = "Competition Type (??? ????????)" },
        @{ Name = "submissionDeadline"; Label = "Submission Deadline (??? ????)" },
        @{ Name = "openingPlace"; Label = "Opening Place (???? ??? ?????)" },
        @{ Name = "category"; Label = "Category (???? ????????)" },
        @{ Name = "executionLocation"; Label = "Execution Location (???? ???????)" },
        @{ Name = "localContentRequirements"; Label = "Local Content (??????? ??????)" }
    )

    Write-Host "=== Critical DTO Properties ===" -ForegroundColor Cyan
    
    $mappedCount = 0
    $emptyCount = 0

    foreach ($field in $criticalFields) {
        $value = $response.($field.Name)
        
        if ($value) {
            $mappedCount++
            $displayValue = if ($value.Length -gt 60) { $value.Substring(0, 60) + "..." } else { $value }
            Write-Host "? $($field.Label)" -ForegroundColor Green
            Write-Host "  Value: $displayValue" -ForegroundColor Gray
        } else {
            $emptyCount++
            Write-Host "? $($field.Label)" -ForegroundColor Red
            Write-Host "  Value: (empty)" -ForegroundColor Gray
        }
    }

    Write-Host ""
    Write-Host "=== Summary ===" -ForegroundColor Cyan
    Write-Host "Mapped: $mappedCount / $($criticalFields.Count)" -ForegroundColor $(if ($mappedCount -eq $criticalFields.Count) { "Green" } elseif ($mappedCount -gt 5) { "Yellow" } else { "Red" })
    Write-Host "Empty: $emptyCount" -ForegroundColor $(if ($emptyCount -eq 0) { "Green" } else { "Red" })
    Write-Host "AllFields Count: $($response.allFields.Count)" -ForegroundColor Yellow

    Write-Host ""

    # Overall assessment
    if ($mappedCount -eq $criticalFields.Count) {
        Write-Host "?? SUCCESS! All critical fields are mapped." -ForegroundColor Green
        exit 0
    } elseif ($mappedCount -gt 5) {
        Write-Host "? PARTIAL SUCCESS. Most fields mapped but some are missing." -ForegroundColor Yellow
        Write-Host "Check logs for detailed mapping information." -ForegroundColor Yellow
        exit 0
    } else {
        Write-Host "? FAILURE. Most fields are empty." -ForegroundColor Red
        Write-Host ""
        Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
        Write-Host "1. Check if MapAllFieldsToDto() is being called"
        Write-Host "2. Enable debug logging in appsettings.json"
        Write-Host "3. Verify AllFields contains data (count > 0)"
        Write-Host "4. Check logs for 'No match found' warnings"
        exit 1
    }

} catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure the API is running:" -ForegroundColor Yellow
    Write-Host "  cd EtimadScraper" -ForegroundColor Gray
    Write-Host "  dotnet run" -ForegroundColor Gray
    exit 1
}
