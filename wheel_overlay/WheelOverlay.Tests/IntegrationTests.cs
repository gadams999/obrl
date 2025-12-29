using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using WheelOverlay.Services;
using WheelOverlay.Views;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Integration tests for About Dialog, Smart Text Display, and Test Mode features.
    /// These tests verify that all features work together correctly.
    /// Feature: about-dialog, Task 12: Integration and final testing
    /// </summary>
    public class IntegrationTests
    {
        #region About Dialog Integration Tests

        /// <summary>
        /// Test that About dialog can be created and has correct properties.
        /// Requirements: 1.1, 1.2, 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.4, 3.5, 3.6, 4.1, 4.2, 4.3, 4.5
        /// Note: This test is skipped because WPF UI components require STA thread which is not available in xUnit by default.
        /// Manual testing is required for AboutWindow UI verification.
        /// </summary>
        [Fact(Skip = "WPF UI components require STA thread - manual testing required")]
        public void Integration_AboutDialog_HasCorrectProperties()
        {
            // Arrange & Act
            var aboutWindow = new AboutWindow();

            // Assert - Verify window properties
            Assert.NotNull(aboutWindow);
            Assert.Equal(System.Windows.WindowStyle.ToolWindow, aboutWindow.WindowStyle);
            Assert.Equal(System.Windows.ResizeMode.NoResize, aboutWindow.ResizeMode);
            Assert.Equal(System.Windows.WindowStartupLocation.CenterScreen, aboutWindow.WindowStartupLocation);
            Assert.False(aboutWindow.ShowInTaskbar);
        }

        #endregion

        #region Smart Text Condensing Integration Tests

        /// <summary>
        /// Test smart condensing with various position configurations.
        /// Requirements: 6.1, 6.3, 6.4, 6.5
        /// </summary>
        [Fact]
        public void Integration_SmartCondensing_WorksWithVariousConfigurations()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "Position 1",  // 0 - populated
                    "",            // 1 - empty
                    "Position 3",  // 2 - populated
                    "",            // 3 - empty
                    "Position 5",  // 4 - populated
                    "",            // 5 - empty
                    "",            // 6 - empty
                    "Position 8"   // 7 - populated
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);

            // Act - Update populated positions
            viewModel.UpdatePopulatedPositions();

            // Assert - Only populated positions should be in the list
            var populatedPositions = viewModel.PopulatedPositions;
            Assert.NotNull(populatedPositions);
            Assert.Equal(4, populatedPositions.Count);
            
            // Verify correct positions are included
            Assert.Contains(0, populatedPositions);
            Assert.Contains(2, populatedPositions);
            Assert.Contains(4, populatedPositions);
            Assert.Contains(7, populatedPositions);
            
            // Verify empty positions are excluded
            Assert.DoesNotContain(1, populatedPositions);
            Assert.DoesNotContain(3, populatedPositions);
            Assert.DoesNotContain(5, populatedPositions);
            Assert.DoesNotContain(6, populatedPositions);
        }

        /// <summary>
        /// Test that position numbers are preserved after filtering.
        /// Requirements: 6.4
        /// </summary>
        [Fact]
        public void Integration_SmartCondensing_PreservesPositionNumbers()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "",            // 0 - empty
                    "Second",      // 1 - populated
                    "",            // 2 - empty
                    "Fourth",      // 3 - populated
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "Seventh",     // 6 - populated
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);

            // Act
            viewModel.UpdatePopulatedPositions();

            // Assert - Position numbers should match original indices
            var populatedPositions = viewModel.PopulatedPositions;
            Assert.Equal(3, populatedPositions.Count);
            Assert.Equal(1, populatedPositions[0]);
            Assert.Equal(3, populatedPositions[1]);
            Assert.Equal(6, populatedPositions[2]);
        }

        #endregion

        #region Flash Animation Integration Tests

        /// <summary>
        /// Test flash animations trigger on empty position selection.
        /// Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 8.3
        /// </summary>
        [Fact]
        public async Task Integration_FlashAnimation_TriggersOnEmptyPositionSelection()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "Position 1",  // 0 - populated
                    "",            // 1 - empty
                    "Position 3",  // 2 - populated
                    "",            // 3 - empty
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "",            // 6 - empty
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            viewModel.UpdatePopulatedPositions();

            // Act - Select empty position
            viewModel.CurrentPosition = 1;

            // Assert - Flash should be triggered
            Assert.True(viewModel.IsFlashing);

            // Wait for flash duration
            await Task.Delay(600); // 500ms + buffer

            // Assert - Flash should stop after duration
            Assert.False(viewModel.IsFlashing);
        }

        /// <summary>
        /// Test that flash stops when populated position is selected.
        /// Requirements: 7.4, 8.5
        /// </summary>
        [Fact]
        public async Task Integration_FlashAnimation_StopsOnPopulatedSelection()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "Position 1",  // 0 - populated
                    "",            // 1 - empty
                    "Position 3",  // 2 - populated
                    "",            // 3 - empty
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "",            // 6 - empty
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            viewModel.UpdatePopulatedPositions();

            // Act - Select empty position to start flash
            viewModel.CurrentPosition = 1;
            Assert.True(viewModel.IsFlashing);

            // Select populated position
            await Task.Delay(100); // Small delay to ensure flash is running
            viewModel.CurrentPosition = 2;

            // Assert - Flash should stop immediately
            Assert.False(viewModel.IsFlashing);
        }

        /// <summary>
        /// Test that flash restarts when another empty position is selected.
        /// Requirements: 7.5
        /// </summary>
        [Fact]
        public async Task Integration_FlashAnimation_RestartsOnAnotherEmptySelection()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "Position 1",  // 0 - populated
                    "",            // 1 - empty
                    "",            // 2 - empty
                    "",            // 3 - empty
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "",            // 6 - empty
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            viewModel.UpdatePopulatedPositions();

            // Act - Select first empty position
            viewModel.CurrentPosition = 1;
            Assert.True(viewModel.IsFlashing);

            await Task.Delay(200); // Wait partway through flash

            // Select another empty position
            viewModel.CurrentPosition = 3;

            // Assert - Flash should still be active (restarted)
            Assert.True(viewModel.IsFlashing);

            // Wait for full duration
            await Task.Delay(600);

            // Assert - Flash should eventually stop
            Assert.False(viewModel.IsFlashing);
        }

        #endregion

        #region Single Layout Integration Tests

        /// <summary>
        /// Test Single layout displays last populated position when empty position is selected.
        /// Requirements: 8.1, 8.2
        /// </summary>
        [Fact]
        public void Integration_SingleLayout_DisplaysLastPopulatedOnEmpty()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "First",       // 0 - populated
                    "",            // 1 - empty
                    "Third",       // 2 - populated
                    "",            // 3 - empty
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "",            // 6 - empty
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            viewModel.UpdatePopulatedPositions();

            // Act - Select populated position first
            viewModel.CurrentPosition = 2;
            Assert.Equal(2, viewModel.LastPopulatedPosition);

            // Select empty position
            viewModel.CurrentPosition = 3;

            // Assert - Should still show last populated position
            Assert.Equal(2, viewModel.LastPopulatedPosition);
            Assert.Equal(3, viewModel.CurrentPosition);
        }

        /// <summary>
        /// Test Single layout startup with empty first position.
        /// Requirements: 8.6
        /// </summary>
        [Fact]
        public void Integration_SingleLayout_StartupWithEmptyFirstPosition()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "",            // 0 - empty
                    "",            // 1 - empty
                    "Third",       // 2 - populated
                    "Fourth",      // 3 - populated
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "",            // 6 - empty
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            // Act
            var viewModel = new OverlayViewModel(settings);
            viewModel.UpdatePopulatedPositions();

            // Assert - Should initialize to first populated position
            Assert.Equal(2, viewModel.LastPopulatedPosition);
        }

        #endregion

        #region Test Mode Integration Tests

        /// <summary>
        /// Test test mode activation.
        /// Requirements: 9.1, 9.2, 9.8
        /// </summary>
        [Fact]
        public void Integration_TestMode_CanBeEnabled()
        {
            // Arrange
            var inputService = new InputService();

            // Act - Enable test mode programmatically
            inputService.TestMode = true;

            // Assert
            Assert.True(inputService.TestMode);
            
            // Cleanup
            inputService.TestMode = false;
        }

        #endregion

        #region Test Mode Indicator Tests

        /// <summary>
        /// Test that test mode indicator is hidden when test mode is disabled.
        /// Requirements: 9.7
        /// </summary>
        [Fact]
        public void Integration_TestModeIndicator_HiddenWhenDisabled()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            var inputService = new InputService();

            // Act - Ensure test mode is disabled
            inputService.TestMode = false;
            viewModel.IsTestMode = inputService.TestMode;

            // Assert - IsTestMode should be false
            Assert.False(viewModel.IsTestMode);
            
            // Cleanup
            inputService.Dispose();
        }

        /// <summary>
        /// Test that test mode indicator is shown when test mode is enabled.
        /// Requirements: 9.7
        /// </summary>
        [Fact]
        public void Integration_TestModeIndicator_ShownWhenEnabled()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            var inputService = new InputService();

            // Act - Enable test mode
            inputService.TestMode = true;
            viewModel.IsTestMode = inputService.TestMode;

            // Assert - IsTestMode should be true
            Assert.True(viewModel.IsTestMode);
            
            // Cleanup
            inputService.TestMode = false;
            inputService.Dispose();
        }

        /// <summary>
        /// Test that ViewModel initializes with test mode disabled by default.
        /// Requirements: 9.7
        /// </summary>
        [Fact]
        public void Integration_TestModeIndicator_DefaultsToDisabled()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            // Act - Create ViewModel without setting test mode
            var viewModel = new OverlayViewModel(settings);

            // Assert - IsTestMode should default to false
            Assert.False(viewModel.IsTestMode);
        }

        #endregion

        #region Full Integration Tests

        /// <summary>
        /// Test all features working together: test mode + smart condensing + flash animation.
        /// Requirements: All
        /// </summary>
        [Fact]
        public async Task Integration_AllFeatures_WorkTogether()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "DASH",        // 0 - populated
                    "",            // 1 - empty
                    "MAP",         // 2 - populated
                    "",            // 3 - empty
                    "BRGT",        // 4 - populated
                    "",            // 5 - empty
                    "",            // 6 - empty
                    "DIFF"         // 7 - populated
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            var inputService = new InputService();

            // Wire up input service to view model
            inputService.RotaryPositionChanged += (sender, position) =>
            {
                viewModel.CurrentPosition = position;
            };

            viewModel.UpdatePopulatedPositions();
            inputService.TestMode = true;

            // Act & Assert - Test sequence of operations

            // 1. Simulate position 0 (populated)
            viewModel.CurrentPosition = 0;
            Assert.False(viewModel.IsFlashing);
            Assert.Equal(0, viewModel.LastPopulatedPosition);

            // 2. Simulate position 1 (empty)
            viewModel.CurrentPosition = 1;
            Assert.True(viewModel.IsFlashing);
            Assert.Equal(0, viewModel.LastPopulatedPosition);

            // 3. Move to populated position
            await Task.Delay(100);
            viewModel.CurrentPosition = 2;
            Assert.False(viewModel.IsFlashing);
            Assert.Equal(2, viewModel.LastPopulatedPosition);

            // 4. Move to another empty position
            viewModel.CurrentPosition = 3;
            Assert.True(viewModel.IsFlashing);
            Assert.Equal(2, viewModel.LastPopulatedPosition);

            // 5. Verify populated positions are filtered correctly
            var populatedPositions = viewModel.PopulatedPositions;
            Assert.Equal(4, populatedPositions.Count);
            Assert.Contains(0, populatedPositions);
            Assert.Contains(2, populatedPositions);
            Assert.Contains(4, populatedPositions);
            Assert.Contains(7, populatedPositions);

            // 6. Test wrap-around behavior
            viewModel.CurrentPosition = 0;
            Assert.False(viewModel.IsFlashing);
            Assert.Equal(0, viewModel.LastPopulatedPosition);

            // Cleanup
            inputService.TestMode = false;
        }

        /// <summary>
        /// Test configuration changes are reflected immediately in all features.
        /// Requirements: 6.5
        /// </summary>
        [Fact]
        public void Integration_ConfigurationChange_UpdatesAllFeatures()
        {
            // Arrange
            var settings = new AppSettings();
            var profile = new Profile
            {
                Name = "Test Profile",
                DeviceName = "Test Device",
                TextLabels = new List<string>
                {
                    "Position 1",  // 0 - populated
                    "",            // 1 - empty
                    "Position 3",  // 2 - populated
                    "",            // 3 - empty
                    "",            // 4 - empty
                    "",            // 5 - empty
                    "",            // 6 - empty
                    ""             // 7 - empty
                }
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;

            var viewModel = new OverlayViewModel(settings);
            viewModel.UpdatePopulatedPositions();

            // Verify initial state
            Assert.Equal(2, viewModel.PopulatedPositions.Count);

            // Act - Change configuration
            profile.TextLabels[1] = "New Position 2";
            profile.TextLabels[4] = "New Position 5";
            viewModel.UpdatePopulatedPositions();

            // Assert - Changes should be reflected immediately
            Assert.Equal(4, viewModel.PopulatedPositions.Count);
            Assert.Contains(1, viewModel.PopulatedPositions);
            Assert.Contains(4, viewModel.PopulatedPositions);
        }

        #endregion
    }
}
