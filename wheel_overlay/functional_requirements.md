# Functional Requirements - Wheel Overlay

## 1. Overview
The Wheel Overlay is a lightweight Windows utility designed for sim racers. It visualizes the current position of a rotary encoder (dial) on a game controller/steering wheel as a specific "button press" or text value on a transparent overlay.

## 2. User Interface
### 2.1. Overlay Window
- **Transparency**: The window background must be completely transparent. Only the text/content should be visible.
- **Always on Top**: The overlay must remain above all other windows, including full-screen games.
- **Frameless**: No standard Windows title bar, borders, or resize handles in normal operation.
- **Taskbar Presence**: The application must support being minimized to the taskbar.
- **Click-through**: In normal operation, the overlay should not intercept mouse clicks.

### 2.2. Configuration & Positioning
- **Config Mode**: A specific mode that allows the window to be dragged and resized.
- **Access**: Config mode and settings should be accessible via a context menu (right-click) on the main app (e.g., taskbar icon or system tray) selecting "Settings..." or "Config/Adjust".
- **Settings**:
    - Toggle "Config Mode" (enable/disable click-through and window borders).
    - Select Display Layout.
    - Configure Colors/Fonts.

### 2.3. Display Content
- **Content**: Displays a set of 1-N text entries (e.g., "Map 1", "Map 2", "Map 3").
- **Highlighting**: Only one text entry is highlighted at a time, corresponding to the current rotary position.
- **Layout Options**:
    1.  **Vertical Row**: List of items arranged vertically.
    2.  **Horizontal Row**: List of items arranged horizontally.
    3.  **Compact Grid**: NxM grid of items.
    4.  **Single Text (Animated)**: Shows only the current active text. When changing, animates (slide/fade) from the previous to the new text.
- **Styling**:
    - **High Contrast**: Option for high contrast selected text.
    - **Color Palettes**: User-configurable colors for "Selected" vs "Non-Selected" entries (text color, background color/glow).

## 3. Input Handling
### 3.1. Controller Support
- **Device Detection**: Must detect the **Bavarian Sim Tec Alpha** racing wheel.
- **Protocol**: DirectInput.

### 3.2. Rotary Logic
- **Hardware Behavior**: The center rotary is activated by pushing it in (Button 9, 1-indexed). It then reports its position as a single held button in the range **58-65** (1-indexed).
- **State Detection**:
    - The application should listen for Buttons 58-65.
    - **Mapping**:
        - Button 58 -> Item 1
        - Button 59 -> Item 2
        - ...
        - Button 65 -> Item 8
    - **Wrap-around**: The rotary wraps from 65 to 58 and vice versa.
    - **Highlighting**: The overlay highlights the text entry corresponding to the currently active button (58-65).

## 4. Performance
- **Latency**: Updates must be near-instantaneous (<16ms).
- **Resource Usage**: Negligible CPU/GPU impact.
