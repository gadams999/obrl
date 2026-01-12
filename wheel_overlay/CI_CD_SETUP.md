# CI/CD Setup for WheelOverlay MSI Installer

## GitHub Actions Workflow

The `.github/workflows/release.yml` workflow has been updated to build the MSI installer using WiX v4.0.4.

### Workflow Triggers
- Push to `main` or `release/v0.2.0` branches
- Pull requests to `main`
- Manual workflow dispatch

### Build Process

#### 1. Build and Test Job
- Runs on Windows
- Restores .NET dependencies
- Builds the project in Release configuration
- Runs tests with configuration based on trigger:
  - **Pull Requests**: Uses FastTests configuration (10 iterations per property test)
  - **Push to Main**: Uses Release configuration (100 iterations per property test)
- Validates all property tests have correct preprocessor directives

#### 2. Package and Release Job
- Installs WiX Toolset v4.0.4
- Runs `build_msi.ps1` script which:
  - Builds .NET application (self-contained, 249 files)
  - Copies all files to Package directory
  - Harvests files dynamically
  - Builds MSI with custom UI dialogs
- Extracts version from WheelOverlay.csproj
- Uploads MSI as artifact
- Creates GitHub release (on main branch only)

### Key Changes from Previous Setup

**Before (WiX v6):**
- Used WiX v6.0.2
- Manual file copying
- Built-in WixUI extension
- Complex multi-step process

**After (WiX v4):**
- Uses WiX v4.0.4 (permissive Ms-RL license)
- Single `build_msi.ps1` script
- Custom UI dialogs (bypasses v4 UI extension bug)
- Self-contained .NET deployment
- Automated file harvesting

### Artifacts

**MSI Installer:**
- Name: `WheelOverlay-{VERSION}-installer`
- Location: `wheel_overlay/Package/WheelOverlay.msi`
- Retention: 7 days

**Release Assets:**
- Attached to GitHub release on main branch
- Tagged as `v{VERSION}`

### Local Testing

To test the build process locally:

```powershell
cd wheel_overlay
.\build_msi.ps1
```

This will:
1. Build the .NET application
2. Copy files to Package directory
3. Generate component list dynamically
4. Build MSI with custom UI

### Requirements

**GitHub Actions Runner:**
- Windows (windows-latest)
- .NET 8.0 SDK (installed via setup-dotnet action)
- WiX v4.0.4 (installed via dotnet tool)

**Local Development:**
- Windows OS
- .NET 8.0 SDK
- WiX v4.0.4: `dotnet tool install -g wix --version 4.0.4`
- PowerShell

### Custom UI Dialogs

The installer includes custom UI dialogs defined in `Package/CustomUI.wxs`:
- Welcome Dialog
- License Agreement Dialog (shows LICENSE.rtf)
- Destination Folder Dialog (with Browse button)
- Ready to Install Dialog
- Progress Dialog
- Complete Dialog
- Cancel Confirmation Dialog

These custom dialogs bypass the WiX v4.0.5 UI extension bug where built-in dialogs are inaccessible.

### Version Management

Version is read from `WheelOverlay/WheelOverlay.csproj`:
```xml
<PropertyGroup>
  <Version>0.2.0</Version>
</PropertyGroup>
```

Update this version to trigger new releases.

### Troubleshooting

**Build Fails:**
- Check WiX v4.0.4 is installed
- Verify all source files exist in Publish directory
- Check CustomUI.wxs syntax

**MSI Not Created:**
- Check build_msi.ps1 output
- Verify Package directory has all files
- Check for WiX compilation errors

**UI Dialogs Not Showing:**
- Verify CustomUI.wxs is included in build
- Check that both Package.wxs and CustomUI.wxs are compiled together
- Ensure LICENSE.rtf exists in parent directory

**Property Test Validation Fails:**
- Error indicates property tests are missing preprocessor directives
- Run `.\Scripts\Add-PropertyTestDirectives.ps1 -WhatIf` to preview required changes
- Run `.\Scripts\Add-PropertyTestDirectives.ps1` to automatically add directives
- Commit the updated test files

