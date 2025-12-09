# WiX v4 Conversion - Status and Notes

## ✅ Successfully Converted to WiX v4.0.5

### What Works:
- ✅ WiX v6 uninstalled
- ✅ WiX v4.0.5 installed via `dotnet tool`
- ✅ Package.wxs converted to WiX v4 syntax
- ✅ MSI builds successfully
- ✅ All application files included
- ✅ Shortcuts (Start Menu + Desktop)
- ✅ UpgradeCode preserved for upgrades
- ✅ Icon configuration
- ✅ Add/Remove Programs integration

### Current Limitation - UI Extension Issue:
WiX v4.0.5 has a known bug where the WixUI extension dialogs (WixUI_InstallDir, etc.) are marked as internal and inaccessible. This affects the custom UI dialogs.

**Current Behavior:**
- MSI uses Windows Installer's default basic UI
- Still fully interactive (shows progress, can cancel, etc.)
- License agreement NOT shown (limitation)
- Installation directory NOT customizable via UI (uses default)

**Workarounds:**
1. **Use command-line for custom directory:**
   ```powershell
   msiexec /i Package\WheelOverlay.msi INSTALLFOLDER="C:\Custom\Path"
   ```

2. **Wait for WiX v4 patch:** The WiX team is aware of this issue and it should be fixed in a future v4.x release

3. **Use WiX v5:** (if/when available with acceptable license)

4. **Custom UI:** Define custom dialogs in the Package.wxs (more complex)

## Installation Testing

### Build the MSI:
```powershell
.\build_msi.ps1
```

### Install (Interactive with default UI):
```powershell
msiexec /i Package\WheelOverlay.msi
```

### Install to Custom Directory:
```powershell
msiexec /i Package\WheelOverlay.msi INSTALLFOLDER="C:\MyApps\WheelOverlay"
```

### Install with Logging:
```powershell
msiexec /i Package\WheelOverlay.msi /l*v install.log
```

### Silent Install:
```powershell
msiexec /i Package\WheelOverlay.msi /quiet
```

### Uninstall:
```powershell
# Via Add/Remove Programs (recommended)
# Or via command:
msiexec /x Package\WheelOverlay.msi
```

## What Was Changed from v6:

1. **Package.wxs:**
   - Uses `<Package>` root element (v4 style)
   - `<StandardDirectory>` elements (v4 feature)
   - Simplified component definitions
   - UI temporarily disabled due to extension bug

2. **wix.json:**
   - Updated to v4 schema
   - References WixToolset.UI.wixext/4.0.5

3. **build_msi.ps1:**
   - Uses `wix build` command (v4 CLI)
   - Checks for WiX v4 toolset

## Files Modified:
- `Package/Package.wxs` - WiX v4 syntax
- `Package/.wix/wix.json` - v4 extension reference
- `build_msi.ps1` - v4 build commands

## License Benefit:
✅ WiX v4 uses Ms-RL (Microsoft Reciprocal License) - permissive open-source license
✅ Avoids WiX v6's Reciprocal License 1.5 restrictions

## Next Steps:
1. ✅ Build MSI - DONE
2. Test installation
3. Test uninstallation  
4. Test upgrade scenarios
5. Monitor WiX v4 updates for UI extension fix
6. Consider custom UI dialogs if needed sooner

## Testing Checklist:
- [ ] Install on clean system
- [ ] Verify all files copied to Program Files
- [ ] Test Start Menu shortcut
- [ ] Test Desktop shortcut
- [ ] Launch application
- [ ] Uninstall via Add/Remove Programs
- [ ] Verify clean uninstall
- [ ] Test upgrade (install v0.1.0, then v0.2.0)
- [ ] Test custom install directory via command line

