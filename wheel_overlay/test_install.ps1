# Test Installation Script
# Helps test the MSI installation with various options
$ErrorActionPreference = "Stop"

$msiPath = ".\Package\WheelOverlay.msi"

if (-not (Test-Path $msiPath)) {
    Write-Host "ERROR: MSI file not found at $msiPath" -ForegroundColor Red
    Write-Host "Run .\build_msi.ps1 first to create the MSI" -ForegroundColor Yellow
    exit 1
}

Write-Host "=== WheelOverlay MSI Test Installation ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "MSI File: $msiPath" -ForegroundColor White
Write-Host ""
Write-Host "Choose installation type:" -ForegroundColor Yellow
Write-Host "  1. Interactive Install (shows all dialogs)" -ForegroundColor White
Write-Host "  2. Interactive Install with Logging" -ForegroundColor White
Write-Host "  3. Silent Install" -ForegroundColor White
Write-Host "  4. Silent Install with Logging" -ForegroundColor White
Write-Host "  5. Uninstall (interactive)" -ForegroundColor White
Write-Host "  6. Uninstall (silent)" -ForegroundColor White
Write-Host "  7. View Install Log (if exists)" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Enter choice (1-7)"

switch ($choice) {
    "1" {
        Write-Host "Starting interactive installation..." -ForegroundColor Green
        Start-Process msiexec -ArgumentList "/i `"$msiPath`"" -Wait
    }
    "2" {
        $logFile = "install_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
        Write-Host "Starting interactive installation with logging..." -ForegroundColor Green
        Write-Host "Log file: $logFile" -ForegroundColor Gray
        Start-Process msiexec -ArgumentList "/i `"$msiPath`" /l*v `"$logFile`"" -Wait
        Write-Host ""
        Write-Host "Installation complete. View log with:" -ForegroundColor Yellow
        Write-Host "  notepad $logFile" -ForegroundColor White
    }
    "3" {
        Write-Host "Starting silent installation..." -ForegroundColor Green
        Start-Process msiexec -ArgumentList "/i `"$msiPath`" /quiet /norestart" -Wait
        Write-Host "Installation complete!" -ForegroundColor Green
    }
    "4" {
        $logFile = "install_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
        Write-Host "Starting silent installation with logging..." -ForegroundColor Green
        Write-Host "Log file: $logFile" -ForegroundColor Gray
        Start-Process msiexec -ArgumentList "/i `"$msiPath`" /quiet /norestart /l*v `"$logFile`"" -Wait
        Write-Host ""
        Write-Host "Installation complete. View log with:" -ForegroundColor Yellow
        Write-Host "  notepad $logFile" -ForegroundColor White
    }
    "5" {
        Write-Host "Starting interactive uninstallation..." -ForegroundColor Green
        Start-Process msiexec -ArgumentList "/x `"$msiPath`"" -Wait
    }
    "6" {
        Write-Host "Starting silent uninstallation..." -ForegroundColor Green
        Start-Process msiexec -ArgumentList "/x `"$msiPath`" /quiet /norestart" -Wait
        Write-Host "Uninstallation complete!" -ForegroundColor Green
    }
    "7" {
        $logs = Get-ChildItem -Filter "install_*.log" | Sort-Object LastWriteTime -Descending
        if ($logs.Count -eq 0) {
            Write-Host "No install logs found" -ForegroundColor Yellow
        } else {
            Write-Host "Opening most recent log: $($logs[0].Name)" -ForegroundColor Green
            notepad $logs[0].FullName
        }
    }
    default {
        Write-Host "Invalid choice" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
