# Design Document

## Overview

This document describes the technical design for implementing user-selectable shortcuts in the WheelOverlay MSI installer. The feature will add a new dialog to the custom UI that allows users to choose which shortcuts to create during installation.

## Architecture

### Component Overview

The implementation consists of three main components:

1. **ShortcutOptionsDlg** - New WiX dialog in CustomUI.wxs
2. **WiX Properties** - INSTALLSTARTMENUSHORTCUT and INSTALLDESKTOPSHORTCUT
3. **Conditional Components** - Modified shortcut components in Package.wxs with conditions

### Dialog Flow

The installation dialog sequence will be modified as follows:

```
WelcomeDlg
    ↓
LicenseAgreementDlg
    ↓
InstallDirDlg
    ↓
ShortcutOptionsDlg (NEW)
    ↓
VerifyReadyDlg
    ↓
ProgressDlg
    ↓
ExitDlg
```

## Detailed Design

### 1. WiX Properties

Two new properties will be defined in Package.wxs to store user selections:

```xml
<Property Id="INSTALLSTARTMENUSHORTCUT" Value="1" />
<Property Id="INSTALLDESKTOPSHORTCUT" Value="1" />
```

**Default Values:** Both properties default to "1" (checked) to maintain current behavior where both shortcuts are created.

**Property Behavior:**
- Value "1" = checkbox checked, shortcut will be created
- Value "" (empty) = checkbox unchecked, shortcut will not be created

### 2. ShortcutOptionsDlg Dialog

A new dialog will be added to CustomUI.wxs with the following specifications:

**Dialog Dimensions:** 370x270 (consistent with other dialogs)

**Controls:**
- **Title** - "Shortcut Options" using WixUI_Font_Title
- **Description** - "Choose which shortcuts to create"
- **BannerLine** - Separator line at Y=44
- **StartMenuCheckbox** - Checkbox control for Start Menu shortcut
  - Position: X=20, Y=70
  - Text: "Create Start Menu shortcut"
  - Property: INSTALLSTARTMENUSHORTCUT
  - CheckBoxValue: "1"
- **DesktopCheckbox** - Checkbox control for Desktop shortcut
  - Position: X=20, Y=95
  - Text: "Create Desktop shortcut"
  - Property: INSTALLDESKTOPSHORTCUT
  - CheckBoxValue: "1"
- **WarningText** - Text control for warning message
  - Position: X=20, Y=130, Width=330, Height=40
  - Text: "Note: It is recommended to create at least one shortcut for easy access to the application."
  - Condition: INSTALLSTARTMENUSHORTCUT="" AND INSTALLDESKTOPSHORTCUT=""
- **Back Button** - Navigate to InstallDirDlg
- **Next Button** - Navigate to VerifyReadyDlg
- **Cancel Button** - Spawn CancelDlg
- **BottomLine** - Separator line at Y=234

**Navigation Logic:**
- Back button publishes NewDialog event with Value="InstallDirDlg"
- Next button publishes NewDialog event with Value="VerifyReadyDlg"
- Cancel button publishes SpawnDialog event with Value="CancelDlg"

### 3. Modified Dialog Navigation

**InstallDirDlg Changes:**
- Next button currently navigates to VerifyReadyDlg
- Will be changed to navigate to ShortcutOptionsDlg

**VerifyReadyDlg Changes:**
- Back button currently navigates to InstallDirDlg
- Will be changed to navigate to ShortcutOptionsDlg
- InstallText control will be enhanced to show shortcut selections

**Enhanced VerifyReadyDlg Text:**
```
[ProductName] will be installed to: [INSTALLFOLDER]

Shortcuts to be created:
[INSTALLSTARTMENUSHORTCUT_TEXT]
[INSTALLDESKTOPSHORTCUT_TEXT]
```

Where:
- INSTALLSTARTMENUSHORTCUT_TEXT = "- Start Menu" if INSTALLSTARTMENUSHORTCUT="1", else ""
- INSTALLDESKTOPSHORTCUT_TEXT = "- Desktop" if INSTALLDESKTOPSHORTCUT="1", else ""

### 4. Conditional Shortcut Components

The existing shortcut components in Package.wxs will be modified to include conditions:

**Start Menu Shortcut Component:**
```xml
<Component Id="ApplicationShortcut" Guid="e8369302-3932-491c-8f43-855f93976375">
  <Condition>INSTALLSTARTMENUSHORTCUT="1"</Condition>
  <!-- existing shortcut definition -->
</Component>
```

**Desktop Shortcut Component:**
```xml
<Component Id="DesktopShortcut" Directory="DesktopFolder" Guid="f8369302-3932-491c-8f43-855f93976376">
  <Condition>INSTALLDESKTOPSHORTCUT="1"</Condition>
  <!-- existing shortcut definition -->
</Component>
```

**Condition Evaluation:**
- WiX evaluates conditions during installation
- If condition is false, component is not installed
- If condition is true, component is installed normally