**Tests Fail in CI but Pass Locally:**
- Check which configuration was used (FastTests vs Release)
- PR builds use FastTests (10 iterations), merge builds use Release (100 iterations)
- Run locally with same configuration: `dotnet test --configuration FastTests` or `dotnet test --configuration Release`
- More iterations may reveal edge cases not found with fewer iterations

## Test Configuration System

### Overview

The CI/CD pipeline uses different test configurations to optimize build times while maintaining thorough validation:

- **FastTests Configuration**: 10 iterations per property test (~30-60 seconds)
- **Release Configuration**: 100 iterations per property test (~3-5 minutes)

### When Each Configuration is Used

**Pull Request Builds:**
- Trigger: PR created or updated
- Configuration: FastTests
- Purpose: Fast feedback for developers
- Target: Complete in under 120 seconds
- Iteration Count: 10 per property test

**Merge Builds:**
- Trigger: Push to main branch
- Configuration: Release
- Purpose: Thorough validation before deployment
- Target: Complete validation regardless of time
- Iteration Count: 100 per property test

### Pipeline Steps

#### 1. Validation Step

Before building, the pipeline validates that all property tests have correct preprocessor directives:

```yaml
- name: Validate Property Tests
  run: |
    cd wheel_overlay
    .\Scripts\Validate-PropertyTests.ps1
```

**What it checks:**
- All `[Property(MaxTest = X)]` attributes have conditional compilation directives
- Directives follow the correct pattern (`#if FAST_TESTS` / `#else` / `#endif`)
- MaxTest values are 10 for FAST_TESTS and 100 for else block

**If validation fails:**
- Build stops with clear error message
- Output lists files needing fixes
- Developer runs `.\Scripts\Add-PropertyTestDirectives.ps1` to fix
- Developer commits updated files

#### 2. Build Step

Builds the project with appropriate configuration:

```yaml
# For PRs
- name: Build
  run: dotnet build --configuration FastTests

# For merges
- name: Build
  run: dotnet build --configuration Release
```

#### 3. Test Step

Runs tests with the configuration used for building:

```yaml
# For PRs
- name: Test
  run: dotnet test --configuration FastTests --no-build

# For merges
- name: Test
  run: dotnet test --configuration Release --no-build
```

**Test output includes:**
- Configuration name (FastTests or Release)
- Iteration count per property test
- Total test execution time
- Pass/fail status for each test

### Configuration in Workflow File

The workflow uses conditional logic to select configuration:

```yaml
env:
  TEST_CONFIGURATION: ${{ github.event_name == 'pull_request' && 'FastTests' || 'Release' }}

steps:
  - name: Build
    run: dotnet build --configuration ${{ env.TEST_CONFIGURATION }}
  
  - name: Test
    run: dotnet test --configuration ${{ env.TEST_CONFIGURATION }} --no-build
```

### Monitoring Build Performance

**Target Times:**
- PR builds: < 120 seconds total
- Merge builds: < 300 seconds total

**If builds are too slow:**
1. Check test execution time in build logs
2. Identify slow tests
3. Consider optimizing test logic or reducing custom iteration counts
4. Verify FastTests configuration is being used for PRs

### Troubleshooting Configuration Issues

**Issue: Wrong configuration used**

Check workflow logs for configuration selection:
```
Configuration: FastTests
Iteration count: 10
```

**Issue: Tests always run with 100 iterations**

1. Verify `FAST_TESTS` symbol is defined in WheelOverlay.Tests.csproj
2. Check workflow specifies `--configuration FastTests`
3. Verify preprocessor directives in test files

**Issue: Validation step fails**

1. Review validation output for list of files needing fixes
2. Run locally: `.\Scripts\Validate-PropertyTests.ps1`
3. Fix with: `.\Scripts\Add-PropertyTestDirectives.ps1`
4. Commit and push updated files

**Issue: Different results between configurations**

This is expected - more iterations find more edge cases:
1. Note the failing input from test output
2. Add unit test for that specific case
3. Fix the code to handle the edge case
4. Verify both configurations pass

