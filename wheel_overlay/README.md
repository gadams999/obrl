# Wheel Overlay

A transparent overlay application for sim racing wheels (e.g., BavarianSimTec Alpha). It displays telemetry or arbitrary text labels on your screen, perfectly aligned with the physical wheel's display/cutouts.

## Features

### v0.2.0 New Features
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

## Usage

1.  **First Run**: The application will start in "Config Mode" (semi-transparent gray background).
2.  **Positioning**: Drag the window to align with your wheel.
3.  **Settings**: Right-click the overlay or the System Tray icon to open Settings.
4.  **Profiles**: 
    - In Settings > Display, use the "Profile" dropdown to switch layouts.
    - Click "New" to create a profile (e.g. for a specific car).
    - Changes to Layout or Text Labels are saved to the active profile.
5.  **Finish**: Press `Enter` or uncheck "Config Mode" to lock the position.

## Troubleshooting

*   **Logs**: If the application fails to start, check `%APPDATA%\WheelOverlay\logs.txt`.
*   **Device Not Found**: Ensure your wheel is connected. The overlay shows "🚨 Not Found!" if the device is disconnected.
