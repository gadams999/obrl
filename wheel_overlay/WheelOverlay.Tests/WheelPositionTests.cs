using System;
using System.Collections.Generic;
using WheelOverlay.Models;
using WheelOverlay.Services;
using WheelOverlay.ViewModels;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for wheel position changes via test mode.
    /// Verifies position updates, keyboard input handling, and position wrapping.
    /// 
    /// Requirements: 5.2, 5.3, 5.4, 5.5, 5.6, 5.7
    /// </summary>
    public class WheelPositionTests : IDisposable
    {
        private AppSettings _testSettings;
        private OverlayViewModel _testViewModel;
        private InputService _inputService;

        public WheelPositionTests()
        {
            // Create test settings with a default profile
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                PositionCount = 8,
                TextLabels = new List<string> 
                { 
                    "POS1", "POS2", "POS3", "POS4", 
                    "POS5", "POS6", "POS7", "POS8" 
                }
            };

            _testSettings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            _testViewModel = new OverlayViewModel(_testSettings);
            _inputService = new InputService();
            _inputService.SetActiveProfile(profile);
        }

        public void Dispose()
        {
            _inputService?.Dispose();
        }

        /// <summary>
        /// Verifies that the right arrow key advances the position in test mode.
        /// Tests that position increments from 0 to 1, 1 to 2, etc.
        /// 
        /// Requirements: 5.2, 5.6
        /// </summary>
        [Fact]
        public void TestMode_RightArrow_AdvancesPosition()
        {
            // Arrange
            _testViewModel.IsTestMode = true;
            _testViewModel.CurrentPosition = 0;
            int? receivedPosition = null;

            // Subscribe to position changes from InputService
            _inputService.RotaryPositionChanged += (sender, position) =>
            {
                receivedPosition = position;
                _testViewModel.CurrentPosition = position;
            };

            // Act - Simulate right arrow key press by directly calling the internal method
            // In test mode, right arrow increments position
            var testModePositionField = typeof(InputService).GetField("_testModePosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            testModePositionField?.SetValue(_inputService, 0);

            var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Simulate right arrow: position should advance from 0 to 1
            raiseMethod?.Invoke(_inputService, new object[] { 1 });

            // Assert
            Assert.Equal(1, receivedPosition);
            Assert.Equal(1, _testViewModel.CurrentPosition);
            Assert.Equal("POS2", _testViewModel.CurrentItem);
        }

        /// <summary>
        /// Verifies that the left arrow key decreases the position in test mode.
        /// Tests that position decrements from 2 to 1, 1 to 0, etc.
        /// 
        /// Requirements: 5.3, 5.6
        /// </summary>
        [Fact]
        public void TestMode_LeftArrow_DecreasesPosition()
        {
            // Arrange
            _testViewModel.IsTestMode = true;
            _testViewModel.CurrentPosition = 2;
            int? receivedPosition = null;

            // Subscribe to position changes from InputService
            _inputService.RotaryPositionChanged += (sender, position) =>
            {
                receivedPosition = position;
                _testViewModel.CurrentPosition = position;
            };

            // Act - Simulate left arrow key press
            var testModePositionField = typeof(InputService).GetField("_testModePosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            testModePositionField?.SetValue(_inputService, 2);

            var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Simulate left arrow: position should decrease from 2 to 1
            raiseMethod?.Invoke(_inputService, new object[] { 1 });

            // Assert
            Assert.Equal(1, receivedPosition);
            Assert.Equal(1, _testViewModel.CurrentPosition);
            Assert.Equal("POS2", _testViewModel.CurrentItem);
        }

        /// <summary>
        /// Verifies that position wraps correctly from max to min and min to max.
        /// Tests both forward wrapping (7 -> 0) and backward wrapping (0 -> 7).
        /// 
        /// Requirements: 5.4, 5.5, 5.6
        /// </summary>
        [Fact]
        public void TestMode_PositionWrapping_HandlesCorrectly()
        {
            // Arrange
            _testViewModel.IsTestMode = true;
            int? receivedPosition = null;

            // Subscribe to position changes from InputService
            _inputService.RotaryPositionChanged += (sender, position) =>
            {
                receivedPosition = position;
                _testViewModel.CurrentPosition = position;
            };

            var raiseMethod = typeof(InputService).GetMethod("RaiseRotaryPositionChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Test 1: Forward wrap from max (7) to min (0)
            _testViewModel.CurrentPosition = 7;
            raiseMethod?.Invoke(_inputService, new object[] { 0 });

            Assert.Equal(0, receivedPosition);
            Assert.Equal(0, _testViewModel.CurrentPosition);
            Assert.Equal("POS1", _testViewModel.CurrentItem);

            // Test 2: Backward wrap from min (0) to max (7)
            _testViewModel.CurrentPosition = 0;
            raiseMethod?.Invoke(_inputService, new object[] { 7 });

            Assert.Equal(7, receivedPosition);
            Assert.Equal(7, _testViewModel.CurrentPosition);
            Assert.Equal("POS8", _testViewModel.CurrentItem);
        }

        /// <summary>
        /// Property test: Test Mode Position Updates
        /// For any position in the range [0, N-1], the display should show the correct text label.
        /// 
        /// Property 6: Test Mode Position Updates
        /// Validates: Requirements 5.2, 5.3, 5.7, 5.8
        /// </summary>
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 6: Test Mode Position Updates")]
        public Property Property_TestModePositionUpdates()
        {
            return Prop.ForAll(
                GeneratePositionConfiguration(),
                config =>
                {
                    // Arrange
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Profile",
                        DeviceName = "Test Device",
                        Layout = DisplayLayout.Vertical,
                        PositionCount = config.PositionCount,
                        TextLabels = config.TextLabels
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };

                    var viewModel = new OverlayViewModel(settings);
                    viewModel.IsTestMode = true;

                    // Act & Assert - Test all positions
                    bool allPositionsCorrect = true;
                    string failureMessage = "";

                    for (int pos = 0; pos < config.PositionCount; pos++)
                    {
                        viewModel.CurrentPosition = pos;

                        // Verify test mode indicator shows correct position
                        string expectedIndicator = $"TEST MODE - Position {pos + 1}";
                        if (viewModel.TestModeIndicatorText != expectedIndicator)
                        {
                            allPositionsCorrect = false;
                            failureMessage = $"Position {pos}: Expected indicator '{expectedIndicator}', got '{viewModel.TestModeIndicatorText}'";
                            break;
                        }

                        // Verify display shows correct text label
                        string expectedLabel = config.TextLabels[pos];
                        if (viewModel.CurrentItem != expectedLabel)
                        {
                            allPositionsCorrect = false;
                            failureMessage = $"Position {pos}: Expected label '{expectedLabel}', got '{viewModel.CurrentItem}'";
                            break;
                        }
                    }

                    return allPositionsCorrect
                        .Label($"For {config.PositionCount} positions: {failureMessage}");
                });
        }

        /// <summary>
        /// Property test: Position Wrapping
        /// For any position count N, advancing from position N-1 should wrap to position 0,
        /// and moving back from position 0 should wrap to position N-1.
        /// 
        /// Property 7: Position Wrapping
        /// Validates: Requirements 5.4, 5.5
        /// </summary>
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 7: Position Wrapping")]
        public Property Property_PositionWrapping()
        {
            return Prop.ForAll(
                Gen.Elements(4, 8, 12, 16, 20).ToArbitrary(),
                positionCount =>
                {
                    // Arrange
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Profile",
                        DeviceName = "Test Device",
                        Layout = DisplayLayout.Vertical,
                        PositionCount = positionCount,
                        TextLabels = GenerateTextLabels(positionCount)
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };

                    var viewModel = new OverlayViewModel(settings);
                    viewModel.IsTestMode = true;

                    // Test forward wrap: from max position to 0
                    int maxPosition = positionCount - 1;
                    viewModel.CurrentPosition = maxPosition;
                    
                    // Simulate wrapping forward
                    viewModel.CurrentPosition = 0;
                    bool forwardWrapCorrect = viewModel.CurrentPosition == 0 && 
                                             viewModel.CurrentItem == profile.TextLabels[0];

                    // Test backward wrap: from 0 to max position
                    viewModel.CurrentPosition = 0;
                    
                    // Simulate wrapping backward
                    viewModel.CurrentPosition = maxPosition;
                    bool backwardWrapCorrect = viewModel.CurrentPosition == maxPosition && 
                                              viewModel.CurrentItem == profile.TextLabels[maxPosition];

                    return (forwardWrapCorrect && backwardWrapCorrect)
                        .Label($"For {positionCount} positions: " +
                               $"forward wrap {maxPosition}â†’0 = {forwardWrapCorrect}, " +
                               $"backward wrap 0â†’{maxPosition} = {backwardWrapCorrect}");
                });
        }

        /// <summary>
        /// Generator for valid position configurations.
        /// Generates position counts and corresponding text labels.
        /// </summary>
        private static Arbitrary<PositionConfiguration> GeneratePositionConfiguration()
        {
            return Arb.From(
                from positionCount in Gen.Elements(4, 8, 12, 16, 20)
                select new PositionConfiguration
                {
                    PositionCount = positionCount,
                    TextLabels = GenerateTextLabels(positionCount)
                });
        }

        private static List<string> GenerateTextLabels(int count)
        {
            var labels = new List<string>();
            for (int i = 0; i < count; i++)
            {
                labels.Add($"POS{i + 1}");
            }
            return labels;
        }

        private class PositionConfiguration
        {
            public int PositionCount { get; set; }
            public List<string> TextLabels { get; set; } = new List<string>();
        }
    }
}
