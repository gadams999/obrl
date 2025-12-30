# Implementation Plan: v0.5.0 Enhancements

## Overview

This implementation plan covers three main enhancements for version 0.5.0:
1. Animated position transitions in Single layout with directional animations
2. Configurable grid dimensions with smart condensing
3. Variable position count per profile (2-20 positions)

The implementation follows an incremental approach, building from data model changes through UI enhancements to animation features.

## Tasks

- [ ] 1. Enhance Profile data model
  - Add PositionCount property (default 8)
  - Add GridRows property (default 2)
  - Add GridColumns property (default 4)
  - Add IsValidGridConfiguration() validation method
  - Add AdjustGridToDefault() method
  - Add NormalizeTextLabels() method
  - _Requirements: 4.1, 4.2, 4.3, 2.1, 2.3, 6.1_

- [ ] 1.1 Write unit tests for Profile model
  - Test default values (PositionCount=8, GridRows=2, GridColumns=4)
  - Test IsValidGridConfiguration with valid and invalid configurations
  - Test AdjustGridToDefault adjusts to 2×N
  - Test NormalizeTextLabels adds/removes labels correctly
  - _Requirements: 4.3, 2.3, 6.1_

- [ ] 2. Create ProfileValidator class
  - Implement ValidateGridDimensions method
  - Implement GetSuggestedDimensions method
  - Support 2×N, 3×N, 4×N, N×2, N×3, N×4 patterns
  - Include square-ish configurations
  - _Requirements: 2.2, 6.1, 6.5_

- [ ] 2.1 Write property test for grid dimension validation
  - **Property 6: Grid Dimension Validation**
  - **Validates: Requirements 2.4, 6.1**

- [ ] 2.2 Write property test for grid suggestion validity
  - **Property 20: Grid Suggestion Validity**
  - **Validates: Requirements 6.5**

- [ ] 3. Update SettingsService for persistence
  - Update LoadSettings to normalize profiles on load
  - Update LoadSettings to validate and auto-correct grid configurations
  - Update SaveSettings to normalize profiles before saving
  - Handle missing v0.5.0 fields with defaults
  - _Requirements: 2.7, 4.8_

- [ ] 3.1 Write property test for grid dimension persistence
  - **Property 7: Grid Dimension Persistence**
  - **Validates: Requirements 2.7**

- [ ] 3.2 Write property test for position count persistence
  - **Property 14: Position Count Persistence**
  - **Validates: Requirements 4.8**

- [ ] 3.3 Write unit tests for settings migration
  - Test profile with missing v0.5.0 fields loads with defaults
  - Test profile with invalid grid dimensions is auto-corrected
  - _Requirements: 2.7, 4.8, 6.3_

- [ ] 4. Checkpoint - Ensure data model tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Create SettingsViewModel
  - Add SelectedProfile property
  - Add AvailablePositionCounts (2-20)
  - Add AvailableRows and AvailableColumns (1-10)
  - Add TextLabelInputs observable collection
  - Add GridPreviewCells observable collection
  - Add SuggestedDimensions observable collection
  - Add GridCapacityDisplay computed property
  - Implement UpdatePositionCount method
  - Implement RefreshTextLabelInputs method
  - Implement RefreshGridPreview method
  - Implement RefreshSuggestedDimensions method
  - Add ApplySuggestedDimensionCommand
  - _Requirements: 4.1, 4.4, 2.1, 7.1, 7.2, 7.3, 8.1, 8.2_

- [ ] 5.1 Write unit tests for SettingsViewModel
  - Test UpdatePositionCount preserves existing labels when increasing
  - Test UpdatePositionCount removes labels when decreasing
  - Test RefreshGridPreview shows correct number of cells
  - Test GridCapacityDisplay shows correct capacity
  - _Requirements: 4.4, 4.5, 4.7, 7.3_

- [ ] 6. Enhance Settings Window UI
  - Add Position Count ComboBox (2-20)
  - Add Grid Dimensions controls (Rows and Columns ComboBoxes)
  - Add Grid Preview with UniformGrid
  - Add Grid Capacity display
  - Add Suggested Dimensions buttons
  - Make Text Labels panel dynamic based on PositionCount
  - Add ScrollViewer for text labels
  - _Requirements: 4.1, 2.1, 7.1, 7.2, 7.3, 7.4, 7.5, 8.1, 8.2_

