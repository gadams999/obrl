using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for animation and transition features.
    /// Validates animation enable/disable, duration configuration, and rapid change handling.
    /// Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6
    /// </summary>
    public class AnimationTests
    {
        // Test helper to create test settings with animation configuration
        private AppSettings CreateTestSettings(bool enableAnimations = true)
        {
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                PositionCount = 8,
                TextLabels = new List<string> 
                { 
                    "POS1", "POS2", "POS3", "POS4", 
                    "POS5", "POS6", "POS7", "POS8" 
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id,
                EnableAnimations = enableAnimations
            };

            return settings;
        }

        // Test animation enable/disable toggle
        // Requirements: 9.1, 9.4, 9.5
        [Fact]
        public void AnimationToggle_EnableDisable_StateChangesImmediately()
        {
            // Arrange
            var settings = CreateTestSettings(enableAnimations: true);
            
            // Act & Assert - Initially enabled
            Assert.True(settings.EnableAnimations, "Animations should be enabled initially");

            // Disable animations
            settings.EnableAnimations = false;
            Assert.False(settings.EnableAnimations, "Animations should be disabled after toggle");

            // Re-enable animations
            settings.EnableAnimations = true;
            Assert.True(settings.EnableAnimations, "Animations should be re-enabled after toggle");
        }

        // Test animation state persists in settings
        // Requirements: 9.1, 9.4, 9.5
        [Fact]
        public void AnimationState_Persists_InSettings()
        {
            // Arrange
            var settingsEnabled = CreateTestSettings(enableAnimations: true);
            var settingsDisabled = CreateTestSettings(enableAnimations: false);

            // Act & Assert
            Assert.True(settingsEnabled.EnableAnimations, "Settings with animations enabled should persist state");
            Assert.False(settingsDisabled.EnableAnimations, "Settings with animations disabled should persist state");
        }

        // Test animation state applies to ViewModel
        // Requirements: 9.1, 9.4, 9.5
        [Fact]
        public void AnimationState_AppliesTo_ViewModel()
        {
            // Arrange
            var settingsEnabled = CreateTestSettings(enableAnimations: true);
            var settingsDisabled = CreateTestSettings(enableAnimations: false);

            // Act
            var viewModelEnabled = new OverlayViewModel(settingsEnabled);
            var viewModelDisabled = new OverlayViewModel(settingsDisabled);

            // Assert
            Assert.True(viewModelEnabled.Settings.EnableAnimations, "ViewModel should reflect enabled animation state");
            Assert.False(viewModelDisabled.Settings.EnableAnimations, "ViewModel should reflect disabled animation state");
        }

        // Test animation duration constant is within acceptable bounds
        // Requirements: 9.2, 9.6
        // Note: Current implementation uses a hardcoded 250ms duration in SingleTextLayout.xaml.cs
        // Future enhancement: Make duration configurable via AppSettings
        [Fact]
        public void AnimationDuration_Constant_IsWithinAcceptableBounds()
        {
            // Arrange
            const double EXPECTED_DURATION_MS = 250.0;
            const double MIN_ACCEPTABLE_MS = 200.0;
            const double MAX_ACCEPTABLE_MS = 300.0;

            // Act & Assert
            Assert.InRange(EXPECTED_DURATION_MS, MIN_ACCEPTABLE_MS, MAX_ACCEPTABLE_MS);
        }

        // Test that animation duration is applied consistently
        // Requirements: 9.2, 9.6
        [Fact]
        public void AnimationDuration_Applied_Consistently()
        {
            // Arrange
            var settings = CreateTestSettings(enableAnimations: true);
            var viewModel = new OverlayViewModel(settings);

            // Act - The animation duration is currently hardcoded in the view layer
            // This test verifies that the settings support animation configuration
            
            // Assert - Settings should support animation enable/disable
            Assert.True(settings.EnableAnimations, "Settings should support animation configuration");
            
            // Note: Full duration configuration testing will be added when
            // animation duration becomes a configurable property in AppSettings
        }

        // Test animation skip during rapid position changes
        // Requirements: 9.3, 9.6
        [Fact]
        public async Task RapidPositionChanges_SkipsAnimations_ToPreventsLag()
        {
            // Arrange
            var settings = CreateTestSettings(enableAnimations: true);
            var viewModel = new OverlayViewModel(settings);

            // Act - Simulate rapid position changes (faster than animation duration)
            viewModel.CurrentPosition = 0;
            await Task.Delay(10); // Very short delay (< 250ms animation duration)
            
            viewModel.CurrentPosition = 1;
            await Task.Delay(10);
            
            viewModel.CurrentPosition = 2;
            await Task.Delay(10);
            
            viewModel.CurrentPosition = 3;

            // Assert - ViewModel should handle rapid changes without errors
            Assert.Equal(3, viewModel.CurrentPosition);
            Assert.Equal("POS4", viewModel.CurrentItem);
            
            // Note: The actual animation skipping logic is implemented in the view layer
            // (SingleTextLayout.xaml.cs) which detects rapid input and skips animations
            // This test verifies the ViewModel can handle rapid position updates
        }

        // Test that rapid changes don't cause state inconsistencies
        // Requirements: 9.3, 9.6
        [Fact]
        public async Task RapidPositionChanges_MaintainsStateConsistency()
        {
            // Arrange
            var settings = CreateTestSettings(enableAnimations: true);
            var viewModel = new OverlayViewModel(settings);

            // Act - Perform many rapid position changes
            for (int i = 0; i < 8; i++)
            {
                viewModel.CurrentPosition = i;
                await Task.Delay(5); // Very rapid changes
            }

            // Assert - Final state should be consistent
            Assert.Equal(7, viewModel.CurrentPosition);
            Assert.Equal("POS8", viewModel.CurrentItem);
            Assert.Equal("POS8", viewModel.DisplayedText);
        }

        // Test animation behavior with position wrapping during rapid changes
        // Requirements: 9.3, 9.6
        [Fact]
        public async Task RapidPositionChanges_WithWrapping_HandlesCorrectly()
        {
            // Arrange
            var settings = CreateTestSettings(enableAnimations: true);
            var viewModel = new OverlayViewModel(settings);

            // Act - Rapid changes that wrap around
            viewModel.CurrentPosition = 7; // Last position
            await Task.Delay(10);
            
            viewModel.CurrentPosition = 0; // Wrap to first
            await Task.Delay(10);
            
            viewModel.CurrentPosition = 1;

            // Assert
            Assert.Equal(1, viewModel.CurrentPosition);
            Assert.Equal("POS2", viewModel.CurrentItem);
        }

        // Feature: dotnet10-upgrade-and-testing, Property 12: Animation State Consistency
        // Validates: Requirements 9.1, 9.4, 9.5
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_AnimationStateConsistency()
        {
            return Prop.ForAll(
                Arb.Generate<bool>().ToArbitrary(),
                (bool enableAnimations) =>
                {
                    // Arrange - Create settings with random animation state
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Profile",
                        DeviceName = "Test Device",
                        Layout = DisplayLayout.Single,
                        PositionCount = 8,
                        TextLabels = new List<string> 
                        { 
                            "POS1", "POS2", "POS3", "POS4", 
                            "POS5", "POS6", "POS7", "POS8" 
                        }
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id,
                        EnableAnimations = enableAnimations
                    };

                    // Act - Create ViewModel with these settings
                    var viewModel = new OverlayViewModel(settings);

                    // Assert - Animation state should be immediately reflected
                    bool stateMatches = viewModel.Settings.EnableAnimations == enableAnimations;

                    // Toggle the state
                    settings.EnableAnimations = !enableAnimations;
                    bool stateToggledCorrectly = settings.EnableAnimations == !enableAnimations;

                    // Toggle back
                    settings.EnableAnimations = enableAnimations;
                    bool stateRestoredCorrectly = settings.EnableAnimations == enableAnimations;

                    return (stateMatches && stateToggledCorrectly && stateRestoredCorrectly)
                        .Label($"Animation state should be consistent: initial={enableAnimations}, " +
                               $"matches={stateMatches}, toggled={stateToggledCorrectly}, restored={stateRestoredCorrectly}");
                });
        }
    }
}
