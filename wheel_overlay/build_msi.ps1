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

Write-Host "Application built successfully" -ForegroundColor Green
Write-Host ""

# Step 2: Copy files to Package directory
Write-Host "[2/4] Preparing Package directory..." -ForegroundColor Yellow
$packageDir = ".\Package"

# Copy ALL published files (self-contained includes .NET runtime)
Write-Host "  Copying all published files..." -ForegroundColor Gray
Copy-Item "$publishDir\*" -Destination $packageDir -Recurse -Force -Exclude "*.pdb"

# Copy LICENSE.txt from root
Copy-Item ".\LICENSE.txt" -Destination $packageDir -Force

# Generate WiX components for all files
Write-Host "  Generating WiX components..." -ForegroundColor Gray
$allFiles = Get-ChildItem $packageDir -File
$files = $allFiles | Where-Object { $_.Extension -notin @('.wxs', '.msi', '.wixpdb') }
$components = @()
foreach ($file in $files) {
    $components += "      <Component>`n        <File Source=`"$($file.Name)`" />`n      </Component>"
}
$componentXml = $components -join "`n"

# Update Package.wxs with generated components
$packageWxs = Get-Content "$packageDir\Package.wxs" -Raw
$startMarker = "      <!-- APPLICATION_FILES_START -->"
$endMarker = "      <!-- APPLICATION_FILES_END -->"
$startIndex = $packageWxs.IndexOf($startMarker)
$endIndex = $packageWxs.IndexOf($endMarker) + $endMarker.Length

if ($startIndex -ge 0 -and $endIndex -gt $startIndex) {
    $beforeMarker = $packageWxs.Substring(0, $startIndex)
    $afterMarker = $packageWxs.Substring($endIndex)
    $newContent = $beforeMarker + $startMarker + "`n" + $componentXml + "`n      " + $endMarker + $afterMarker
    Set-Content "$packageDir\Package.wxs" -Value $newContent -NoNewline
    Write-Host "  Updated Package.wxs with $($files.Count) components" -ForegroundColor Gray
} else {
    Write-Host "  WARNING: Could not find component markers in Package.wxs" -ForegroundColor Yellow
}

Write-Host "Files copied to Package directory" -ForegroundColor Green
Write-Host ""

# Step 3: Build MSI with WiX v4
Write-Host "[3/4] Building MSI with WiX v4..." -ForegroundColor Yellow
Push-Location $packageDir

# Check if wix.exe is available (WiX v4)
$wixCmd = Get-Command wix -ErrorAction SilentlyContinue
if (-not $wixCmd) {
    Write-Host "ERROR: WiX v4 toolset not found!" -ForegroundColor Red
    Write-Host "Install with: dotnet tool install -g wix --version 4.0.5" -ForegroundColor Yellow
    Pop-Location
    exit 1
}

# Build the MSI with custom UI
wix build Package.wxs CustomUI.wxs -o WheelOverlay.msi

if ($LASTEXITCODE -ne 0) {
    Write-Host "MSI build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "MSI built successfully" -ForegroundColor Green
Pop-Location

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