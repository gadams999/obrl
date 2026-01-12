# WheelOverlay Build Commands

## Prerequisites
- .NET 10.0 SDK
- WiX Toolset v4.0.5: `dotnet tool install --global wix --version 4.0.5`

## Essential Build Commands

### 1. Build Application Only
```powershell
dotnet build WheelOverlay/WheelOverlay.csproj -c Release
```

### 2. Run Tests
```powershell
dotnet test WheelOverlay.Tests/WheelOverlay.Tests.csproj
```

Run tests with detailed output:
```powershell
dotnet test WheelOverlay.Tests/WheelOverlay.Tests.csproj --logger "console;verbosity=detailed"
```

Run tests and generate coverage report:
```powershell
dotnet test WheelOverlay.Tests/WheelOverlay.Tests.csproj --collect:"XPlat Code Coverage"
```

### 3. Publish Self-Contained Application
```powershell
dotnet publish WheelOverlay/WheelOverlay.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:TreatWarningsAsErrors=true -o Publish
```

### 4. Build MSI Installer (Complete Process)
```powershell
.\build_msi.ps1
```

### 5. Build Single File Executable + ZIP
```powershell
.\build_release.ps1
```

## Manual MSI Build Steps
If you need to build MSI manually:

1. Publish application:
   ```powershell
   dotnet publish WheelOverlay/WheelOverlay.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o Publish
   ```

2. Copy files to Package directory:
   ```powershell
   Copy-Item "Publish\*" -Destination Package -Recurse -Force -Exclude "*.pdb"
   Copy-Item "LICENSE.txt" -Destination Package -Force
   ```

3. Build MSI:
   ```powershell
   cd Package
   wix build Package.wxs CustomUI.wxs -o WheelOverlay.msi
   ```

## GitHub Actions
The CI/CD pipeline uses these same commands in `.github/workflows/release.yml`.