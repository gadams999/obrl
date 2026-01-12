using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class OverlayViewModelTests
    {
        // Test flash lasts approximately 500ms
        // Requirements: 7.3, 8.4
        [Fact]
        public async Task FlashAnimation_Duration_ShouldBeApproximately500ms()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                TextLabels = new List<string> { "DASH", "", "TC2" } // Position 1 is empty
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Verify position 1 is empty
            Assert.DoesNotContain(1, viewModel.PopulatedPositions);

            // Start at a populated position
            viewModel.CurrentPosition = 0;
            Assert.False(viewModel.IsFlashing, "Should not be flashing at populated position");

            // Act - trigger flash and measure when it starts
            var startTime = DateTime.UtcNow;
            viewModel.CurrentPosition = 1; // Select empty position to trigger flash

            // Assert - flash should be active immediately
            Assert.True(viewModel.IsFlashing, "Flash should be active immediately after triggering");

            // Wait for flash to complete with generous timeout for CI environments
            var timeout = TimeSpan.FromSeconds(3);
            var checkInterval = TimeSpan.FromMilliseconds(50);
            var elapsed = TimeSpan.Zero;
            
            while (viewModel.IsFlashing && elapsed < timeout)
            {
                await Task.Delay(checkInterval);
                elapsed = DateTime.UtcNow - startTime;
            }

            var actualDuration = elapsed.TotalMilliseconds;

            // Assert - flash should stop after approximately 500ms
            Assert.False(viewModel.IsFlashing, "Flash should stop after approximately 500ms");
            Assert.InRange(actualDuration, 450, 1200); // 500ms with tolerance for CI environments
        }

        // Test first position empty displays first populated position
        // Requirements: 8.6
        [Fact]
        public void StartupEmptyPosition_ShouldDisplayFirstPopulatedPosition()
        {
            // Arrange - create profile where first position is empty
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "", "TC2", "MAP", "FUEL" } // Position 0 is empty
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act - create ViewModel (simulates startup)
            var viewModel = new OverlayViewModel(settings);

            // Assert - LastPopulatedPosition should be initialized to first populated position (position 1)
            Assert.Equal(1, viewModel.LastPopulatedPosition);

            // When we set current position to the empty first position
            viewModel.CurrentPosition = 0;

            // DisplayedText should show the first populated position's text
            Assert.Equal("TC2", viewModel.DisplayedText);
            
            // IsDisplayingEmptyPosition should be true
            Assert.True(viewModel.IsDisplayingEmptyPosition);
        }

        // Test GetTextForPosition returns correct text for populated positions
        // Requirements: 2.4, 2.5
        [Fact]
        public void GetTextForPosition_PopulatedPosition_ReturnsCorrectText()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                PositionCount = 5,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRAKE" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act & Assert
            Assert.Equal("DASH", viewModel.GetTextForPosition(0));
            Assert.Equal("TC2", viewModel.GetTextForPosition(1));
            Assert.Equal("MAP", viewModel.GetTextForPosition(2));
            Assert.Equal("FUEL", viewModel.GetTextForPosition(3));
            Assert.Equal("BRAKE", viewModel.GetTextForPosition(4));
        }

        // Test GetTextForPosition returns position number for empty positions
        // Requirements: 2.4, 2.5
        [Fact]
        public void GetTextForPosition_EmptyPosition_ReturnsPositionNumber()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                PositionCount = 5,
                TextLabels = new List<string> { "DASH", "", "MAP", "   ", "BRAKE" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act & Assert
            Assert.Equal("2", viewModel.GetTextForPosition(1)); // Empty string at position 1
            Assert.Equal("4", viewModel.GetTextForPosition(3)); // Whitespace at position 3
        }

        // Test GetTextForPosition returns empty string for out-of-range positions
        // Requirements: 2.4, 2.5
        [Fact]
        public void GetTextForPosition_OutOfRange_ReturnsEmptyString()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                PositionCount = 5,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRAKE" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act & Assert
            Assert.Equal("", viewModel.GetTextForPosition(-1)); // Negative position
            Assert.Equal("", viewModel.GetTextForPosition(5));  // Position >= PositionCount
            Assert.Equal("", viewModel.GetTextForPosition(10)); // Far out of range
        }

        // Test GetTextForPosition handles null profile gracefully
        // Requirements: 2.4, 2.5, 7.1, 7.7
        [Fact]
        public void GetTextForPosition_NullProfile_CreatesDefaultProfile()
        {
            // Arrange - create settings with no active profile
            var settings = new AppSettings
            {
                Profiles = new List<Profile>()
            };

            var viewModel = new OverlayViewModel(settings);

            // Act & Assert - With the vertical layout fix, a default profile is now created
            // So GetTextForPosition should return the default profile's text labels
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.NotEmpty(viewModel.Settings.ActiveProfile.TextLabels);
            
            // Should return the first label from the default profile
            Assert.Equal("POS1", viewModel.GetTextForPosition(0));
            Assert.Equal("POS2", viewModel.GetTextForPosition(1));
            
            // Out of range should still return empty string
            Assert.Equal("", viewModel.GetTextForPosition(-1));
        }
    }
}
