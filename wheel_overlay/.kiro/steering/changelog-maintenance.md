---
inclusion: always
---

# Changelog Maintenance Guidelines

## Overview

The CHANGELOG.md file is the single source of truth for release notes. It MUST be updated as part of every feature or fix branch before merging to main.

## When to Update CHANGELOG.md

You MUST update the CHANGELOG.md file in these situations:

1. **When Starting a New Feature Branch**: Add an "Unreleased" section if it doesn't exist
2. **When Implementing Features**: Add entries under the appropriate category as you complete them
3. **Before Completing a Spec**: Ensure all spec changes are documented
4. **Before Merging to Main**: Finalize all entries and ensure completeness
5. **When Fixing Bugs**: Add entry under "Fixed" section
6. **When Making Breaking Changes**: Add entry under "Changed" or "Removed" with clear migration notes

## CHANGELOG.md Format

Follow the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format:

### Structure
```markdown
# Changelog

## [Unreleased]
### Added
- New features

### Changed
- Changes to existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Security fixes

## [X.Y.Z] - YYYY-MM-DD
(Same structure as Unreleased)
```

### Categories

- **Added**: New features, capabilities, or functionality
- **Changed**: Changes to existing functionality (not bug fixes)
- **Deprecated**: Features that will be removed in future versions
- **Removed**: Features that have been removed
- **Fixed**: Bug fixes
- **Security**: Security vulnerability fixes

## Writing Good Changelog Entries

### Guidelines

1. **User-Focused**: Write from the user's perspective, not the developer's
2. **Clear and Concise**: One line per change, with sub-bullets for details if needed
3. **Action-Oriented**: Start with a verb (Added, Fixed, Changed, etc.)
4. **Specific**: Include enough detail to understand the change
5. **Link to Issues**: Reference issue numbers when applicable

### Good Examples

✅ **Good:**
```markdown
### Added
- **About Dialog**: Accessible from system tray with version info and GitHub link
  - Displays version number from assembly metadata
  - Clickable GitHub repository link
  - Modal dialog with Escape key support
```

✅ **Good:**
```markdown
### Fixed
- **First-Run Text Labels**: Default text labels now display on first launch
  - Previously required opening settings and clicking Apply
  - Fixed by creating default profile with text labels on first run
```

### Bad Examples

❌ **Bad:**
```markdown
### Added
- Added some stuff to the code
```
*Too vague - what was added?*

❌ **Bad:**
```markdown
### Fixed
- Fixed bug in OverlayViewModel.cs line 42
```
*Too technical - what user-facing issue was fixed?*

❌ **Bad:**
```markdown
### Changed
- Refactored InputService
```
*Internal change - not user-facing unless it affects behavior*

## Workflow

### Starting a New Feature/Fix Branch

1. **Check for Unreleased Section**:
   ```markdown
   ## [Unreleased]
   ```
   If it doesn't exist, add it at the top of the changelog.

2. **Add Placeholder Entries** (optional):
   ```markdown
   ## [Unreleased]
   ### Added
   - [Feature name]: Brief description (WIP)
   ```

### During Development

1. **Update as You Go**: Add entries when you complete features or fixes
2. **Use Sub-bullets**: Add details under main entries
3. **Keep It Current**: Don't wait until the end to update

### Before Merging

1. **Review All Changes**: Ensure every user-facing change is documented
2. **Check Completeness**: Verify all features, fixes, and changes are listed
3. **Proofread**: Check for typos, clarity, and consistency
4. **Remove WIP Markers**: Remove any "WIP" or "TODO" markers
5. **Add Technical Section** (optional): Add a "Technical" subsection for developer-relevant details

### When Releasing

1. **Change Unreleased to Version Number**:
   ```markdown
   ## [0.4.0] - 2024-12-29
   ```

2. **Add Comparison Link** at bottom:
   ```markdown
   [0.4.0]: https://github.com/gadams999/obrl/compare/v0.3.0...v0.4.0
   ```

3. **Create New Unreleased Section** for next version:
   ```markdown
   ## [Unreleased]
   ```

## Integration with Other Processes

### With README.md Updates
- CHANGELOG.md is the detailed record
- README.md Version History is the summary
- Keep them synchronized but CHANGELOG.md is more detailed

### With GitHub Releases
- Copy the relevant version section from CHANGELOG.md
- Paste into GitHub Release notes
- Add any additional release-specific information (download links, etc.)

### With PR Descriptions
- PR description can be more detailed than CHANGELOG
- CHANGELOG should be user-focused summary
- PR description can include technical implementation details

## Checklist Before Merging

Before merging any feature or fix branch, verify:

- [ ] CHANGELOG.md has an [Unreleased] section
- [ ] All user-facing changes are documented
- [ ] Entries are in the correct category (Added, Fixed, Changed, etc.)
- [ ] Entries are clear and user-focused
- [ ] Sub-bullets provide necessary details
- [ ] No WIP or TODO markers remain
- [ ] Spelling and grammar are correct
- [ ] Format follows Keep a Changelog standard
- [ ] Technical details are in a separate subsection if needed

## Examples by Change Type

### New Feature
```markdown
### Added
- **Test Mode**: Development mode for testing without physical hardware
  - Launch with `--test-mode` or `/test` command-line flags
  - Left/Right arrow keys simulate wheel position changes
  - Yellow border indicator shows when test mode is active
```

### Bug Fix
```markdown
### Fixed
- **Test Mode Indicator**: Yellow border now only shows when test mode is enabled
  - Previously showed even when test mode was disabled
  - Fixed by removing hardcoded BorderThickness attribute
```

### Breaking Change
```markdown
### Changed
- **Settings File Format**: Updated to new JSON schema (BREAKING CHANGE)
  - Old settings files will be automatically migrated on first run
  - Backup your settings file before upgrading if needed
  - See migration guide in docs/MIGRATION.md
```

### Deprecation
```markdown
### Deprecated
- **Legacy Layout API**: Old layout system will be removed in v0.6.0
  - Use new profile-based layouts instead
  - Migration guide available in documentation
```

## Common Mistakes to Avoid

1. **Don't Include Internal Changes**: Refactoring, code cleanup, test additions (unless they affect users)
2. **Don't Use Technical Jargon**: Avoid class names, method names, file paths
3. **Don't Be Vague**: "Fixed bugs" or "Improved performance" without specifics
4. **Don't Forget Sub-bullets**: Major features need details
5. **Don't Mix Categories**: Keep Added, Fixed, Changed separate
6. **Don't Skip Dates**: Always include release date in format YYYY-MM-DD

## Automation Reminder

When you:
- Complete a spec → Update CHANGELOG.md
- Fix a bug → Update CHANGELOG.md
- Merge a PR → Ensure CHANGELOG.md is updated
- Create a release → Copy from CHANGELOG.md to GitHub Release

The CHANGELOG.md is your release notes source of truth!
