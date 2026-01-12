<#
.SYNOPSIS
    Adds or validates conditional compilation directives for property-based tests.

.DESCRIPTION
    This script scans C# test files for [Property(MaxTest = X)] attributes and ensures
    they have the correct conditional compilation directives for FAST_TESTS configuration.
    
    The script can:
    - Add missing directives to property tests
    - Validate that all property tests have correct directives
    - Preview changes before applying them
    - Generate a report of changes made

.PARAMETER WhatIf
    Preview changes without modifying files.

.PARAMETER Validate
    Only validate that all property tests have directives. Returns exit code 1 if any are missing.

.PARAMETER TestPath
    Path to the test directory. Defaults to WheelOverlay.Tests relative to script location.

.EXAMPLE
    .\Add-PropertyTestDirectives.ps1 -WhatIf
    Preview what changes would be made without modifying files.

.EXAMPLE
    .\Add-PropertyTestDirectives.ps1
    Add directives to all property tests that are missing them.

.EXAMPLE
    .\Add-PropertyTestDirectives.ps1 -Validate
    Check if all property tests have directives and report any missing.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$WhatIf,
    
    [Parameter()]
    [switch]$Validate,
    
    [Parameter()]
    [string]$TestPath
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

Write-Host "Scanning test files in: $TestPath" -ForegroundColor Cyan
Write-Host ""

# Statistics
$stats = @{
    FilesScanned = 0
    FilesWithPropertyTests = 0
    PropertyTestsFound = 0
    PropertyTestsWithDirectives = 0
    PropertyTestsNeedingDirectives = 0
    FilesModified = 0
}

# Track files needing fixes
$filesNeedingFixes = @()

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
    
    $needsModification = $false
    $modifiedLines = $lines.Clone()
    $lineOffset = 0
    $processedMatches = @()
    
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
            # This property attribute is already inside a conditional block, skip it
            continue
        }
        
        $stats.PropertyTestsFound++
        
        # Check if there are already directives above this attribute
        $hasDirectives = $false
        
        # Look at the 3 lines before the [Property] attribute
        if ($lineNumber -ge 3) {
            $line1 = $lines[$lineNumber - 3].Trim()
            $line2 = $lines[$lineNumber - 2].Trim()
            $line3 = $lines[$lineNumber - 1].Trim()
            
            # Check for the pattern: #if FAST_TESTS, [Property(MaxTest = 10)], #else
            if ($line1 -match '^#if\s+FAST_TESTS' -and 
                $line2 -match '\[Property\(MaxTest\s*=\s*10\)\]' -and 
                $line3 -match '^#else') {
                $hasDirectives = $true
                $stats.PropertyTestsWithDirectives++
            }
        }
        
        if (-not $hasDirectives) {
            $needsModification = $true
            $stats.PropertyTestsNeedingDirectives++
            $processedMatches += $match
            
            # Calculate the actual line number in the modified array (accounting for previous insertions)
            $actualLineNumber = $lineNumber + $lineOffset
            
            # Get the indentation of the [Property] line
            $propertyLine = $modifiedLines[$actualLineNumber]
            $indentation = ""
            if ($propertyLine -match '^(\s*)') {
                $indentation = $matches[1]
            }
            
            # Create the directive lines
            $directiveLines = @(
                "${indentation}#if FAST_TESTS",
                "${indentation}[Property(MaxTest = 10)]",
                "${indentation}#else",
                "${indentation}[Property(MaxTest = 100)]",
                "${indentation}#endif"
            )
            
            # Remove the original [Property] line and insert the directives
            $modifiedLines = $modifiedLines[0..($actualLineNumber - 1)] + 
                             $directiveLines + 
                             $modifiedLines[($actualLineNumber + 1)..($modifiedLines.Length - 1)]
            
            # Update offset for subsequent matches in this file
            $lineOffset += 4  # We added 5 lines and removed 1, net +4
            
            if ($WhatIf -or $Validate) {
                Write-Host "  Line $($lineNumber + 1): Property test needs directives" -ForegroundColor Yellow
            }
        }
    }
    
    if ($needsModification) {
        $filesNeedingFixes += $file.FullName
        
        # Count is already tracked in $processedMatches
        $testsNeedingDirectivesInFile = $processedMatches.Count
        
        if ($WhatIf) {
            Write-Host "[PREVIEW] Would modify: $($file.Name)" -ForegroundColor Yellow
            Write-Host "  Found $($stats.PropertyTestsFound) property test(s), $testsNeedingDirectivesInFile need directives" -ForegroundColor Gray
        }
        elseif ($Validate) {
            Write-Host "[MISSING] $($file.Name)" -ForegroundColor Red
            Write-Host "  Found $($stats.PropertyTestsFound) property test(s), $testsNeedingDirectivesInFile need directives" -ForegroundColor Gray
        }
        else {
            # Write the modified content back to the file
            $modifiedLines | Set-Content -Path $file.FullName -Encoding UTF8
            $stats.FilesModified++
            Write-Host "[MODIFIED] $($file.Name)" -ForegroundColor Green
            Write-Host "  Added directives to $testsNeedingDirectivesInFile property test(s)" -ForegroundColor Gray
        }
    }
    else {
        Write-Verbose "  $($file.Name): All property tests have directives"
    }
}

# Print summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Files scanned:                    $($stats.FilesScanned)"
Write-Host "Files with property tests:        $($stats.FilesWithPropertyTests)"
Write-Host "Total property tests found:       $($stats.PropertyTestsFound)"
Write-Host "Property tests with directives:   $($stats.PropertyTestsWithDirectives)"
Write-Host "Property tests needing directives: $($stats.PropertyTestsNeedingDirectives)"

if ($WhatIf) {
    Write-Host ""
    Write-Host "Mode: PREVIEW (no changes made)" -ForegroundColor Yellow
    if ($stats.PropertyTestsNeedingDirectives -gt 0) {
        Write-Host "Run without -WhatIf to apply changes" -ForegroundColor Yellow
    }
}
elseif ($Validate) {
    Write-Host ""
    if ($stats.PropertyTestsNeedingDirectives -eq 0) {
        Write-Host "Mode: VALIDATE - All property tests have directives" -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "Mode: VALIDATE - Some property tests are missing directives" -ForegroundColor Red
        Write-Host ""
        Write-Host "Files needing fixes:" -ForegroundColor Red
        foreach ($file in $filesNeedingFixes) {
            Write-Host "  - $file" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "Run without -Validate to add missing directives" -ForegroundColor Yellow
        exit 1
    }
}
else {
    Write-Host ""
    Write-Host "Files modified:                   $($stats.FilesModified)" -ForegroundColor Green
    if ($stats.FilesModified -gt 0) {
        Write-Host ""
        Write-Host "Directives successfully added!" -ForegroundColor Green
    }
    else {
        Write-Host ""
        Write-Host "No changes needed - all property tests already have directives" -ForegroundColor Green
    }
}

Write-Host ""
