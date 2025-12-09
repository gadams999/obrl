# WiX v3 Toolset Setup Instructions

## Current Situation
- You have WiX v6.0.2 installed
- The converted Package.wxs now uses WiX v3 syntax (classic WiX)
- WiX v3 has the most permissive license and is widely used

## Why WiX v3?
- WiX v3 is the classic, stable version with MIT-like license
- WiX v4+ introduced license changes that prompted this conversion
- v3 is still widely used and fully supported for production use

## Installation Options

### Option 1: Install WiX v3 Toolset (Recommended)
Download and install from: https://github.com/wixtoolset/wix3/releases/tag/wix3141rtm

Direct link: https://github.com/wixtoolset/wix3/releases/download/wix3141rtm/wix314.exe

After installation:
- WiX v3 tools will be in `C:\Program Files (x86)\WiX Toolset v3.14\bin\`
- Add to PATH or use full path in build scripts

### Option 2: Use .NET Tool (dotnet-wix)
```powershell
dotnet tool install --global wix3
```

### Option 3: Use Both (Recommended for Development)
Keep WiX v6 for other projects, install v3 for this project.
Use explicit paths in build scripts to choose which version.

## Building with WiX v3

### Manual Build:
```powershell
cd Package
candle.exe Package.wxs
light.exe -ext WixUIExtension Package.wixobj -o WheelOverlay.msi
```

### Using the build script:
The `build_msi.ps1` script will need to be updated to use WiX v3 commands (candle/light) instead of WiX v4+ commands (wix build).

## Next Steps
1. Install WiX v3 toolset
2. Update build_msi.ps1 to use candle.exe and light.exe
3. Test the build
4. Install and verify the MSI

