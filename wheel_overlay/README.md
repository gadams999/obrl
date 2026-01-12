# Wheel Overlay

A transparent overlay application for sim racing wheels (e.g., BavarianSimTec Alpha). It displays telemetry or arbitrary text labels on your screen based on the position of a rotary encoder. Useful as another indicator for the encoder's position (MAP, TC1, TC2, etc.) and when paired with [OpenKneeboard](https://openkneeboard.com/), provides the information within VR.

## Features

### v0.5.0 New Features
*   **Animated Transitions**: Smooth fade-in/fade-out animations when switching between wheel positions (configurable 0-2000ms, default 300ms).
*   **Configurable Grid Layout**: Customize grid dimensions from 1-4 rows and 1-4 columns (defaults to 2x4 for 8 positions).
*   **Variable Position Support**: Configure wheels with different position counts (4, 8, 12, 16, etc.) - UI adapts dynamically.
*   **Improved UI**: Better default window size and spacing in settings dialog.

### v0.4.0 Features
*   **About Dialog**: Access application information, version, and GitHub repository from the system tray menu.
*   **Smart Text Condensing**: Empty positions are automatically hidden from multi-position layouts (Vertical, Horizontal, Grid).
*   **Empty Position Feedback**: Visual flash animation when selecting an empty position to confirm input detection.
*   **Single Layout Enhancement**: Displays the last populated position when an empty position is selected, with visual indication.
*   **Test Mode**: Develop and test without physical hardware using keyboard arrow keys (launch with `--test-mode` flag).

### v0.2.0 Features
*   **Layout Profiles**: Create and save multiple profiles for different cars or sims (e.g., "GT3", "Formula").
*   **Device Awareness**: Profiles are linked to specific devices.
*   **Dynamic Fields**: The settings interface adjusts the number of text inputs based on the selected wheel's capabilities.
*   **Application Icon**: Proper branding application icon.
*   **Robust Startup**: Improved crash handling and logging.

### Core Features
*   **Moveable Overlay**: Enter "Config Mode" to drag the overlay to match your wheel's position.
*   **Customizable Layout**: Choose between Single, Vertical, Horizontal, or Grid layouts.
*   **Appearance**: Customize fonts, colors, and opacity.
*   **System Tray**: Minimized to tray for unobtrusive operation.

## Installation

