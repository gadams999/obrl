# Complete Build Script for WheelOverlay MSI
# Builds the application, prepares files, and creates MSI installer
$ErrorActionPreference = "Stop"

Write-Host "=== WheelOverlay MSI Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build the .NET application
Write-Host "[1/4] Building .NET application..." -ForegroundColor Yellow
$projectPath = ".\WheelOverlay\WheelOverlay.csproj"
$publishDir = ".\Publish"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:TreatWarningsAsErrors=true -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build Failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Application built successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Copy files to Package directory
Write-Host "[2/4] Preparing Package directory..." -ForegroundColor Yellow
$packageDir = ".\Package"

# Copy ALL published files (self-contained includes .NET runtime)
Write-Host "  Copying all published files..." -ForegroundColor Gray
Copy-Item "$publishDir\*" -Destination $packageDir -Recurse -Force -Exclude "*.pdb"

# Copy LICENSE.txt from root
Copy-Item ".\LICENSE.txt" -Destination $packageDir -Force

Write-Host "✓ Files copied to Package directory" -ForegroundColor Green
Write-Host ""

# Step 3: Build MSI with WiX v4
Write-Host "[3/4] Building MSI with WiX v4..." -ForegroundColor Yellow
Push-Location $packageDir

try {
    # Check if wix.exe is available (WiX v4)
    $wixCmd = Get-Command wix -ErrorAction SilentlyContinue
    if (-not $wixCmd) {
        Write-Host "ERROR: WiX v4 toolset not found!" -ForegroundColor Red
        Write-Host "Install with: dotnet tool install -g wix --version 4.0.5" -ForegroundColor Yellow
        exit 1
    }

    # Harvest all files from current directory
    Write-Host "  Harvesting files..." -ForegroundColor Gray
    
    # Create a fragment with all files
    $files = Get-ChildItem -File | Where-Object { $_.Extension -notin @('.wxs','.wixobj','.wixpdb','.msi') -and $_.Name -ne 'app.ico' }
    Write-Host "  Found $($files.Count) files to package" -ForegroundColor Gray
    $fileComponents = @()
    
    $firstFile = $true
    foreach ($file in $files) {
        if ($firstFile) {
            # First file is the KeyPath
            $fileComponents += "      <Component>`n        <File Source=`"$($file.Name)`" KeyPath=`"yes`" />`n      </Component>"
            $firstFile = $false
        } else {
            $fileComponents += "      <Component>`n        <File Source=`"$($file.Name)`" />`n      </Component>"
        }
    }
    
    # Create temporary file list
    $componentXml = $fileComponents -join "`n"
    
    # Update Package.wxs with file list
    $wxsContent = Get-Content "Package.wxs" -Raw
    $wxsContent = $wxsContent -replace '(?s)<!-- APPLICATION_FILES_START -->.*?<!-- APPLICATION_FILES_END -->', "<!-- APPLICATION_FILES_START -->`n$componentXml`n      <!-- APPLICATION_FILES_END -->"
    Set-Content "Package_temp.wxs" $wxsContent
    
    # Build the MSI with custom UI
    wix build Package_temp.wxs CustomUI.wxs -o WheelOverlay.msi
    
    # Clean up
    Remove-Item "Package_temp.wxs" -ErrorAction SilentlyContinue

    if ($LASTEXITCODE -ne 0) {
        Write-Host "MSI build failed!" -ForegroundColor Red
        exit 1
    }

    Write-Host "✓ MSI built successfully" -ForegroundColor Green
} finally {
    Pop-Location
}

Write-Host ""

# Step 4: Summary
Write-Host "[4/4] Build Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Output Files:" -ForegroundColor Cyan
Write-Host "  MSI Installer: .\Package\WheelOverlay.msi" -ForegroundColor White
Write-Host "  Application:   .\Publish\WheelOverlay.exe" -ForegroundColor White
Write-Host ""
Write-Host "To install, run:" -ForegroundColor Yellow
Write-Host "  msiexec /i Package\WheelOverlay.msi" -ForegroundColor White
Write-Host ""
Write-Host "To install with logging:" -ForegroundColor Yellow
Write-Host "  msiexec /i Package\WheelOverlay.msi /l*v install.log" -ForegroundColor White
Write-Host ""
