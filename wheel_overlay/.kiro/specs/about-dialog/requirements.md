# Requirements Document

## Introduction

This specification defines the requirements for adding an "About Wheel Overlay" dialog to the application. The dialog will be accessible from the system tray icon context menu and will display application information including version, description, and relevant links.

## Glossary

- **System_Tray_Icon**: The application icon displayed in the Windows system tray (notification area)
- **About_Dialog**: A modal window displaying application information
- **Context_Menu**: The menu that appears when right-clicking the system tray icon
- **Application_Version**: The semantic version number of the application (e.g., 0.3.2)
- **Text_Position**: A numbered position on the rotary encoder (1 through N)
- **Empty_Position**: A Text_Position that has no text label configured
- **Populated_Position**: A Text_Position that has a text label configured
- **Flash_Animation**: A brief visual effect where text rapidly alternates between selected and non-selected colors
- **Single_Layout**: Display layout showing only the currently selected text
- **Test_Mode**: A development mode that simulates wheel input using keyboard arrow keys
- **Test_Wheel_Profile**: A virtual wheel profile used for testing without physical hardware

## Requirements

### Requirement 1: About Menu Item

**User Story:** As a user, I want to access application information from the system tray, so that I can view version details and other relevant information.

#### Acceptance Criteria

1. WHEN the user right-clicks the System_Tray_Icon, THE Context_Menu SHALL display an "About Wheel Overlay" menu item
2. WHEN the user clicks the "About Wheel Overlay" menu item, THE System SHALL open the About_Dialog
3. THE "About Wheel Overlay" menu item SHALL be positioned at the bottom of the Context_Menu, above the "Exit" option
4. THE "About Wheel Overlay" menu item SHALL have a menu separator between it and the "Exit" option

### Requirement 2: About Dialog Content

**User Story:** As a user, I want to see application details in the About dialog, so that I can identify the version and access project resources.

#### Acceptance Criteria

1. WHEN the About_Dialog is displayed, THE System SHALL show the application name "Wheel Overlay"
2. WHEN the About_Dialog is displayed, THE System SHALL show the current Application_Version
3. WHEN the About_Dialog is displayed, THE System SHALL show a brief description of the application
4. WHEN the About_Dialog is displayed, THE System SHALL show a clickable link to the GitHub repository
5. WHEN the About_Dialog is displayed, THE System SHALL show copyright information

### Requirement 3: About Dialog Behavior

**User Story:** As a user, I want the About dialog to behave like a standard Windows dialog, so that I have a familiar user experience.

#### Acceptance Criteria

1. THE About_Dialog SHALL be a modal window
2. WHEN the About_Dialog is open, THE System SHALL prevent interaction with other application windows until the dialog is closed
3. WHEN the user clicks outside the About_Dialog, THE System SHALL keep the dialog focused
4. THE About_Dialog SHALL include a "Close" or "OK" button
5. WHEN the user clicks the "Close" button, THE System SHALL close the About_Dialog
6. WHEN the user presses the Escape key, THE System SHALL close the About_Dialog

### Requirement 4: About Dialog Appearance

**User Story:** As a user, I want the About dialog to have a clean and professional appearance, so that it reflects well on the application quality.

#### Acceptance Criteria

1. THE About_Dialog SHALL have a fixed size and location appropriate for its content
2. THE About_Dialog SHALL not be resizable
3. THE About_Dialog SHALL display the application icon
4. THE About_Dialog SHALL use consistent fonts and spacing with the rest of the application
5. THE About_Dialog SHALL be centered on the screen when opened

### Requirement 5: Version Information Accuracy

**User Story:** As a developer, I want the About dialog to automatically display the correct version, so that version information stays synchronized with the build.

#### Acceptance Criteria

1. THE System SHALL read the Application_Version from the assembly metadata
2. WHEN the application is built with a new version number, THE About_Dialog SHALL display the updated version without code changes
3. THE displayed version SHALL match the version in WheelOverlay.csproj

### Requirement 6: Smart Text Condensing

**User Story:** As a user, I want the overlay to show only the positions that have text configured, so that I don't see empty positions cluttering the display.

#### Acceptance Criteria

1. WHEN displaying in Vertical, Horizontal, or Grid layouts, THE System SHALL only render Populated_Positions
2. WHEN all Text_Positions have text configured, THE System SHALL display all positions
3. WHEN some Text_Positions are empty, THE System SHALL process those as Empty Position Selection Feedback (requiremet 7)
4. THE System SHALL maintain the original position numbering for Populated_Positions
5. WHEN the configuration changes, THE System SHALL immediately update the display to show only Populated_Positions

### Requirement 7: Empty Position Selection Feedback

**User Story:** As a user, I want visual feedback when I select an empty position, so that I know the system detected my input even though there's no text to display.

#### Acceptance Criteria

1. WHEN the user selects an Empty_Position in Vertical, Horizontal, or Grid layouts, THE System SHALL trigger a Flash_Animation on all displayed text
2. WHEN the Flash_Animation occurs, THE System SHALL rapidly alternate all text between selected and non-selected colors
3. THE Flash_Animation SHALL last approximately 500 milliseconds
4. WHEN the user selects a Populated_Position after an Empty_Position, THE System SHALL stop flashing and return to normal highlighting
5. WHEN the user selects another Empty_Position while flashing, THE System SHALL restart the Flash_Animation

### Requirement 8: Single Layout Empty Position Handling

**User Story:** As a user using Single layout, I want to see the last valid text when I select an empty position, so that the display doesn't go blank.

#### Acceptance Criteria

1. WHEN using Single_Layout and the user selects an Empty_Position, THE System SHALL display the most recently selected Populated_Position
2. WHEN displaying the most recent text for an Empty_Position, THE System SHALL show it in non-selected color
3. WHEN displaying the most recent text for an Empty_Position, THE System SHALL trigger a Flash_Animation
4. THE Flash_Animation SHALL last approximately 500 milliseconds
5. WHEN the user selects a Populated_Position after an Empty_Position, THE System SHALL display the new text in selected color without flashing
6. WHEN the application starts and the first selected position is empty, THE System SHALL display the first Populated_Position text in non-selected color

### Requirement 9: Test Mode for Development

**User Story:** As a developer, I want to test the overlay without a physical wheel, so that I can develop and debug features without hardware dependencies.

#### Acceptance Criteria

1. WHEN a command-line flag or configuration setting enables Test_Mode, THE System SHALL create a Test_Wheel_Profile
2. THE Test_Wheel_Profile SHALL be based on the BavarianSimTec Alpha configuration with 8 positions
3. WHEN Test_Mode is active and the user presses the Left Arrow key, THE System SHALL simulate selecting the previous position
4. WHEN Test_Mode is active and the user presses the Right Arrow key, THE System SHALL simulate selecting the next position
5. WHEN the simulated position reaches position 1 and Left Arrow is pressed, THE System SHALL wrap to position 8
6. WHEN the simulated position reaches position 8 and Right Arrow is pressed, THE System SHALL wrap to position 1
7. WHEN Test_Mode is active, THE System SHALL display a visual indicator that test mode is enabled
8. WHEN Test_Mode is disabled, THE System SHALL use normal DirectInput device detection