- [ ] 6.1 Implement PositionCount_Changed event handler
  - Check for populated positions being removed
  - Show confirmation dialog if data loss would occur
  - Call UpdatePositionCount on ViewModel
  - Call ValidateAndAdjustGridDimensions
  - _Requirements: 4.4, 4.5, 4.6, 4.7, 6.2_

- [ ] 6.2 Implement GridDimensions_Changed event handler
  - Call ValidateGridDimensions
  - Update grid preview
  - _Requirements: 2.4, 6.1, 6.4_

- [ ] 6.3 Implement ValidateAndAdjustGridDimensions method
  - Check if grid configuration is valid
  - Auto-adjust to default if invalid
  - Show notification message
  - _Requirements: 6.2, 6.3_

- [ ] 6.4 Write unit tests for Settings Window
  - Test position count change shows confirmation when needed
  - Test grid dimension validation rejects invalid configurations
  - Test grid auto-adjustment works correctly
  - _Requirements: 4.6, 6.2, 6.3, 6.4_

- [ ] 7. Checkpoint - Ensure settings UI tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 8. Update InputService for variable position count
  - Add SetActiveProfile method
  - Update _maxButtonIndex based on PositionCount
  - Update PollDevice to check button range [57, 57+N-1]
  - Update test mode to use PositionCount for wrap-around
  - Log configured button range
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 10.1, 10.2, 10.3_

- [ ] 8.1 Write property test for input button range
  - **Property 15: Input Button Range**
  - **Validates: Requirements 5.1, 5.2**

- [ ] 8.2 Write property test for out-of-range input filtering
  - **Property 16: Out-of-Range Input Filtering**
  - **Validates: Requirements 5.3**

- [ ] 8.3 Write property test for in-range input handling
  - **Property 17: In-Range Input Handling**
  - **Validates: Requirements 5.4**

- [ ] 8.4 Write property test for position wrap-around
  - **Property 18: Position Wrap-Around**
  - **Validates: Requirements 5.5**

- [ ] 8.5 Write property test for test mode position range
  - **Property 22: Test Mode Position Range**
  - **Validates: Requirements 10.1, 10.2, 10.3**

- [ ] 9. Enhance OverlayViewModel for grid layout
  - Add EffectiveGridRows computed property
  - Add EffectiveGridColumns computed property
  - Add PopulatedPositionItems observable collection
  - Implement CalculateCondensedRows method
  - Implement CalculateCondensedColumns method
  - Update grid items when position or configuration changes
  - _Requirements: 2.5, 2.6, 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 9.1 Write property test for empty position filtering
  - **Property 8: Empty Position Filtering**
  - **Validates: Requirements 3.1**

- [ ] 9.2 Write property test for position number preservation
  - **Property 9: Position Number Preservation**
  - **Validates: Requirements 3.4**

- [ ] 9.3 Write property test for grid aspect ratio preservation
  - **Property 10: Grid Aspect Ratio Preservation**
  - **Validates: Requirements 3.3**

- [ ] 10. Update GridLayout view
  - Update ItemsControl to use PopulatedPositionItems
  - Update UniformGrid to bind to EffectiveGridRows and EffectiveGridColumns
  - Update item template to show position number and label
  - Apply styling for selected/non-selected items
  - _Requirements: 2.5, 2.6, 3.1, 3.2_

- [ ] 10.1 Write unit tests for GridLayout
  - Test grid renders correct number of cells for various configurations
  - Test grid maintains position numbers after condensing
  - Test grid expands when all positions become populated
  - _Requirements: 2.5, 3.1, 3.4, 3.5_

- [ ] 11. Checkpoint - Ensure grid layout tests pass
  - Ensure all tests pass, ask the user if questions arise.


