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
- Runs tests

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

