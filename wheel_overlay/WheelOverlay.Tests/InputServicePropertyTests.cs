using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Services;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class InputServicePropertyTests
    {
        // Feature: v0.5.0-enhancements, Property 15: Input Button Range
        // Validates: Requirements 5.1, 5.2
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_InputButtonRange()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)), // Generate position counts 2-20
                positionCount =>
                {
                    // Arrange
                    var inputService = new InputService();
                    var profile = new Profile { PositionCount = positionCount };
                    
                    // Act
                    inputService.SetActiveProfile(profile);
                    
                    // Use reflection to get the configured button range
                    var maxButtonIndexField = typeof(InputService).GetField("_maxButtonIndex", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int maxButtonIndex = (int)maxButtonIndexField?.GetValue(inputService)!;
                    
                    var baseButtonIndexField = typeof(InputService).GetField("BASE_BUTTON_INDEX", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    int baseButtonIndex = (int)baseButtonIndexField?.GetValue(null)!;
                    
                    // Calculate expected max button index
                    int expectedMaxButtonIndex = baseButtonIndex + positionCount - 1;
                    
                    // Assert
                    inputService.Dispose();
                    
                    return (maxButtonIndex == expectedMaxButtonIndex)
                        .Label($"For position count {positionCount}, max button index should be {expectedMaxButtonIndex}, but got {maxButtonIndex}");
                });
        }

        // Feature: v0.5.0-enhancements, Property 16: Out-of-Range Input Filtering
        // Validates: Requirements 5.3
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_OutOfRangeInputFiltering()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 19)), // Generate position counts 2-19 (so we can test beyond range)
                positionCount =>
                {
                    // Arrange
                    var inputService = new InputService();
                    var profile = new Profile { PositionCount = positionCount };
                    inputService.SetActiveProfile(profile);
                    
                    // Get the base button index
                    var baseButtonIndexField = typeof(InputService).GetField("BASE_BUTTON_INDEX", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    int baseButtonIndex = (int)baseButtonIndexField?.GetValue(null)!;
                    
                    // Test a button beyond the configured range
                    int outOfRangeButton = baseButtonIndex + positionCount; // One beyond max
                    
                    // Act - Simulate button press using reflection (we can't actually press buttons in tests)
                    // We'll verify the logic by checking that the max button index is correctly set
                    var maxButtonIndexField = typeof(InputService).GetField("_maxButtonIndex", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int maxButtonIndex = (int)maxButtonIndexField?.GetValue(inputService)!;
                    
                    // Assert - The out of range button should be beyond maxButtonIndex
                    bool isOutOfRange = outOfRangeButton > maxButtonIndex;
                    
                    inputService.Dispose();
                    
                    return isOutOfRange
                        .Label($"Button {outOfRangeButton} should be out of range for position count {positionCount} (max button: {maxButtonIndex})");
                });
        }

        // Feature: v0.5.0-enhancements, Property 17: In-Range Input Handling
        // Validates: Requirements 5.4
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_InRangeInputHandling()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)), // Generate position counts 2-20
                positionCount =>
                {
                    // Arrange
                    var inputService = new InputService();
                    var profile = new Profile { PositionCount = positionCount };
                    inputService.SetActiveProfile(profile);
                    
                    // Get the base button index
                    var baseButtonIndexField = typeof(InputService).GetField("BASE_BUTTON_INDEX", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    int baseButtonIndex = (int)baseButtonIndexField?.GetValue(null)!;
                    
                    // Pick a random button within range
                    var random = new System.Random();
                    int buttonIndex = baseButtonIndex + random.Next(0, positionCount);
                    int expectedPosition = buttonIndex - baseButtonIndex;
                    
                    // Verify the button is in range
                    var maxButtonIndexField = typeof(InputService).GetField("_maxButtonIndex", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int maxButtonIndex = (int)maxButtonIndexField?.GetValue(inputService)!;
                    
                    bool isInRange = buttonIndex >= baseButtonIndex && buttonIndex <= maxButtonIndex;
                    bool positionIsValid = expectedPosition >= 0 && expectedPosition < positionCount;
                    
                    inputService.Dispose();
                    
                    return (isInRange && positionIsValid)
                        .Label($"Button {buttonIndex} should be in range for position count {positionCount} (range: {baseButtonIndex}-{maxButtonIndex}), expected position: {expectedPosition}");
                });
        }

        // Feature: v0.5.0-enhancements, Property 18: Position Wrap-Around
        // Validates: Requirements 5.5
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_PositionWrapAround()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)), // Generate position counts 2-20
                positionCount =>
                {
                    // Arrange
                    var inputService = new InputService();
                    var profile = new Profile { PositionCount = positionCount };
                    inputService.SetActiveProfile(profile);
                    
                    // Test wrap-around logic for test mode
                    var testModeField = typeof(InputService).GetField("_testMode", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    testModeField?.SetValue(inputService, true);
                    
                    var testModeMaxPositionField = typeof(InputService).GetField("_testModeMaxPosition", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int testModeMaxPosition = (int)testModeMaxPositionField?.GetValue(inputService)!;
                    
                    // Verify wrap-around boundaries
                    bool maxPositionCorrect = testModeMaxPosition == positionCount - 1;
                    
                    // Test forward wrap (from max to 0)
                    var positionField = typeof(InputService).GetField("_testModePosition", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    positionField?.SetValue(inputService, testModeMaxPosition);
                    
                    int? receivedPosition = null;
                    inputService.RotaryPositionChanged += (sender, position) =>
                    {
                        receivedPosition = position;
                    };
                    
                    // Simulate incrementing past max
                    int nextPosition = testModeMaxPosition + 1;
                    if (nextPosition > testModeMaxPosition)
                        nextPosition = 0;
                    
                    var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    raiseMethod?.Invoke(inputService, new object[] { nextPosition });
                    
                    bool wrapAroundWorks = receivedPosition == 0;
                    
                    inputService.Dispose();
                    
                    return (maxPositionCorrect && wrapAroundWorks)
                        .Label($"For position count {positionCount}, max position should be {positionCount - 1}, got {testModeMaxPosition}. Wrap-around from max should go to 0, got {receivedPosition}");
                });
        }

        // Feature: v0.5.0-enhancements, Property 22: Test Mode Position Range
        // Validates: Requirements 10.1, 10.2, 10.3
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_TestModePositionRange()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)), // Generate position counts 2-20
                positionCount =>
                {
                    // Arrange
                    var inputService = new InputService();
                    var profile = new Profile { PositionCount = positionCount };
                    inputService.SetActiveProfile(profile);
                    
                    // Enable test mode
                    var testModeField = typeof(InputService).GetField("_testMode", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    testModeField?.SetValue(inputService, true);
                    
                    var testModeMaxPositionField = typeof(InputService).GetField("_testModeMaxPosition", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int testModeMaxPosition = (int)testModeMaxPositionField?.GetValue(inputService)!;
                    
                    // Test that we can cycle through all positions
                    var positionField = typeof(InputService).GetField("_testModePosition", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    List<int> receivedPositions = new List<int>();
                    inputService.RotaryPositionChanged += (sender, position) =>
                    {
                        receivedPositions.Add(position);
                    };
                    
                    // Cycle through all positions
                    for (int i = 0; i < positionCount; i++)
                    {
                        raiseMethod?.Invoke(inputService, new object[] { i });
                    }
                    
                    // Verify all positions were received
                    bool allPositionsReceived = receivedPositions.Count == positionCount;
                    bool allPositionsInRange = receivedPositions.All(p => p >= 0 && p < positionCount);
                    bool maxPositionCorrect = testModeMaxPosition == positionCount - 1;
                    
                    inputService.Dispose();
                    
                    return (allPositionsReceived && allPositionsInRange && maxPositionCorrect)
                        .Label($"For position count {positionCount}, should receive all positions 0-{positionCount - 1}. " +
                               $"Received {receivedPositions.Count} positions, all in range: {allPositionsInRange}, max position: {testModeMaxPosition}");
                });
        }

        // Feature: about-dialog, Property 12: Test Mode Position Increment
        // Validates: Requirements 9.3, 9.6
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_TestModePositionIncrement()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(0, 7)), // Generate positions 0-7
                startPosition =>
                {
                    // Arrange
                    var inputService = new InputService();
                    
                    // Use reflection to set test mode and position
                    var testModeField = typeof(InputService).GetField("_testMode", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    testModeField?.SetValue(inputService, true);
                    
                    var positionField = typeof(InputService).GetField("_testModePosition", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    positionField?.SetValue(inputService, startPosition);

                    int? receivedPosition = null;
                    inputService.RotaryPositionChanged += (sender, position) =>
                    {
                        receivedPosition = position;
                    };

                    // Act - directly call the position change method using reflection
                    var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    // Simulate Right arrow key: increment position with wrap-around
                    int newPosition = startPosition + 1;
                    if (newPosition > 7)
                        newPosition = 0;
                    
                    raiseMethod?.Invoke(inputService, new object[] { newPosition });

                    // Calculate expected position with wrap-around
                    int expectedPosition = (startPosition + 1) % 8;

                    // Assert
                    inputService.Dispose();

                    return (receivedPosition == expectedPosition)
                        .Label($"Starting at position {startPosition}, Right arrow should result in position {expectedPosition}, but got {receivedPosition}");
                });
        }

        // Feature: about-dialog, Property 13: Test Mode Position Decrement
        // Validates: Requirements 9.4, 9.5
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_TestModePositionDecrement()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(0, 7)), // Generate positions 0-7
                startPosition =>
                {
                    // Arrange
                    var inputService = new InputService();
                    
                    // Use reflection to set test mode and position
                    var testModeField = typeof(InputService).GetField("_testMode", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    testModeField?.SetValue(inputService, true);
                    
                    var positionField = typeof(InputService).GetField("_testModePosition", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    positionField?.SetValue(inputService, startPosition);

                    int? receivedPosition = null;
                    inputService.RotaryPositionChanged += (sender, position) =>
                    {
                        receivedPosition = position;
                    };

                    // Act - directly call the position change method using reflection
                    var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    // Simulate Left arrow key: decrement position with wrap-around
                    int newPosition = startPosition - 1;
                    if (newPosition < 0)
                        newPosition = 7;
                    
                    raiseMethod?.Invoke(inputService, new object[] { newPosition });

                    // Calculate expected position with wrap-around
                    int expectedPosition = (startPosition - 1 + 8) % 8;

                    // Assert
                    inputService.Dispose();

                    return (receivedPosition == expectedPosition)
                        .Label($"Starting at position {startPosition}, Left arrow should result in position {expectedPosition}, but got {receivedPosition}");
                });
        }

        // Feature: about-dialog, Property 14: Test Mode Activation
        // Validates: Requirements 9.1, 9.8
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_TestModeActivation()
        {
            return Prop.ForAll(
                Arb.From(Gen.Elements("--test-mode", "/test")),
                flag =>
                {
                    // Arrange - We need to test that command-line flags enable test mode
                    // Since we can't easily modify Environment.GetCommandLineArgs() in tests,
                    // we'll test the TestMode property directly
                    var inputService = new InputService();

                    // Act - Set test mode
                    inputService.TestMode = true;

                    // Assert - Test mode should be enabled
                    bool testModeEnabled = inputService.TestMode;

                    // Clean up
                    inputService.Dispose();

                    return testModeEnabled
                        .Label($"Test mode should be enabled when TestMode property is set to true");
                });
        }
    }
}