### 5. Uninstall Behavior

**Registry Tracking:**
The existing registry values in each shortcut component already serve as KeyPath and track installation:
- Start Menu: `HKCU\Software\OBRL\WheelOverlay\installed`
- Desktop: `HKCU\Software\OBRL\WheelOverlay\DesktopShortcut`

**Uninstall Logic:**
- WiX automatically tracks which components were installed
- During uninstall, only installed components are removed
- No additional code needed - WiX handles this automatically
- If a shortcut was not created (condition false), its component was never installed, so uninstall won't attempt to remove it

### 6. Warning Message Implementation

**Approach:** Display a non-blocking warning when both checkboxes are unchecked.

**Implementation:**
- Use a Text control with a Condition attribute
- Condition: `INSTALLSTARTMENUSHORTCUT="" AND INSTALLDESKTOPSHORTCUT=""`
- Control is only visible when condition is true
- Does not block installation (user can still click Next)

**Rationale:** 
- Non-blocking approach respects user choice
- Warning provides guidance without being intrusive
- Some users may prefer no shortcuts (e.g., portable installations, custom launchers)

## Data Flow

### Installation Flow

1. User launches MSI installer
2. Properties INSTALLSTARTMENUSHORTCUT and INSTALLDESKTOPSHORTCUT are initialized to "1"
3. User navigates through dialogs: Welcome → License → Install Directory
4. ShortcutOptionsDlg displays with both checkboxes checked (default)
5. User can check/uncheck boxes, modifying property values
6. If both unchecked, warning text appears
7. User clicks Next to VerifyReadyDlg
8. VerifyReadyDlg shows installation summary including shortcuts
9. User clicks Install
10. WiX evaluates component conditions:
    - If INSTALLSTARTMENUSHORTCUT="1", install Start Menu shortcut component
    - If INSTALLDESKTOPSHORTCUT="1", install Desktop shortcut component
11. Installation completes with selected shortcuts created

### Uninstall Flow

1. User runs uninstaller
2. WiX queries installed components from registry
3. Only components that were installed are removed
4. Shortcuts that were created are removed
5. Shortcuts that were not created are ignored (component was never installed)

## UI Consistency

### Visual Design

All UI elements follow the existing custom dialog patterns:

- **Fonts:** WixUI_Font_Normal (Tahoma 8pt), WixUI_Font_Title (Tahoma 9pt bold)
- **Colors:** Default Windows installer colors (transparent backgrounds)
- **Layout:** 370x270 dialog size, consistent spacing
- **Controls:** Standard button sizes (56x17), consistent positioning
- **Lines:** Banner line at Y=44, bottom line at Y=234

### Control Positioning

Following the established pattern from other dialogs:
- Title: X=15, Y=6
- Description: X=25, Y=23
- Content area: Y=60 to Y=220
- Buttons: Y=243 (Back at X=180, Next at X=236, Cancel at X=304)

## Build Process Impact

### No Changes Required

The build process (build_msi.ps1) does not need modification:
- Dialog definitions are in CustomUI.wxs (already compiled)
- Property definitions are in Package.wxs (already compiled)
- Component conditions are evaluated at install time, not build time
- File harvesting logic remains unchanged

### Testing Considerations

After implementation, test the following scenarios:
1. Both shortcuts selected (default) - both created
2. Only Start Menu selected - only Start Menu created
3. Only Desktop selected - only Desktop created
4. Neither selected - no shortcuts created, warning displayed
5. Uninstall after each scenario - only created shortcuts removed

## Risk Analysis

### Low Risk Items

- **Dialog addition:** Adding a new dialog to existing UI sequence is straightforward
- **Property usage:** WiX properties are well-documented and reliable
- **Component conditions:** Standard WiX feature, widely used
- **Uninstall behavior:** WiX handles component tracking automatically

### Mitigation Strategies

- **Testing:** Comprehensive testing of all checkbox combinations
- **Default values:** Both shortcuts enabled by default maintains current behavior
- **Non-blocking warning:** Allows advanced users to proceed without shortcuts
- **Registry tracking:** Existing registry values ensure proper uninstall

## Implementation Notes

### File Modifications

1. **Package.wxs**
   - Add two Property definitions
   - Add Condition elements to two Component elements
   - No structural changes to existing components

2. **CustomUI.wxs**
   - Add ShortcutOptionsDlg dialog definition
   - Modify InstallDirDlg Next button navigation
   - Modify VerifyReadyDlg Back button navigation
   - Optionally enhance VerifyReadyDlg text to show selections

3. **build_msi.ps1**
   - No changes required

### Backward Compatibility

- Default behavior unchanged (both shortcuts created)
- Existing installations not affected
- Upgrade scenarios work normally (MajorUpgrade already defined)

## Future Enhancements

Potential future improvements (out of scope for this spec):

1. Remember user preferences across upgrades
2. Add option to create Quick Launch shortcut
3. Add option to run application after installation
4. Localization support for multiple languages
