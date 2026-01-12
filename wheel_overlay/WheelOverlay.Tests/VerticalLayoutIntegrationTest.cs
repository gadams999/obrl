using System;
using System.Collections.Generic;
using System.IO;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Integration test for the vertical layout crash bug fix.
    /// Simulates the exact user workflow: clear settings, run in test mode, change to vertical layout.
    /// Requirements: 7.1, 7.2, 7.6
    /// </summary>
    [Collection("SettingsFile")]
    public class VerticalLayoutIntegrationTest
    {
        private readonly string _testSettingsPath;

        public VerticalLayoutIntegrationTest()
        {
            // Use a test-specific settings path
            _testSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WheelOverlay",
                "settings_test.json");
        }

        [Fact]
        public void UserWorkflow_ClearSettings_TestMode_ChangeToVertical_DoesNotCrash()
        {
            // Step 1: Clear settings (simulate fresh install)
            if (File.Exists(_testSettingsPath))
            {
                File.Delete(_testSettingsPath);
            }

            // Step 2: Load settings (simulates app startup)
            var settings = AppSettings.Load();
            Assert.NotNull(settings);
            Assert.NotNull(settings.ActiveProfile);

            // Step 3: Create ViewModel (simulates MainWindow initialization)
            var viewModel = new OverlayViewModel(settings);
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);

            // Step 4: Enable test mode (simulates --test-mode flag)
            viewModel.IsTestMode = true;
            Assert.True(viewModel.IsTestMode);

            // Step 5: Change to vertical layout (simulates user selecting vertical in settings)
            viewModel.Settings.ActiveProfile!.Layout = DisplayLayout.Vertical;
            
            // Step 6: Verify PopulatedPositionLabels doesn't crash
            var labels = viewModel.PopulatedPositionLabels;
            Assert.NotNull(labels);
            
            // Step 7: Verify LayoutValidator passes
            var isValid = LayoutValidator.ValidateVerticalLayout(viewModel);
            Assert.True(isValid);

            // Step 8: Simulate position changes in test mode
            for (int i = 0; i < 8; i++)
            {
                viewModel.CurrentPosition = i;
                Assert.Equal(i, viewModel.CurrentPosition);
                
                // Verify display doesn't crash
                var displayedText = viewModel.DisplayedText;
                Assert.NotNull(displayedText);
            }

            // Step 9: Save settings (simulates Apply button)
            settings.Save();

            // Step 10: Reload settings and verify vertical layout still works
            var reloadedSettings = AppSettings.Load();
            var reloadedViewModel = new OverlayViewModel(reloadedSettings);
            Assert.Equal(DisplayLayout.Vertical, reloadedViewModel.Settings.ActiveProfile!.Layout);
            
            var reloadedLabels = reloadedViewModel.PopulatedPositionLabels;
            Assert.NotNull(reloadedLabels);
            Assert.NotEmpty(reloadedLabels);
        }

        [Fact]
        public void UserWorkflow_SwitchBetweenLayouts_DoesNotCrash()
        {
            // Arrange
            var settings = AppSettings.Load();
            var viewModel = new OverlayViewModel(settings);

            // Act & Assert - Switch through all layouts
            var layouts = new[] { 
                DisplayLayout.Vertical, 
                DisplayLayout.Horizontal, 
                DisplayLayout.Grid, 
                DisplayLayout.Single 
            };

            foreach (var layout in layouts)
            {
                // Change layout
                viewModel.Settings.ActiveProfile!.Layout = layout;
                
                // Verify no crash
                var labels = viewModel.PopulatedPositionLabels;
                Assert.NotNull(labels);
                
                // Verify validator passes
                var isValid = LayoutValidator.ValidateLayout(viewModel, layout);
                Assert.True(isValid, $"Layout {layout} failed validation");
                
                // Simulate position change
                viewModel.CurrentPosition = 0;
                var displayedText = viewModel.DisplayedText;
                Assert.NotNull(displayedText);
            }
        }

        [Fact]
        public void UserWorkflow_VerticalLayoutWithEmptyProfile_DoesNotCrash()
        {
            // Arrange - Create profile with all empty labels
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Empty Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                PositionCount = 8,
                TextLabels = new List<string> { "", "", "", "", "", "", "", "" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act - Create ViewModel with vertical layout
            var viewModel = new OverlayViewModel(settings);

            // Assert - Should not crash
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.PopulatedPositionLabels);
            Assert.Empty(viewModel.PopulatedPositionLabels); // All labels are empty
            
            // Verify validator passes
            var isValid = LayoutValidator.ValidateVerticalLayout(viewModel);
            Assert.True(isValid);
        }
    }
}