1.  Download the latest `.msi` from the [Releases](https://github.com/gadams999/obrl/releases) page.
2.  Run the installer.
3.  Launch "WheelOverlay" from the Start Menu or Desktop shortcut.

## Getting Started

### First-Time Setup

1.  **Initial Launch**: The application starts in "Config Mode" with a semi-transparent gray background.
2.  **Position the Overlay**: 
    - Drag the window to align with your physical wheel's display area.
    - The overlay should match the position of your wheel's cutouts or display.
3.  **Configure Your First Profile**:
    - Right-click the overlay or System Tray icon → "Settings"
    - In the Display tab, configure your text labels for each wheel position
    - Choose your preferred layout (Single, Vertical, Horizontal, or Grid)
4.  **Lock Position**: 
    - Press `Enter` or uncheck "Config Mode" in the system tray menu
    - The overlay becomes transparent and click-through

### Daily Usage

1.  **Launch Application**: Start "WheelOverlay" from the Start Menu or let it auto-start with Windows.
2.  **Switch Profiles**: Right-click System Tray icon → Settings → Display → Select profile from dropdown.
3.  **View Information**: Right-click System Tray icon → "About Wheel Overlay" to see version and links.
4.  **Reposition**: Right-click System Tray icon → Check "Config Mode" → Drag to new position → Press `Enter`.

## Usage

### System Tray Menu

Right-click the System Tray icon to access:
- **Settings**: Configure profiles, layouts, text labels, and appearance
- **Config Mode**: Enable/disable overlay repositioning
- **Minimize**: Minimize to taskbar (if enabled in settings)
- **About Wheel Overlay**: View application version and GitHub repository
- **Exit**: Close the application

### Profiles

Profiles allow you to save different configurations for different cars or racing sims:

1.  **Create Profile**: Settings → Display → Click "New" button
2.  **Name Profile**: Give it a descriptive name (e.g., "GT3 Car", "Formula")
3.  **Configure**: Set text labels for each wheel position
4.  **Switch Profiles**: Use the dropdown to switch between saved profiles

### Layouts

Choose the layout that best matches your wheel's physical display:

- **Single**: Shows only the currently selected position (large text) with smooth fade transitions (v0.5.0+)
- **Vertical**: Stacked list of all populated positions
- **Horizontal**: Side-by-side list of all populated positions
- **Grid**: 2D grid arrangement with configurable rows and columns (v0.5.0+)

**Note**: In v0.4.0+, empty positions are automatically hidden in Vertical, Horizontal, and Grid layouts.

### Animation Settings (v0.5.0+)

Control how the overlay transitions between positions:

1.  **Enable/Disable Animations**: Settings → Display → Check/uncheck "Enable Animations"
2.  **Animation Duration**: Adjust slider from 0ms (instant) to 2000ms (slow fade)
3.  **Default**: 300ms provides smooth transitions without lag

**Note**: Animations automatically skip during rapid wheel rotation to prevent lag.

### Grid Configuration (v0.5.0+)

Customize the grid layout dimensions:

1.  **Settings → Display → Grid Layout**
2.  **Rows**: Select 1-4 rows
3.  **Columns**: Select 1-4 columns
4.  **Default**: 2 rows × 4 columns (optimal for 8 positions)

### Position Count (v0.5.0+)

Configure wheels with different position counts:

1.  **Settings → Display → Position Count**
2.  **Select Count**: Choose 4, 8, 12, 16, or custom
3.  **Dynamic UI**: Text input fields adjust automatically
4.  **Default**: 8 positions (standard for most wheels)

### Smart Text Condensing (v0.4.0+)

The overlay intelligently handles empty positions:

- **Automatic Filtering**: Only positions with configured text are displayed
- **Position Numbers Preserved**: Original position numbers are maintained
- **Empty Position Feedback**: When you select an empty position:
  - All text flashes for 500ms (alternating between selected/non-selected colors)
  - Confirms the wheel input was detected even though no text is configured
- **Single Layout**: Displays the last populated position in non-selected color when empty position is selected

### Test Mode (v0.4.0+)

Test the overlay without physical hardware:

1.  **Launch with Test Mode**:
    ```
    WheelOverlay.exe --test-mode
    ```
    or
    ```
    WheelOverlay.exe /test
    ```

2.  **Keyboard Controls**:
    - **Left Arrow**: Move to previous position (wraps from 0 to 7)
    - **Right Arrow**: Move to next position (wraps from 7 to 0)

3.  **Visual Indicator**: Yellow border appears around the overlay when test mode is active

**Note:** This mode is normally used for development testing. As such, it captures the left and right arrow keys while the appication is running.

## Troubleshooting

### Common Issues

*   **Device Not Found**: 
    - Ensure your wheel is connected and powered on
    - The overlay shows "🚨 Not Found!" if the device is disconnected
    - Check that the correct device is selected in Settings → Display → Device

*   **Overlay Not Visible**:
    - Check that "Config Mode" is disabled (overlay should be transparent)
    - Verify the overlay isn't positioned off-screen
    - Try re-entering Config Mode to reposition

*   **Text Not Updating**:
    - Verify your wheel is properly connected
    - Check that text labels are configured in the active profile
    - Ensure the correct profile is selected for your device

*   **Application Crashes on Startup**:
    - Check logs at `%APPDATA%\WheelOverlay\logs.txt`
    - Try deleting settings file at `%APPDATA%\WheelOverlay\settings.json` (will reset to defaults)
    - Reinstall the application

### Logs

If you encounter issues:
1.  Navigate to `%APPDATA%\WheelOverlay\`
2.  Open `logs.txt` to view detailed error messages
3.  Include relevant log entries when reporting issues on GitHub

## Development

### Building from Source

Requirements:
- .NET 10.0 SDK
- Visual Studio 2022 or later (recommended)

```bash
cd wheel_overlay
dotnet build
```

### Running Tests

The project includes comprehensive automated tests covering all user interactions, layout modes, and error handling scenarios.

#### Standard Test Run (100 iterations)

For thorough validation with full property-based test coverage:

```bash
cd wheel_overlay/WheelOverlay.Tests
dotnet test
```

This runs all tests with 100 iterations per property test, providing comprehensive validation.

#### Fast Test Run (10 iterations)

For quick feedback during development:

```bash
cd wheel_overlay/WheelOverlay.Tests
dotnet test --configuration FastTests
```

This runs all tests with 10 iterations per property test, completing in a fraction of the time while still catching most issues.

#### When to Use Each Configuration

- **Debug/Release (100 iterations)**: Use for final validation before committing, pre-merge checks, and when investigating test failures
- **FastTests (10 iterations)**: Use during active development for rapid feedback, when making frequent changes, or when running tests repeatedly

Test coverage includes:
- Unit tests for core functionality
- Property-based tests using FsCheck (configurable 10 or 100 iterations per test)
- Integration tests for end-to-end workflows
- UI automation tests for system tray and mouse interactions

### Test Mode for Development

Use test mode to develop without physical hardware:

```bash
cd wheel_overlay/WheelOverlay
dotnet run -- --test-mode
```

## Contributing

Contributions are welcome! Please:
1.  Fork the repository
2.  Create a feature branch
3.  Make your changes with tests
4.  Submit a pull request

## License

[Add license information]

## Support

- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/gadams999/obrl/issues)
- **Discussions**: Ask questions on [GitHub Discussions](https://github.com/gadams999/obrl/discussions)

## Version History

### v0.5.2 (Current)
- Upgraded to .NET 10 framework
- Comprehensive automated testing suite with 100+ tests
- Fixed vertical layout crash on fresh installs
- Property-based testing using FsCheck
- UI automation tests for all user interactions
- Enhanced error handling and logging

### v0.5.0
- Animated transitions with configurable duration
- Configurable grid layout dimensions (1-4 rows/columns)
- Variable position support for different wheel configurations
- Improved UI defaults and spacing
- Build system enhancements with warnings-as-errors

### v0.4.0
- Added About Wheel Overlay dialog
- Smart text condensing with empty position feedback
- Test mode for development without hardware
- Enhanced Single layout for empty position handling
- Comprehensive integration test suite

### v0.2.0
- Layout profiles with device awareness
- Dynamic field configuration
- Application icon and branding
- Improved startup and error handling

### v0.1.0
- Initial release
- Basic overlay functionality
- Multiple layout options
- Config mode for positioning
