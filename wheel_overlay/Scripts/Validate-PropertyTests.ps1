<#
.SYNOPSIS
    Validates that all property-based tests have conditional compilation directives.

.DESCRIPTION
    This script scans C# test files for [Property(MaxTest = X)] attributes and verifies
    that each has the correct conditional compilation directives for FAST_TESTS configuration.
    
    The script is designed for use in CI pipelines to ensure all property tests follow
    the required pattern before building and testing.
    
    Returns exit code 0 if all property tests are valid, exit code 1 if any are missing directives.

.PARAMETER TestPath
    Path to the test directory. Defaults to WheelOverlay.Tests relative to script location.

.PARAMETER Quiet
    Suppress detailed output, only show summary and errors.

.EXAMPLE
    .\Validate-PropertyTests.ps1
    Validate all property tests and show detailed results.

.EXAMPLE
    .\Validate-PropertyTests.ps1 -Quiet
    Validate all property tests with minimal output.

.EXAMPLE
    if (.\Validate-PropertyTests.ps1) { Write-Host "All tests valid" }
    Use in a script to check validation status.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$TestPath,
    
    [Parameter()]
    [switch]$Quiet
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Determine test directory path
if (-not $TestPath) {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $TestPath = Join-Path (Split-Path -Parent $scriptDir) "WheelOverlay.Tests"
}

if (-not (Test-Path $TestPath)) {
    Write-Error "Test directory not found: $TestPath"
    exit 1
}

if (-not $Quiet) {
    Write-Host "Validating property tests in: $TestPath" -ForegroundColor Cyan
    Write-Host ""
}

# Statistics
$stats = @{
    FilesScanned = 0
    FilesWithPropertyTests = 0
    PropertyTestsFound = 0
    PropertyTestsValid = 0
    PropertyTestsInvalid = 0
}

# Track files with issues
$filesWithIssues = @()
$issueDetails = @()

# Get all C# test files
$testFiles = Get-ChildItem -Path $TestPath -Filter "*.cs" -Recurse

foreach ($file in $testFiles) {
    $stats.FilesScanned++
    
    $content = Get-Content -Path $file.FullName -Raw
    $lines = Get-Content -Path $file.FullName
    
    # Find all [Property(MaxTest = X)] attributes
    $propertyMatches = [regex]::Matches($content, '\[Property\(MaxTest\s*=\s*(\d+)\)\]')
    
    if ($propertyMatches.Count -eq 0) {
        continue
    }
    
    $stats.FilesWithPropertyTests++
    $fileHasIssues = $false
    
    foreach ($match in $propertyMatches) {
        # Find the line number of this match
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count - 1
        
        # Check if this line is inside a conditional compilation block
        $insideConditionalBlock = $false
        for ($i = $lineNumber - 1; $i -ge 0; $i--) {
            $checkLine = $lines[$i].Trim()
            if ($checkLine -match '^#if\s+FAST_TESTS') {
                $insideConditionalBlock = $true
                break
            }
            # Stop checking if we hit a method or class declaration
            if ($checkLine -match '^\s*(public|private|protected|internal)\s+(class|Property|void|static)') {
                break
            }
        }
        
        if ($insideConditionalBlock) {
            # This property attribute is already inside a conditional block, skip validation
            continue
        }
        
        $stats.PropertyTestsFound++
        
        # Check if there are directives above this attribute
        $hasValidDirectives = $false
        
        # Look at the 3 lines before the [Property] attribute
        if ($lineNumber -ge 3) {
            $line1 = $lines[$lineNumber - 3].Trim()
            $line2 = $lines[$lineNumber - 2].Trim()
            $line3 = $lines[$lineNumber - 1].Trim()
            
            # Check for the pattern: #if FAST_TESTS, [Property(MaxTest = 10)], #else
            if ($line1 -match '^#if\s+FAST_TESTS' -and 
                $line2 -match '\[Property\(MaxTest\s*=\s*10\)\]' -and 
                $line3 -match '^#else') {
                $hasValidDirectives = $true
            }
        }
        
        if ($hasValidDirectives) {
            $stats.PropertyTestsValid++
        }
        else {
            $stats.PropertyTestsInvalid++
            $fileHasIssues = $true
            
            $relativePath = $file.FullName.Replace($TestPath, "").TrimStart('\', '/')
            $issueDetails += [PSCustomObject]@{
                File = $relativePath
                Line = $lineNumber + 1
                Issue = "Property test missing conditional compilation directives"
            }
            
            if (-not $Quiet) {
                Write-Host "  [INVALID] $relativePath" -ForegroundColor Red
                Write-Host "    Line $($lineNumber + 1): Property test missing directives" -ForegroundColor Yellow
            }
        }
    }
    
    if ($fileHasIssues -and -not ($filesWithIssues -contains $file.FullName)) {
        $filesWithIssues += $file.FullName
    }
}

# Print summary
if (-not $Quiet) {
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Files scanned:                 $($stats.FilesScanned)"
Write-Host "Files with property tests:     $($stats.FilesWithPropertyTests)"
Write-Host "Total property tests found:    $($stats.PropertyTestsFound)"
Write-Host "Property tests valid:          $($stats.PropertyTestsValid)" -ForegroundColor Green
Write-Host "Property tests invalid:        $($stats.PropertyTestsInvalid)" -ForegroundColor $(if ($stats.PropertyTestsInvalid -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($stats.PropertyTestsInvalid -eq 0) {
    Write-Host "[PASS] All property tests have valid conditional compilation directives" -ForegroundColor Green
    Write-Host ""
    exit 0
}
else {
    Write-Host "[FAIL] Some property tests are missing conditional compilation directives" -ForegroundColor Red
    Write-Host ""
    Write-Host "Files needing fixes ($($filesWithIssues.Count)):" -ForegroundColor Red
    foreach ($file in $filesWithIssues) {
        $relativePath = $file.Replace($TestPath, "").TrimStart('\', '/')
        Write-Host "  - $relativePath" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "To fix these issues, run:" -ForegroundColor Yellow
    Write-Host '  .\Scripts\Add-PropertyTestDirectives.ps1' -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