- [ ] 12. Implement SingleTextLayout animations
  - Add CurrentText and NextText TextBlocks to XAML
  - Add RenderTransforms (RotateTransform, TranslateTransform) to both TextBlocks
  - Implement OnPositionChanged method
  - Implement IsForwardTransition method with wrap-around logic
  - Implement AnimateTransition method with directional animations
  - Implement StopCurrentAnimation method for interruption
  - Set animation duration to 250ms
  - Set rotation angle to 15 degrees
  - Set translation distance to 50 pixels
  - Use CubicEase easing functions
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [ ] 12.1 Write property test for forward animation direction
  - **Property 1: Forward Animation Direction**
  - **Validates: Requirements 1.1, 1.2**

- [ ] 12.2 Write property test for backward animation direction
  - **Property 2: Backward Animation Direction**
  - **Validates: Requirements 1.3, 1.4**

- [ ] 12.3 Write property test for animation duration bounds
  - **Property 3: Animation Duration Bounds**
  - **Validates: Requirements 1.5**

- [ ] 12.4 Write property test for animation interruption
  - **Property 4: Animation Interruption**
  - **Validates: Requirements 1.6**

- [ ] 12.5 Write property test for empty position animation
  - **Property 5: Empty Position Animation**
  - **Validates: Requirements 1.7**

- [ ] 12.6 Write unit test for no animation on startup
  - Test animation doesn't occur when application first starts
  - Test animation doesn't occur when switching profiles
  - _Requirements: 1.8_

- [ ] 13. Update MainWindow for animation coordination
  - Add _previousPosition field
  - Update OnRotaryPositionChanged to detect layout type
  - Call SingleTextLayout.OnPositionChanged for Single layout
  - Pass previous and new position to animation
  - Add FindVisualChild helper method
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [ ] 14. Add animation performance optimization
  - Implement animation queue management
  - Add lag detection (>100ms behind actual position)
  - Implement animation skipping when lagging
  - Add animation disable option to settings
  - _Requirements: 9.4, 9.5_

- [ ] 14.1 Write property test for animation lag prevention
  - **Property 21: Animation Lag Prevention**
  - **Validates: Requirements 9.4**

- [ ] 14.2 Write unit test for animation disable option
  - Test animation can be disabled in settings
  - Test position changes work without animation when disabled
  - _Requirements: 9.5_

- [ ] 15. Checkpoint - Ensure animation tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Update test mode indicator
  - Update test mode indicator to show current position number
  - Update indicator text to include position (e.g., "TEST MODE - Position 5")
  - _Requirements: 10.4_

- [ ] 16.1 Write unit test for test mode position display
  - Test indicator shows current position number
  - _Requirements: 10.4_

- [ ] 16.2 Write property test for test mode grid support
  - **Property 23: Test Mode Grid Support**
  - **Validates: Requirements 10.5**

- [ ] 17. Integration testing and bug fixes
  - Test all features work together
  - Test position count changes with various layouts
  - Test grid dimension changes with various position counts
  - Test animations with variable position counts
  - Test settings persistence across application restarts
  - Fix any integration issues discovered
  - _Requirements: All_

- [ ] 18. Write property tests for position count changes
  - Write property test for position count range support
  - Write property test for position count increase preservation
  - Write property test for position count decrease removal
  - Write property test for grid auto-adjustment
  - _Requirements: 4.2, 4.5, 4.7, 6.3_

- [ ] 18.1 Write property test for position count range support
  - **Property 11: Position Count Range Support**
  - **Validates: Requirements 4.2**

- [ ] 18.2 Write property test for position count increase preservation
  - **Property 12: Position Count Increase Preservation**
  - **Validates: Requirements 4.5**

- [ ] 18.3 Write property test for position count decrease removal
  - **Property 13: Position Count Decrease Removal**
  - **Validates: Requirements 4.7**

- [ ] 18.4 Write property test for grid auto-adjustment
  - **Property 19: Grid Auto-Adjustment**
  - **Validates: Requirements 6.3**

- [ ] 19. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at logical breakpoints
- Property tests validate universal correctness properties across input space
- Unit tests validate specific examples, edge cases, and error conditions
- Implementation follows incremental approach: data model → settings UI → grid layout → animations
- Test mode enhancements are integrated throughout to support variable position counts
- All tests are required for comprehensive validation of the feature

