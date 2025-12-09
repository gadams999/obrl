# WiX Conversion Status - Ready for Testing

## What Was Done
Converted the WheelOverlay installer from WiX v6 to **WiX v3** (classic WiX).

## Why WiX v3 Instead of v4?
- WiX v3 is the classic, stable version with the most permissive open-source license
- WiX v4+ introduced license changes
- v3 is still widely used in production and fully supported
- v3 has the mature, battle-tested tooling

## Files Modified
1. **Package/Package.wxs** - Converted to WiX v3 syntax
   - Changed namespace to `http://schemas.microsoft.com/wix/2006/wi`
   - Uses `<Product>` root element (v3 standard)
   - Explicit directory hierarchy with TARGETDIR
   - All components have Guid="*" for auto-generation
   
2. **Package/.wix/wix.json** - Updated extension version to 3.14.1

3. **build_msi.ps1** - New comprehensive build script
   - Builds .NET application
   - Copies files to Package directory
   - Compiles WiX source with candle.exe
   - Links and creates MSI with light.exe

## Current Blocker
**You need WiX v3 Toolset installed**

You currently have WiX v6.0.2 installed, but the converted files use v3 syntax.

## Installation Instructions

### Quick Install:
1. Download WiX v3.14: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe
2. Run the installer
3. WiX v3 tools will be added to your PATH automatically

### Verify Installation:
```powershell
candle.exe -?
light.exe -?
```

## Testing the Build

### Full Build and Test:
```powershell
cd wheel_overlay
.\build_msi.ps1
```

This will:
1. Build the .NET application (self-contained)
2. Copy all required files to Package directory
3. Compile and link the MSI installer
4. Output: `Package\WheelOverlay.msi`

### Install the MSI:
```powershell
# Interactive install (shows UI dialogs)
msiexec /i Package\WheelOverlay.msi

# Install with detailed logging
msiexec /i Package\WheelOverlay.msi /l*v install.log

# Silent install
msiexec /i Package\WheelOverlay.msi /quiet
```

### Uninstall:
```powershell
# Via Add/Remove Programs (interactive)
# Or via command line:
msiexec /x Package\WheelOverlay.msi
```

## What to Test

### Installation Testing:
- [ ] MSI builds without errors
- [ ] Interactive installer shows license agreement
- [ ] Can customize installation directory
- [ ] All files copied correctly
- [ ] Start Menu shortcut created
- [ ] Desktop shortcut created
- [ ] Application launches from shortcuts
- [ ] Application icon appears correctly

### Uninstallation Testing:
- [ ] Uninstall via Add/Remove Programs works
- [ ] All files removed
- [ ] Shortcuts removed
- [ ] Registry entries cleaned up
- [ ] No leftover folders

### Upgrade Testing:
- [ ] Install older version first
- [ ] Install newer version
- [ ] Verify upgrade works (no duplicate entries)
- [ ] Settings/data preserved if applicable

## Interactive UI Features
The installer includes full interactive dialogs:
- **Welcome Dialog** - Introduction
- **License Agreement** - Must accept LICENSE.rtf
- **Installation Directory** - User can customize location
- **Progress Dialog** - Shows installation progress
- **Completion Dialog** - Optional "Launch Wheel Overlay" checkbox
- **Uninstall Support** - Full interactive uninstall

## Preserved from Original
✓ UpgradeCode GUID (critical for upgrades)
✓ Shortcut GUIDs
✓ All application files and dependencies
✓ License file reference
✓ Icon configuration
✓ Registry entries
✓ Add/Remove Programs settings

## Next Steps
1. Install WiX v3 Toolset
2. Run `.\build_msi.ps1`
3. Test installation
4. Test uninstallation
5. Test upgrade scenario
6. Commit changes to git

## Branch
Current branch: `wix-v4-conversion` (note: actually converted to v3, can rename if desired)

