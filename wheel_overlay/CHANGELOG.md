# Changelog

All notable changes to Wheel Overlay will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.3](https://github.com/gadams999/obrl/compare/v0.5.2...v0.5.3) (2026-01-13)

> Critical bug fix release resolving application exit issues and implementing single-instance enforcement.

### Upgrade Steps
* No action required - all changes are backward compatible
* Existing settings and profiles will continue to work
* MSI installer will automatically upgrade from 0.5.2

### Breaking Changes
* None

### New Features
* **Single Instance Enforcement**: Application now prevents multiple instances from running
  - Uses Mutex-based detection to ensure only one instance runs at a time
  - Second/subsequent launch attempts exit silently without UI or logging
  - Prevents duplicate system tray icons and resource conflicts
* **Automatic Version Management**: Version number now read from assembly
  - Single source of truth in WheelOverlay.csproj
  - About dialog automatically displays correct version after rebuild
  - Eliminates manual version synchronization across files

### Bug Fixes
* **System Tray Exit Freeze**: Fixed application freeze when clicking Exit from system tray menu
  - Implemented deferred shutdown using BeginInvoke to prevent context menu blocking
  - Context menu now closes immediately before shutdown begins
  - Proper cleanup order: child windows → main window → tray icon
* **Taskbar Close Not Exiting**: Fixed issue where closing from taskbar left app running in tray
  - MainWindow_Closing event now triggers proper application shutdown
  - Both taskbar close and tray menu exit use same cleanup path
* **Modal Dialog Blocking Exit**: Fixed hang when exiting with About or Settings window open
  - Application now tracks and closes all child windows before shutdown
  - AboutWindow and SettingsWindow properly closed during exit sequence
  - Prevents orphaned modal dialogs blocking shutdown
* **Resource Cleanup**: Improved disposal order to prevent race conditions
  - Event handlers removed before disposal to prevent callbacks
  - Context menu disposed separately from NotifyIcon
  - All cleanup operations wrapped in try-catch with logging

### Performance Improvements
* None

### Other Changes
* **Test Suite**: Updated tests to validate assembly version reading
  - AboutWindowTests now uses dynamic version checking
  - VersionInfoTests validates assembly version format
  - Build tests refactored to parameterized Theory pattern
  - All 287 tests passing
* **Documentation**: Enhanced version management steering documentation
  - Added workflow guidance for version updates on new branches
  - Documented all files requiring version updates
  - Included best practices for version synchronization
* **Code Quality**: Added ShutdownMode="OnExplicitShutdown" to App.xaml
  - Prevents premature shutdown when windows close
  - Gives application full control over lifecycle

## [0.5.2](https://github.com/gadams999/obrl/compare/v0.5.0...v0.5.2) (2026-01-12)

> Major upgrade release migrating to .NET 10, implementing comprehensive automated testing, and fixing critical vertical layout bug.

### Upgrade Steps
* No action required - all changes are backward compatible
* Existing profiles and settings will continue to work
* .NET 10 runtime will be installed automatically by the MSI installer

### Breaking Changes
* None

### New Features
* **Comprehensive Test Suite**: 100+ automated tests covering all functionality
  - Unit tests for core components
  - Property-based tests using FsCheck (100+ iterations per test)
  - Integration tests for end-to-end workflows
  - UI automation tests for system tray and mouse interactions
  - Test coverage for all layout modes, settings, and error handling
* **Enhanced Error Handling**: Improved logging and error recovery
  - Detailed error messages with stack traces
  - Better handling of missing or corrupted configuration files
  - Graceful degradation when DirectInput device is not found

### Bug Fixes
* **Vertical Layout Crash**: Fixed crash when selecting vertical layout on fresh install
  - Added null-safety checks to OverlayViewModel
  - Added FallbackValue to XAML bindings
  - Implemented LayoutValidator for pre-render validation
  - Ensured default profile creation on first run
  - Added comprehensive logging for layout errors
* **Settings Initialization**: Fixed issues with missing or null settings on first run
  - Application now creates valid default settings automatically
  - All layout modes work correctly on fresh installations

### Performance Improvements
* None

### Other Changes
* **Framework Upgrade**: Migrated from .NET 8 to .NET 10
  - Updated all project files to target net10.0-windows
  - Updated NuGet packages to .NET 10 compatible versions
  - Verified all existing functionality works on .NET 10
* **MSI Installer**: Updated to bundle .NET 10 runtime dependencies
* **Documentation**: Updated README and build documentation for .NET 10
* **Test Infrastructure**: Added UITestBase class for UI automation tests
* **Code Quality**: Enhanced null-safety and error handling throughout codebase

## [0.5.0](https://github.com/gadams999/obrl/compare/v0.4.0...v0.5.0) (2024-12-31)

> Enhancement release adding animated transitions, configurable grid layouts, and variable position support for different wheel configurations.

### Upgrade Steps
* No action required - all changes are backward compatible
* Existing profiles will use default animation settings (300ms fade)
* Grid layouts will default to 2x4 configuration

### Breaking Changes
* None

### New Features
* **Animated Transitions**: Smooth fade-in/fade-out animations when switching positions
  - Configurable animation duration (0-2000ms, default 300ms)
  - Option to disable animations entirely for instant switching
  - Works across all layout modes (Single, Vertical, Horizontal, Grid)
  - Smart animation skipping during rapid wheel rotation
* **Configurable Grid Dimensions**: Customize grid layout rows and columns
  - Support for 1-4 rows and 1-4 columns
  - Defaults to 2x4 for optimal 8-position display
  - Automatically adapts to different wheel configurations
  - Smart text condensing works with custom grid sizes
* **Variable Position Support**: Support for wheels with different position counts
  - Configurable position count per profile (4, 8, 12, 16, etc.)
  - UI dynamically shows/hides position fields based on count
  - Maintains backward compatibility with existing 8-position profiles
* **Improved UI Defaults**: Better default window size (800x600) and spacing in settings dialog

### Bug Fixes
* **Single Text Animation**: Fixed animation state management bugs
  - Corrected display states during position transitions
  - Improved animation queue handling
* **Application Shutdown**: Enhanced shutdown handling to prevent hanging
* **Nullable Reference Warnings**: Fixed all compiler warnings for cleaner builds

### Performance Improvements
* None

### Other Changes
* **Build System**: Added TreatWarningsAsErrors to CI/CD and build scripts
  - Ensures code quality by failing builds on warnings
  - Applied to GitHub Actions, MSI build, and release build scripts
* **Repository Cleanup**: Added .NET build artifacts to .gitignore
  - Removed tracked bin, obj, and .vs folders
  - Cleaner repository structure

## [0.4.0](https://github.com/gadams999/obrl/compare/v0.2.0...v0.4.0) (2024-12-29)

> Major feature release adding About dialog, smart text condensing, empty position feedback, enhanced single layout, test mode, and comprehensive test suite. Includes fixes for test mode indicator and first-run text labels.

### Upgrade Steps
* No action required - all changes are backward compatible

### Breaking Changes
* None

### New Features
* **About Wheel Overlay Dialog**: Accessible from system tray menu with application information
  - Displays version number read from assembly metadata
  - Clickable GitHub repository link
  - Modal dialog with fixed size and centered positioning
  - Close button and Escape key support
* **Smart Text Condensing**: Automatically hides empty positions in multi-position layouts
  - Only displays positions with configured text labels
  - Maintains original position numbers after filtering
  - Works in Vertical, Horizontal, and Grid layouts
* **Empty Position Feedback**: Visual flash animation when selecting empty positions
  - 500ms flash duration alternating between selected/non-selected colors
  - Confirms wheel input was detected even when no text is configured
  - Stops immediately when populated position is selected
  - Restarts if another empty position is selected while flashing
* **Enhanced Single Layout**: Improved handling of empty positions
  - Displays last populated position text when empty position is selected
  - Shows text in non-selected color to indicate empty position
  - Handles startup case when first position is empty
* **Test Mode**: Development mode for testing without physical hardware
  - Launch with `--test-mode` or `/test` command-line flags
  - Left/Right arrow keys simulate wheel position changes
  - Position wraps around at boundaries (0-7)
  - Yellow border indicator shows when test mode is active
* **Comprehensive Test Suite**: Property-based and integration tests
  - 51 total tests covering all new features
  - Property-based tests using FsCheck for universal correctness
  - Integration tests verifying features work together
  - All tests pass with 1 skipped (requires STA thread for WPF UI)

### Bug Fixes
* **Test Mode Indicator**: Yellow border now only shows when test mode is enabled
  - Previously showed even when test mode was disabled
  - Fixed by removing hardcoded BorderThickness attribute
* **First-Run Text Labels**: Default text labels now display on first launch
  - Previously required opening settings and clicking Apply
  - Fixed by creating default profile with text labels on first run

### Performance Improvements
* None

### Other Changes
* **README.md**: Comprehensive update for v0.4.0
  - Added detailed Getting Started guide
  - Expanded Usage section with all new features
  - Enhanced Troubleshooting with common issues
  - Added Development section with build instructions
  - Added Version History section
* **Documentation**: Added steering file for keeping documentation current
  - Guidelines for when to update README
  - Checklist for documentation updates before pushing
  - Documentation quality standards
* **Technical**: Added property-based testing with FsCheck (minimum 100 iterations per test)
* **Technical**: All code changes mapped to specific requirements for traceability
* **Technical**: Design document includes 14 correctness properties validated by tests
* **Technical**: 3,600+ lines added across 25+ files

## [0.2.0](https://github.com/gadams999/obrl/compare/v0.1.0...v0.2.0) (2023-XX-XX)

> Feature release adding layout profiles, device awareness, dynamic fields, application icon, and robust startup handling.

### Upgrade Steps
* No action required - profiles are created automatically on first launch

### Breaking Changes
* None

### New Features
* **Layout Profiles**: Create and save multiple profiles for different cars or sims
* **Device Awareness**: Profiles linked to specific devices
* **Dynamic Fields**: Settings interface adjusts based on selected wheel capabilities
* **Application Icon**: Proper branding with application icon
* **Robust Startup**: Improved crash handling and logging

### Bug Fixes
* None

### Performance Improvements
* None

### Other Changes
* Enhanced settings management with profile support
* Improved error handling and recovery

## [0.1.0](https://github.com/gadams999/obrl/releases/tag/v0.1.0) (2023-XX-XX)

> Initial release of Wheel Overlay with core functionality for displaying wheel position overlays.

### Upgrade Steps
* [ACTION REQUIRED] First installation - download and run installer

### Breaking Changes
* None

### New Features
* **Moveable Overlay**: Config mode for positioning overlay
* **Multiple Layouts**: Single, Vertical, Horizontal, and Grid layouts
* **Customizable Appearance**: Font, color, and opacity settings
* **System Tray Integration**: Minimize to tray for unobtrusive operation
* **DirectInput Support**: Works with BavarianSimTec Alpha wheel
* **Device Detection**: Shows "Not Found" message when device disconnected

### Bug Fixes
* None

### Performance Improvements
* None

### Other Changes
* Initial project setup and architecture
