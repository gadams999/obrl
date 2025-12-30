# Requirements Document

## Introduction

This specification defines the requirements for version 0.5.0 enhancements to the Wheel Overlay application. These enhancements focus on improving the visual experience with animated text transitions, providing flexible grid layout configurations, and supporting variable position counts per profile to accommodate different wheel hardware configurations.

## Glossary

- **Text_Position**: A numbered position on the rotary encoder (1 through N, where N is configurable per profile)
- **Single_Layout**: Display layout showing only the currently selected text
- **Grid_Layout**: Display layout arranging text positions in a 2D grid (rows × columns)
- **Vertical_Layout**: Display layout arranging text positions in a vertical list
- **Horizontal_Layout**: Display layout arranging text positions in a horizontal list
- **Position_Transition**: The change from one Text_Position to another
- **Transition_Animation**: A visual effect that animates the change between text positions
- **Rotation_Direction**: The direction of position change (forward/next or backward/previous)
- **Grid_Dimensions**: The row and column configuration for Grid_Layout (e.g., 2×4, 3×3, 4×2)
- **Position_Count**: The total number of Text_Positions available in a profile (2-20)
- **Profile**: A saved configuration containing text labels, layout settings, and position count for a specific device
- **Populated_Position**: A Text_Position that has a text label configured
- **Empty_Position**: A Text_Position that has no text label configured
- **Grid_Condensing**: The automatic removal of Empty_Positions from Grid_Layout display

## Requirements

### Requirement 1: Animated Position Transitions in Single Layout

**User Story:** As a user, I want smooth animated transitions when changing positions in Single layout, so that I can better perceive the direction of change and have a more polished visual experience.

#### Acceptance Criteria

1. WHEN using Single_Layout and the user changes to the next position, THE System SHALL animate the current text rotating upward and fading out
2. WHEN using Single_Layout and the user changes to the next position, THE System SHALL animate the new text appearing from the bottom and rotating into place
3. WHEN using Single_Layout and the user changes to the previous position, THE System SHALL animate the current text rotating downward and fading out
4. WHEN using Single_Layout and the user changes to the previous position, THE System SHALL animate the new text appearing from the top and rotating into place
5. THE Transition_Animation SHALL complete within 200-300 milliseconds
6. WHEN the user rapidly changes positions, THE System SHALL interrupt the current animation and start the new animation immediately
7. WHEN the user changes to an Empty_Position, THE System SHALL still perform the Transition_Animation before displaying the last Populated_Position
8. THE Transition_Animation SHALL not occur when the application first starts or when switching profiles

### Requirement 2: Configurable Grid Dimensions

**User Story:** As a user, I want to configure the grid layout dimensions per profile, so that I can choose the arrangement that best matches my wheel's physical layout and personal preference.

#### Acceptance Criteria

1. WHEN creating or editing a Profile, THE System SHALL allow the user to configure Grid_Dimensions
2. THE System SHALL support Grid_Dimensions of 2×N, 3×N, 4×N, N×2, N×3, and N×4 where N is calculated based on Position_Count
3. THE default Grid_Dimensions SHALL be 2×N (2 rows, N columns)
4. WHEN the user selects Grid_Dimensions, THE System SHALL validate that rows × columns ≥ Position_Count
5. WHEN displaying Grid_Layout, THE System SHALL arrange Populated_Positions according to the configured Grid_Dimensions
6. WHEN Grid_Condensing removes Empty_Positions, THE System SHALL maintain the configured Grid_Dimensions for the remaining positions
7. THE System SHALL persist Grid_Dimensions settings with the Profile

### Requirement 3: Grid Condensing with Configurable Dimensions

**User Story:** As a user, I want empty positions to be automatically removed from the grid while maintaining my chosen grid dimensions, so that the display remains compact and organized.

#### Acceptance Criteria

1. WHEN displaying Grid_Layout with Empty_Positions, THE System SHALL remove Empty_Positions from the display
2. WHEN Grid_Condensing occurs, THE System SHALL fill the grid according to the configured Grid_Dimensions using only Populated_Positions
3. WHEN the number of Populated_Positions is less than rows × columns, THE System SHALL reduce the grid size while maintaining the aspect ratio of the configured Grid_Dimensions
4. THE System SHALL maintain the original position numbering for all Populated_Positions after condensing
5. WHEN all positions become populated, THE System SHALL expand the grid to the full configured Grid_Dimensions

### Requirement 4: Configurable Position Count Per Profile

**User Story:** As a user, I want to configure how many positions my wheel has per profile, so that I can support different wheel hardware with varying position counts.

#### Acceptance Criteria

