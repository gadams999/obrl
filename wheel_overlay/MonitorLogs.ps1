# Monitor WheelOverlay Logs in Real-Time
# This script will display the log file and continuously update as new entries are added

$logPath = "$env:APPDATA\WheelOverlay\logs.txt"

Write-Host "WheelOverlay Log Monitor" -ForegroundColor Cyan
Write-Host "=======================" -ForegroundColor Cyan
Write-Host "Log file: $logPath" -ForegroundColor Yellow
Write-Host ""

# Check if log file exists
if (-not (Test-Path $logPath)) {
    Write-Host "Log file does not exist yet. Waiting for application to start..." -ForegroundColor Yellow
    Write-Host "Start WheelOverlay to create the log file." -ForegroundColor Yellow
    Write-Host ""
    
    # Wait for file to be created
    while (-not (Test-Path $logPath)) {
        Start-Sleep -Seconds 1
    }
    
    Write-Host "Log file created! Starting monitor..." -ForegroundColor Green
    Write-Host ""
}

Write-Host "Monitoring logs (Press Ctrl+C to stop)..." -ForegroundColor Green
Write-Host "-------------------------------------------" -ForegroundColor Gray
Write-Host ""

# Display last 20 lines and follow new content
Get-Content $logPath -Wait -Tail 20
