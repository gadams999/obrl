# Requirements Document

## Introduction

This specification defines the enhancement to the WheelOverlay MSI installer to allow users to choose which shortcuts to create during installation. Currently, the installer automatically creates both a Start Menu shortcut and a Desktop shortcut. This feature will give users control over shortcut creation through the installer UI.

## Glossary

- **Installer** - The WheelOverlay MSI installer application built with WiX v4
- **Start Menu Shortcut** - A shortcut placed in the Windows Start Menu under "Wheel Overlay"
- **Desktop Shortcut** - A shortcut placed on the user's desktop
- **CustomUI** - The custom WiX UI dialog set defined in CustomUI.wxs
- **Feature** - A WiX installer component that can be conditionally installed
- **Property** - A WiX installer variable that stores user choices

## Requirements

### Requirement 1

**User Story:** As a user installing WheelOverlay, I want to choose whether to create a Start Menu shortcut, so that I can control what appears in my Start Menu.

#### Acceptance Criteria

1. WHEN the installer displays the shortcut options dialog THEN the system SHALL present a checkbox for "Create Start Menu shortcut"
2. WHEN the Start Menu checkbox is checked THEN the system SHALL create the Start Menu shortcut during installation
3. WHEN the Start Menu checkbox is unchecked THEN the system SHALL NOT create the Start Menu shortcut during installation
4. WHEN the installer first displays the shortcut options dialog THEN the Start Menu checkbox SHALL be checked by default

### Requirement 2

**User Story:** As a user installing WheelOverlay, I want to choose whether to create a Desktop shortcut, so that I can keep my desktop organized.

#### Acceptance Criteria

1. WHEN the installer displays the shortcut options dialog THEN the system SHALL present a checkbox for "Create Desktop shortcut"
2. WHEN the Desktop checkbox is checked THEN the system SHALL create the Desktop shortcut during installation
3. WHEN the Desktop checkbox is unchecked THEN the system SHALL NOT create the Desktop shortcut during installation
4. WHEN the installer first displays the shortcut options dialog THEN the Desktop checkbox SHALL be checked by default

### Requirement 3

**User Story:** As a user installing WheelOverlay, I want the shortcut options to appear at an appropriate point in the installation flow, so that I can make informed decisions before installation begins.

#### Acceptance Criteria

1. WHEN the user proceeds from the installation directory dialog THEN the system SHALL display the shortcut options dialog
2. WHEN the user clicks Back from the shortcut options dialog THEN the system SHALL return to the installation directory dialog
3. WHEN the user clicks Next from the shortcut options dialog THEN the system SHALL proceed to the ready-to-install dialog
4. WHEN the ready-to-install dialog displays THEN the system SHALL show which shortcuts will be created based on user selections

### Requirement 4

**User Story:** As a user, I want at least one shortcut option available, so that I can easily launch the application after installation.

#### Acceptance Criteria

1. WHEN both shortcut checkboxes are unchecked THEN the system SHALL display a warning message
2. WHEN the warning is displayed THEN the system SHALL recommend selecting at least one shortcut option
3. WHEN both checkboxes are unchecked and the user clicks Next THEN the system SHALL allow the installation to proceed (warning only, not blocking)

### Requirement 5

**User Story:** As a user uninstalling WheelOverlay, I want only the shortcuts that were created to be removed, so that the uninstaller doesn't attempt to remove non-existent shortcuts.

#### Acceptance Criteria

1. WHEN the installer creates a Start Menu shortcut THEN the system SHALL record that the shortcut was created
2. WHEN the installer creates a Desktop shortcut THEN the system SHALL record that the shortcut was created
3. WHEN the uninstaller runs THEN the system SHALL only attempt to remove shortcuts that were recorded as created
4. WHEN the uninstaller completes THEN the system SHALL remove all created shortcuts without errors

### Requirement 6

**User Story:** As a developer, I want the shortcut selection to integrate with the existing WiX v4 custom UI, so that the installer maintains a consistent look and feel.

#### Acceptance Criteria

1. WHEN the shortcut options dialog is displayed THEN the system SHALL use the same fonts, colors, and layout as other custom UI dialogs
2. WHEN the shortcut options dialog is displayed THEN the system SHALL include standard navigation buttons (Back, Next, Cancel)
3. WHEN the shortcut options dialog is displayed THEN the system SHALL follow the same dialog dimensions (370x270) as other dialogs
4. WHEN the shortcut options dialog is displayed THEN the system SHALL include the standard title bar and bottom line separator

### Requirement 7

**User Story:** As a developer, I want the shortcut features to be properly conditioned in WiX, so that the installer only includes components that the user selected.

#### Acceptance Criteria

1. WHEN the installer evaluates the Start Menu shortcut component THEN the system SHALL check the INSTALLSTARTMENUSHORTCUT property
2. WHEN the installer evaluates the Desktop shortcut component THEN the system SHALL check the INSTALLDESKTOPSHORTCUT property
3. WHEN a shortcut property is set to "1" THEN the system SHALL include that shortcut component in the installation
4. WHEN a shortcut property is not set to "1" THEN the system SHALL exclude that shortcut component from the installation