1. WHEN creating or editing a Profile, THE System SHALL allow the user to configure Position_Count
2. THE System SHALL support Position_Count values from 2 to 20
3. THE default Position_Count SHALL be 8 (for backward compatibility with existing profiles)
4. WHEN the user changes Position_Count, THE System SHALL adjust the number of text input fields in the settings interface
5. WHEN the user increases Position_Count, THE System SHALL preserve existing text labels and add empty fields for new positions
6. WHEN the user decreases Position_Count, THE System SHALL warn the user if populated positions will be removed
7. WHEN the user decreases Position_Count, THE System SHALL remove text labels for positions beyond the new Position_Count
8. THE System SHALL persist Position_Count with the Profile

### Requirement 5: Dynamic Input Detection for Variable Position Counts

**User Story:** As a user with a wheel that has a non-standard position count, I want the system to correctly detect and respond to all my wheel positions, so that all positions function properly.

#### Acceptance Criteria

1. WHEN a Profile has Position_Count set to N, THE System SHALL monitor button inputs corresponding to positions 1 through N
2. THE System SHALL map DirectInput buttons based on the configured Position_Count (e.g., buttons 57-64 for 8 positions, buttons 57-76 for 20 positions)
3. WHEN the user selects a position beyond the configured Position_Count, THE System SHALL ignore the input
4. WHEN the user selects a position within the configured Position_Count, THE System SHALL update the display accordingly
5. THE System SHALL support wrap-around behavior from position N to position 1 and vice versa

### Requirement 6: Grid Dimension Validation

**User Story:** As a user, I want the system to prevent invalid grid configurations, so that I don't accidentally create unusable layouts.

#### Acceptance Criteria

1. WHEN the user selects Grid_Dimensions, THE System SHALL ensure rows × columns ≥ Position_Count
2. WHEN the user changes Position_Count, THE System SHALL validate that the current Grid_Dimensions can accommodate the new Position_Count
3. IF the current Grid_Dimensions cannot accommodate the new Position_Count, THE System SHALL automatically adjust Grid_Dimensions to the default (2×N)
4. THE System SHALL display an error message if the user attempts to set invalid Grid_Dimensions
5. THE System SHALL provide suggested Grid_Dimensions based on the current Position_Count

### Requirement 7: Settings UI for Grid Configuration

**User Story:** As a user, I want an intuitive interface to configure grid dimensions, so that I can easily set up my preferred layout.

#### Acceptance Criteria

1. WHEN the user opens Profile settings, THE System SHALL display a Grid_Dimensions configuration section
2. THE Grid_Dimensions configuration SHALL include dropdown or input controls for rows and columns
3. THE System SHALL display a preview of the grid arrangement based on the selected dimensions
4. THE System SHALL show the total capacity (rows × columns) and compare it to Position_Count
5. WHEN the user changes Grid_Dimensions, THE System SHALL immediately update the preview
6. THE System SHALL disable invalid Grid_Dimensions options in the UI

### Requirement 8: Settings UI for Position Count Configuration

**User Story:** As a user, I want an intuitive interface to configure the number of positions, so that I can easily match my wheel's capabilities.

#### Acceptance Criteria

1. WHEN the user opens Profile settings, THE System SHALL display a Position_Count configuration control
2. THE Position_Count control SHALL be a numeric input or dropdown with values from 2 to 20
3. WHEN the user changes Position_Count, THE System SHALL immediately adjust the number of text input fields
4. WHEN the user decreases Position_Count and populated positions will be lost, THE System SHALL display a confirmation dialog
5. THE confirmation dialog SHALL list which positions will be removed
6. THE System SHALL only apply the Position_Count change after user confirmation

### Requirement 9: Animation Performance

**User Story:** As a user, I want smooth animations that don't impact game performance, so that the overlay remains unobtrusive during racing.

#### Acceptance Criteria

1. THE Transition_Animation SHALL maintain 60 FPS or higher
2. THE Transition_Animation SHALL not cause noticeable CPU or GPU spikes
3. WHEN the system is under load, THE System SHALL gracefully degrade animation quality rather than causing stuttering
4. THE System SHALL complete or skip animations if they would cause the display to lag behind the actual position by more than 100ms
5. THE System SHALL provide an option to disable Transition_Animation in settings

### Requirement 10: Test Mode Support for Variable Positions

**User Story:** As a developer, I want test mode to work with different position counts, so that I can test configurations without physical hardware.

#### Acceptance Criteria

1. WHEN Test_Mode is active with a Profile that has Position_Count set to N, THE System SHALL simulate N positions
2. WHEN the user presses Right Arrow at position N, THE System SHALL wrap to position 1
3. WHEN the user presses Left Arrow at position 1, THE System SHALL wrap to position N
4. THE System SHALL display the current position number in the test mode indicator
5. THE System SHALL allow testing all configured Grid_Dimensions in test mode
