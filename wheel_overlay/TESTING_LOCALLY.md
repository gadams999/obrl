# Testing WheelOverlay Locally

This guide explains how to run and test the WheelOverlay application locally after the .NET 10 upgrade.

## Quick Start - Run the Application

### Option 1: Run with dotnet (Recommended for Testing)

```powershell
# Navigate to the project directory
cd obrl/wheel_overlay

# Run in Debug mode (with console output)
dotnet run --project WheelOverlay

# Run in Release mode
dotnet run --project WheelOverlay --configuration Release
```

### Option 2: Run the Compiled Executable

```powershell
# After building, run the executable directly
cd obrl/wheel_overlay

# Debug build
.\WheelOverlay\bin\Debug\net10.0-windows\WheelOverlay.exe

# Release build
.\WheelOverlay\bin\Release\net10.0-windows\WheelOverlay.exe
```

### Option 3: Run with Test Mode (No Hardware Required)

Test mode allows you to test the application without a physical wheel device:

```powershell
# Run with test mode enabled
dotnet run --project WheelOverlay -- --test-mode

# Or with the executable
.\WheelOverlay\bin\Debug\net10.0-windows\WheelOverlay.exe --test-mode
```

**In Test Mode:**
- Use **Right Arrow** to advance position
- Use **Left Arrow** to go back
- No DirectInput device required

## Testing Different Features

### 1. Test System Tray Menu

1. Run the application
2. Look for the wheel icon in the system tray (bottom-right of taskbar)
3. Right-click the icon to see the menu:
   - **Settings** - Opens configuration dialog
   - **Config Mode** - Toggle to enable window dragging
   - **Minimize** - Minimize to taskbar
   - **About Wheel Overlay** - Show version info
   - **Exit** - Close application

### 2. Test Config Mode (Window Positioning)

1. Run the application
2. Right-click system tray icon → **Config Mode** (check it)
3. The overlay window should now be draggable
4. Click and drag the window to a new position
5. Uncheck **Config Mode** - position should be saved
6. Restart the app - window should appear at saved position

### 3. Test Layout Modes

1. Run the application
2. Right-click system tray icon → **Settings**
3. Try each layout mode:
   - **Vertical** - Positions shown in a vertical list
   - **Horizontal** - Positions shown in a horizontal row
   - **Grid** - Positions shown in a configurable grid
   - **Single** - Only current position shown

**Important:** This tests the vertical layout crash fix!

### 4. Test Position Changes (Test Mode)

1. Run with test mode: `dotnet run --project WheelOverlay -- --test-mode`
2. Press **Right Arrow** - position should advance
3. Press **Left Arrow** - position should go back
4. Keep pressing Right Arrow past the last position - should wrap to first
5. Keep pressing Left Arrow past the first position - should wrap to last

### 5. Test Fresh Install Scenario

To test the vertical layout crash fix on a fresh install:

```powershell
# Backup your current settings (if you have any)
Copy-Item "$env:APPDATA\WheelOverlay\settings.json" "$env:APPDATA\WheelOverlay\settings.json.backup"

# Delete settings to simulate fresh install
Remove-Item "$env:APPDATA\WheelOverlay\settings.json" -ErrorAction SilentlyContinue

# Run the application
dotnet run --project WheelOverlay

# Open Settings and select Vertical layout
# Should NOT crash!

# Restore your backup if needed
Copy-Item "$env:APPDATA\WheelOverlay\settings.json.backup" "$env:APPDATA\WheelOverlay\settings.json"
```

## Running Tests

### Run All Tests

```powershell
cd obrl/wheel_overlay

# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with test results file
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### Run Specific Test Classes

```powershell
# Run only tests from a specific class
dotnet test --filter "FullyQualifiedName~WheelOverlay.Tests.SettingsTests"

# Run tests by category/trait
dotnet test --filter "Category=Unit"
```

### View Test Coverage

```powershell
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Coverage report will be in: TestResults/[guid]/coverage.cobertura.xml
```

## Build Verification

### Build Both Configurations

```powershell
# Clean previous builds
dotnet clean

# Build Debug
dotnet build

# Build Release
dotnet build -c Release

# Verify no errors or warnings
```

### Check Build Output

```powershell
# Debug output location
ls .\WheelOverlay\bin\Debug\net10.0-windows\

# Release output location
ls .\WheelOverlay\bin\Release\net10.0-windows\

# Should see:
# - WheelOverlay.exe
# - WheelOverlay.dll
# - All dependencies
```

## Troubleshooting

### Application Won't Start

1. Check .NET 10 is installed: `dotnet --version`
2. Rebuild: `dotnet clean && dotnet build`
3. Check for errors in Event Viewer (Windows Logs → Application)

### System Tray Icon Not Appearing

1. Check Windows notification area settings
2. Look for hidden icons (click up arrow in system tray)
3. Run with console to see error messages: `dotnet run --project WheelOverlay`

### DirectInput Device Not Found

1. Use test mode: `--test-mode` flag
2. Check device is connected and recognized by Windows
3. Check Device Manager for DirectInput devices

### Settings Not Persisting

Settings are stored at: `%APPDATA%\WheelOverlay\settings.json`

```powershell
# View current settings
Get-Content "$env:APPDATA\WheelOverlay\settings.json"

# Check if directory exists
Test-Path "$env:APPDATA\WheelOverlay"
```

## Performance Testing

### Check Startup Time

```powershell
Measure-Command { 
    Start-Process ".\WheelOverlay\bin\Release\net10.0-windows\WheelOverlay.exe"
    Start-Sleep -Seconds 2
    Stop-Process -Name "WheelOverlay"
}
```

### Monitor Resource Usage

1. Run the application
2. Open Task Manager (Ctrl+Shift+Esc)
3. Find "WheelOverlay" process
4. Check CPU and Memory usage (should be minimal when idle)

## Version Verification

### Check Version Numbers

```powershell
# Check assembly version
(Get-Item ".\WheelOverlay\bin\Debug\net10.0-windows\WheelOverlay.exe").VersionInfo

# Should show:
# ProductVersion: 5.1.0
# FileVersion: 5.1.0
```

### Check About Dialog

1. Run the application
2. Right-click system tray icon → **About Wheel Overlay**
3. Should display: "Wheel Overlay v5.1.0"

## Next Steps

After local testing is successful:

1. ✓ Verify all features work as expected
2. ✓ Confirm no crashes or errors
3. ✓ Check version numbers are correct
4. → Proceed to next task: Implement centralized version management (Task 6)

## Getting Help

If you encounter issues:

1. Check the logs in `%APPDATA%\WheelOverlay\logs\`
2. Run with verbose output: `dotnet run --project WheelOverlay --verbosity detailed`
3. Review the requirements: `.kiro/specs/dotnet10-upgrade-and-testing/requirements.md`
4. Review the design: `.kiro/specs/dotnet10-upgrade-and-testing/design.md`
