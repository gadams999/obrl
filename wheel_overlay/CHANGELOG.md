# Changelog

All notable changes to Wheel Overlay will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2024-12-29

### Added
- **About Wheel Overlay Dialog**: Accessible from system tray menu with application information
  - Displays version number read from assembly metadata
  - Clickable GitHub repository link
  - Modal dialog with fixed size and centered positioning
  - Close button and Escape key support
- **Smart Text Condensing**: Automatically hides empty positions in multi-position layouts
  - Only displays positions with configured text labels
  - Maintains original position numbers after filtering
  - Works in Vertical, Horizontal, and Grid layouts
- **Empty Position Feedback**: Visual flash animation when selecting empty positions
  - 500ms flash duration alternating between selected/non-selected colors
  - Confirms wheel input was detected even when no text is configured
  - Stops immediately when populated position is selected
  - Restarts if another empty position is selected while flashing
- **Enhanced Single Layout**: Improved handling of empty positions
  - Displays last populated position text when empty position is selected
  - Shows text in non-selected color to indicate empty position
  - Handles startup case when first position is empty
- **Test Mode**: Development mode for testing without physical hardware
  - Launch with `--test-mode` or `/test` command-line flags
  - Left/Right arrow keys simulate wheel position changes
  - Position wraps around at boundaries (0-7)
  - Yellow border indicator shows when test mode is active
- **Comprehensive Test Suite**: Property-based and integration tests
  - 51 total tests covering all new features
  - Property-based tests using FsCheck for universal correctness
  - Integration tests verifying features work together
  - All tests pass with 1 skipped (requires STA thread for WPF UI)

### Fixed
- **Test Mode Indicator**: Yellow border now only shows when test mode is enabled
  - Previously showed even when test mode was disabled
  - Fixed by removing hardcoded BorderThickness attribute
- **First-Run Text Labels**: Default text labels now display on first launch
  - Previously required opening settings and clicking Apply
  - Fixed by creating default profile with text labels on first run

### Changed
- **README.md**: Comprehensive update for v0.4.0
  - Added detailed Getting Started guide
  - Expanded Usage section with all new features
  - Enhanced Troubleshooting with common issues
  - Added Development section with build instructions
  - Added Version History section
- **Documentation**: Added steering file for keeping documentation current
  - Guidelines for when to update README
  - Checklist for documentation updates before pushing
  - Documentation quality standards

### Technical
- Added property-based testing with FsCheck (minimum 100 iterations per test)
- All code changes mapped to specific requirements for traceability
- Design document includes 14 correctness properties validated by tests
- 3,600+ lines added across 25+ files

## [0.2.0] - 2023-XX-XX

### Added
- **Layout Profiles**: Create and save multiple profiles for different cars or sims
- **Device Awareness**: Profiles linked to specific devices
- **Dynamic Fields**: Settings interface adjusts based on selected wheel capabilities
- **Application Icon**: Proper branding with application icon
- **Robust Startup**: Improved crash handling and logging

### Changed
- Enhanced settings management with profile support
- Improved error handling and recovery

## [0.1.0] - 2023-XX-XX

### Added
- Initial release of Wheel Overlay
- **Moveable Overlay**: Config mode for positioning overlay
- **Multiple Layouts**: Single, Vertical, Horizontal, and Grid layouts
- **Customizable Appearance**: Font, color, and opacity settings
- **System Tray Integration**: Minimize to tray for unobtrusive operation
- **DirectInput Support**: Works with BavarianSimTec Alpha wheel
- **Device Detection**: Shows "Not Found" message when device disconnected

[0.4.0]: https://github.com/gadams999/obrl/compare/v0.2.0...v0.4.0
[0.2.0]: https://github.com/gadams999/obrl/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/gadams999/obrl/releases/tag/v0.1.0
