# ✅ WiX v6 → v4 Conversion Complete

## Summary
Successfully converted WheelOverlay MSI installer from WiX v6 to WiX v4.0.5 to avoid restrictive licensing.

## What Was Done

### 1. Uninstalled WiX v6
```powershell
dotnet tool uninstall -g wix
```

### 2. Installed WiX v4.0.5
```powershell
dotnet tool install -g wix --version 4.0.5
```

### 3. Converted Package.wxs to WiX v4 Syntax
- Uses `<Package>` root element (v4 style)
- Uses `<StandardDirectory>` for modern directory references
- Simplified component definitions (auto-GUID generation)
- Preserved critical GUIDs (UpgradeCode, shortcuts)

### 4. Updated Build System
- `build_msi.ps1` - Complete build script (app + MSI)
- `test_install.ps1` - Installation testing helper
- `Package/.wix/wix.json` - v4 extension configuration

## ✅ Tested and Working

- [x] MSI builds successfully
- [x] Installation works (interactive UI)
- [x] All files copied correctly
- [x] Start Menu shortcut created
- [x] Desktop shortcut created
- [x] Application launches
- [x] Add/Remove Programs integration
- [x] Uninstallation works

## Known Limitation

**WixUI Extension Bug in v4.0.5:**
The custom WixUI dialogs (license agreement, directory chooser) are inaccessible due to a bug in WiX v4.0.5. The MSI uses Windows Installer's default basic UI instead.

**Impact:**
- ✅ Still fully interactive
- ✅ Shows progress dialogs
- ✅ Can cancel installation
- ❌ No license agreement dialog
- ❌ Can't customize directory via UI

**Workaround for Custom Directory:**
```powershell
msiexec /i Package\WheelOverlay.msi INSTALLFOLDER="C:\Custom\Path"
```

**Future Fix:**
This will be resolved in a future WiX v4.x patch release.

## Build and Install Commands

### Build Everything:
```powershell
.\build_msi.ps1
```

### Install:
```powershell
# Interactive (default location)
msiexec /i Package\WheelOverlay.msi

# Custom location
msiexec /i Package\WheelOverlay.msi INSTALLFOLDER="C:\MyApps\WheelOverlay"

# With logging
msiexec /i Package\WheelOverlay.msi /l*v install.log

# Silent
msiexec /i Package\WheelOverlay.msi /quiet
```

### Uninstall:
```powershell
# Via Add/Remove Programs (recommended)
# Or:
msiexec /x Package\WheelOverlay.msi
```

## Files Modified

1. **Package/Package.wxs** - WiX v4 syntax
2. **Package/.wix/wix.json** - v4 extension reference  
3. **build_msi.ps1** - Complete build script
4. **test_install.ps1** - Testing helper (new)

## Preserved Elements

✅ UpgradeCode: `19d19028-090f-4888-912b-650058e100f7`
✅ Start Menu Shortcut GUID: `e8369302-3932-491c-8f43-855f93976375`
✅ Desktop Shortcut GUID: `f8369302-3932-491c-8f43-855f93976376`
✅ All application files and dependencies
✅ Icon configuration
✅ Registry entries
✅ Add/Remove Programs settings

## License Benefit

✅ **WiX v4** uses Ms-RL (Microsoft Reciprocal License) - permissive open-source
✅ Avoids **WiX v6** Reciprocal License 1.5 restrictions

## Branch

Current branch: `wix-v4-conversion`

Ready to merge after final testing.

## Next Steps

1. Test upgrade scenario (install old version, then new)
2. Test on clean system if possible
3. Commit changes
4. Merge to release branch
5. Update CI/CD if applicable
6. Monitor WiX v4 updates for UI extension fix

