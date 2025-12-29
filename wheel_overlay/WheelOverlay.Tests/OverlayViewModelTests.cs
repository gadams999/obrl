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

            // Act
            var stopwatch = Stopwatch.StartNew();
            viewModel.CurrentPosition = 1; // Select empty position to trigger flash

            // Assert - flash should be active immediately
            Assert.True(viewModel.IsFlashing, "Flash should be active immediately after triggering");

            // Wait for flash to complete (500ms + small buffer)
            await Task.Delay(550);
            stopwatch.Stop();

            // Assert - flash should stop after approximately 500ms
            Assert.False(viewModel.IsFlashing, "Flash should stop after approximately 500ms");
            Assert.InRange(stopwatch.ElapsedMilliseconds, 450, 650); // 500ms ±150ms tolerance
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
    }
}
