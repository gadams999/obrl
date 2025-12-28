# Version Management Guidelines

## Version Updates for Releases

When creating branches intended to be merged to `main` for release, **always update the version number** in the project file before pushing or creating a PR.

### WheelOverlay Project

For the `wheel_overlay` project, update the version in:
- **File**: `wheel_overlay/WheelOverlay/WheelOverlay.csproj`
- **Properties to update**:
  - `<Version>X.Y.Z</Version>`
  - `<AssemblyVersion>X.Y.Z.0</AssemblyVersion>`
  - `<FileVersion>X.Y.Z.0</FileVersion>`

### When to Update

- **Feature branches** (`feature/*`): Update version when ready to merge to main
- **Fix branches** (`fix/*`): Update version before creating PR to main
- **Release branches** (`release/*`): Update version at the start of the release branch

### Version Numbering

Follow semantic versioning (MAJOR.MINOR.PATCH):
- **MAJOR**: Breaking changes
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, backward compatible

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

# 4. Commit version update with changes
git add wheel_overlay/WheelOverlay/WheelOverlay.csproj
git commit -m "Bump version to 0.3.1"

# 5. Push and create PR
git push -u origin fix/0.3.1-some-fix

# 6. After PR is merged to main, the release workflow runs automatically
```
