# Test if EtimadScraper API is working

Write-Host "?? Testing EtimadScraper API..." -ForegroundColor Cyan
Write-Host ""

# Get the process running on port 7000 or 5000
$port7000 = Get-NetTCPConnection -LocalPort 7000 -ErrorAction SilentlyContinue
$port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue

if ($port7000 -or $port5000) {
    Write-Host "? API is running on:" -ForegroundColor Green
    if ($port7000) { Write-Host "   https://localhost:7000" -ForegroundColor Green }
    if ($port5000) { Write-Host "   http://localhost:5000" -ForegroundColor Green }
    Write-Host ""
    
    # Test the status endpoint
    Write-Host "Testing /api/TenderScraper/status endpoint..." -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri "https://localhost:7000/api/TenderScraper/status" -SkipCertificateCheck -ErrorAction Stop
        Write-Host "? Status endpoint works!" -ForegroundColor Green
        Write-Host "   Service: $($response.service)" -ForegroundColor Gray
        Write-Host "   Version: $($response.version)" -ForegroundColor Gray
        Write-Host "   Status: $($response.status)" -ForegroundColor Gray
        Write-Host ""
        Write-Host "? Swagger UI should be available at:" -ForegroundColor Green
        Write-Host "   https://localhost:7000/swagger" -ForegroundColor Cyan
    }
    catch {
        Write-Host "??  Could not connect to HTTPS, trying HTTP..." -ForegroundColor Yellow
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:5000/api/TenderScraper/status" -ErrorAction Stop
            Write-Host "? Status endpoint works (HTTP)!" -ForegroundColor Green
            Write-Host "   Service: $($response.service)" -ForegroundColor Gray
            Write-Host "   Version: $($response.version)" -ForegroundColor Gray
            Write-Host ""
            Write-Host "? Swagger UI should be available at:" -ForegroundColor Green
            Write-Host "   http://localhost:5000/swagger" -ForegroundColor Cyan
        }
        catch {
            Write-Host "? API is not responding. Error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}
else {
    Write-Host "? API is not running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "To start the API, run:" -ForegroundColor Yellow
    Write-Host "   cd EtimadScraper" -ForegroundColor Gray
    Write-Host "   dotnet run" -ForegroundColor Gray
    Write-Host ""
}

Write-Host ""
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
