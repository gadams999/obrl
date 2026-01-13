# Version Management Guidelines

## Critical: Update Version When Creating New Branches

**IMPORTANT**: When creating a new feature or fix branch, **immediately update the version number** in all related files before making any other changes. This ensures all local builds and testing reflect the correct version.

### Files to Update

For the `wheel_overlay` project, update the version in these files:

1. **WheelOverlay.csproj** - `wheel_overlay/WheelOverlay/WheelOverlay.csproj`
   - `<Version>X.Y.Z</Version>`
   - `<AssemblyVersion>X.Y.Z.0</AssemblyVersion>`
   - `<FileVersion>X.Y.Z.0</FileVersion>`

2. **Package.wxs** - `wheel_overlay/Package/Package.wxs`
   - `<Product ... Version="X.Y.Z" ...>`
   - Update the `Id` GUID to a new GUID (required for MSI upgrades)

3. **build_release.ps1** - `wheel_overlay/build_release.ps1`
   - `$zipPath = ".\WheelOverlay_vX.Y.Z.zip"`

### Workflow: Creating a New Branch

```bash
# 1. Create branch with version in name
git checkout -b fix/v0.5.3-exit-handling

# 2. IMMEDIATELY update version in all files
# - WheelOverlay.csproj: 0.5.2 → 0.5.3
# - Package.wxs: Version="0.5.2" → Version="0.5.3" and new GUID
# - build_release.ps1: v0.5.2.zip → v0.5.3.zip

# 3. Commit version bump as first commit
git add WheelOverlay/WheelOverlay.csproj Package/Package.wxs build_release.ps1
git commit -m "chore: bump version to 0.5.3"

# 4. Now make your feature/fix changes
# ... code changes ...

# 5. All local builds will now show version 0.5.3
```

### Why Update Version First?

- **Local builds** reflect the correct version immediately
- **MSI installers** built locally have the correct version
- **About box** shows the correct version during testing
- **Consistency** between development and release
- **Traceability** - easier to identify which build you're testing

### Version Numbering

Follow semantic versioning (MAJOR.MINOR.PATCH):
- **MAJOR**: Breaking changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

### Package.wxs GUID Update

When updating the version in Package.wxs, you **must** also update the Product Id GUID:

```xml
<!-- OLD -->
<Product Id="12345678-1234-1234-1234-123456789012" Version="0.5.2" ...>

<!-- NEW - Generate new GUID -->
<Product Id="87654321-4321-4321-4321-210987654321" Version="0.5.3" ...>
```

This is required for Windows Installer to recognize it as an upgrade. You can generate a new GUID with:
- PowerShell: `[guid]::NewGuid()`
- Online: https://www.guidgenerator.com/

### Version Updates for Releases

When creating branches intended to be merged to `main` for release, **always update the version number** in the project file before pushing or creating a PR.

### WheelOverlay Project

For the `wheel_overlay` project, update the version in:
- **File**: `wheel_overlay/WheelOverlay/WheelOverlay.csproj`
- **Properties to update**:
  - `<Version>X.Y.Z</Version>`
  - `<AssemblyVersion>X.Y.Z.0</AssemblyVersion>`
  - `<FileVersion>X.Y.Z.0</FileVersion>`

**Note**: The `AssemblyVersion` property is automatically read by the About box dialog (`AboutWindow.xaml.cs`) to display the version to users. Updating this property ensures the About box shows the correct version.

**Important**: The version is embedded into the assembly at **build time**. After updating the .csproj file, you must rebuild the project for the About box to show the new version:
- `dotnet build` - Rebuilds with new version
- `dotnet run` - Automatically rebuilds before running
- The version change takes effect immediately after rebuild

### When to Update

- **Feature branches** (`feature/*`): Update version when ready to merge to main
- **Fix branches** (`fix/*`): Update version before creating PR to main
- **Release branches** (`release/*`): Update version at the start of the release branch

### GitHub Release Workflow

The GitHub Actions workflow (`.github/workflows/release.yml`) automatically:
1. Reads the version from the `.csproj` file
2. Builds the MSI installer
3. Creates a GitHub release with tag `vX.Y.Z`
4. Uploads the MSI to the release

**Important**: If the version is not updated, the workflow will create a release with the old version number or fail if the tag already exists.

### Example Workflow

```bash
# 1. Create fix branch
git checkout -b fix/0.3.1-some-fix

# 2. Make your changes
# ... code changes ...

# 3. Update version in WheelOverlay.csproj
# Change Version from 0.3.0 to 0.3.1
# Change AssemblyVersion from 0.3.0.0 to 0.3.1.0
# Change FileVersion from 0.3.0.0 to 0.3.1.0
# This will automatically update the About box to show "Version 0.3.1"

# 4. Commit version update with changes
git add wheel_overlay/WheelOverlay/WheelOverlay.csproj
git commit -m "Bump version to 0.3.1"

# 5. Push and create PR
git push -u origin fix/0.3.1-some-fix

# 6. After PR is merged to main, the release workflow runs automatically
```

### Verification

After updating the version:
1. Build the project
2. Run the application
3. Open the About box (System Tray → About Wheel Overlay)
4. Verify the version displayed matches your update
