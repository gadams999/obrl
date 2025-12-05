# Technical Requirements - Wheel Overlay

## 1. Technology Stack
- **Language**: C#
- **Framework**: .NET 8.0 (Desktop)
- **UI Framework**: Windows Presentation Foundation (WPF)
- **Input Library**: `Vortice.DirectInput` (or `SharpDX.DirectInput` if Vortice is unavailable) for low-level controller access.

## 2. System Requirements
- **OS**: Windows 10 / Windows 11 (64-bit)
- **Runtimes**: .NET 8.0 Runtime

## 3. Architecture
### 3.1. Application Structure
- **Main Window (WPF)**: Handles the visual presentation.
    - `WindowStyle="None"`
    - `AllowsTransparency="True"`
    - `Topmost="True"`
    - `ShowInTaskbar="False"` (optional)
- **Input Service**: A background worker/thread that polls the DirectInput device.
    - Must run independently of the UI thread to prevent freezing.
    - Polling rate: ~60Hz or higher.

### 3.2. Input Processing
- **Device Enumeration**: Ability to list connected DirectInput devices.
- **Device Selection**: Mechanism (config file or hardcoded initially) to select the specific controller GUID.
- **Button Mapping**:
    - **Target Device**: Bavarian Sim Tec Alpha.
    - **Button Range**: Buttons 58-65 (1-indexed) => Buttons 57-64 (DirectInput 0-indexed).
    - **Logic**: Monitor this specific range. If `Button[i]` is pressed, update UI to `Index = i - 57`.

### 3.3. Window Management
- **Click-Through**: Use Windows API (`SetWindowLong`, `GetWindowLong`, `WS_EX_TRANSPARENT`, `WS_EX_LAYERED`) to ensure the window is click-through.

## 4. Deployment
- **Output**: Single executable (Self-contained or Framework-dependent).
- **Installer**: MSI or Setup.exe (optional for MVP).

## 5. Development Environment
### 5.1. Required Tools
- **.NET SDK**: .NET 8.0 SDK or later.
- **IDE**: Visual Studio 2022 (Community or higher) with ".NET desktop development" workload OR Visual Studio Code with C# Dev Kit.
- **Git**: For version control.

### 5.2. Packaging Tools
- **WiX Toolset**: (Optional) For building MSI installers if `dotnet publish` single-file exe is insufficient.
- **Inno Setup**: (Alternative) for creating simple installers.

### 5.3. Testing Tools
- **Joystick Test Application**: [Planet Pointy Joystick Test App](https://www.planetpointy.co.uk/joystick-test-application/). Essential for verifying devices with more than 32 buttons (which DIView and standard Windows tools often fail to display).
