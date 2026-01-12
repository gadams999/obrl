using System;
using System.Collections.Generic;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for the vertical layout crash bug fix.
    /// Validates null-safety checks and proper handling of edge cases.
    /// Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7
    /// </summary>
    public class VerticalLayoutFixTests
    {
        // Test OverlayViewModel with null settings
        // Requirements: 7.1, 7.7
        [Fact]
        public void OverlayViewModel_NullSettings_CreatesDefaultSettings()
        {
            // Act - pass null settings to constructor
            var viewModel = new OverlayViewModel(null!);

            // Assert - ViewModel should have valid settings
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.NotNull(viewModel.Settings.ActiveProfile.TextLabels);
            Assert.NotEmpty(viewModel.Settings.ActiveProfile.TextLabels);
        }

        // Test OverlayViewModel with settings but no profiles
        // Requirements: 7.1, 7.2, 7.7
        [Fact]
        public void OverlayViewModel_SettingsWithNoProfiles_CreatesDefaultProfile()
        {
            // Arrange - create settings with empty profiles list
            var settings = new AppSettings
            {
                Profiles = new List<Profile>()
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert - ViewModel should create a default profile
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.NotNull(viewModel.Settings.ActiveProfile.TextLabels);
            Assert.NotEmpty(viewModel.Settings.ActiveProfile.TextLabels);
            Assert.Single(viewModel.Settings.Profiles);
            Assert.Equal("Default", viewModel.Settings.ActiveProfile.Name);
        }

        // Test OverlayViewModel with null profile
        // Requirements: 7.1, 7.2, 7.7
        [Fact]
        public void OverlayViewModel_NullActiveProfile_CreatesDefaultProfile()
        {
            // Arrange - create settings with profiles but no selected profile
            var settings = new AppSettings
            {
                Profiles = new List<Profile>(),
                SelectedProfileId = Guid.Empty
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert - ViewModel should create a default profile
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.NotNull(viewModel.Settings.ActiveProfile.TextLabels);
            Assert.NotEmpty(viewModel.Settings.ActiveProfile.TextLabels);
        }

        // Test PopulatedPositionLabels with empty labels
        // Requirements: 7.3
        [Fact]
        public void PopulatedPositionLabels_EmptyLabels_ReturnsEmptyList()
        {
            // Arrange - create profile with all empty labels
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Empty Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                TextLabels = new List<string> { "", "", "", "" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            var labels = viewModel.PopulatedPositionLabels;

            // Assert - should return empty list, not crash
            Assert.NotNull(labels);
            Assert.Empty(labels);
        }

        // Test PopulatedPositionLabels with whitespace labels
        // Requirements: 7.3
        [Fact]
        public void PopulatedPositionLabels_WhitespaceLabels_ReturnsEmptyList()
        {
            // Arrange - create profile with whitespace-only labels
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Whitespace Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                TextLabels = new List<string> { "   ", "\t", "\n", "  \t  " }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            var labels = viewModel.PopulatedPositionLabels;

            // Assert - should return empty list, not crash
            Assert.NotNull(labels);
            Assert.Empty(labels);
        }

        // Test PopulatedPositionLabels with mixed empty and populated labels
        // Requirements: 7.3
        [Fact]
        public void PopulatedPositionLabels_MixedLabels_ReturnsOnlyPopulated()
        {
            // Arrange - create profile with mix of empty and populated labels
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Mixed Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                TextLabels = new List<string> { "DASH", "", "MAP", "   ", "FUEL", "" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            var labels = viewModel.PopulatedPositionLabels;

            // Assert - should return only populated labels
            Assert.NotNull(labels);
            Assert.Equal(3, labels.Count);
            Assert.Contains("DASH", labels);
            Assert.Contains("MAP", labels);
            Assert.Contains("FUEL", labels);
        }

        // Test Settings property setter with null value
        // Requirements: 7.1, 7.7
        [Fact]
        public void Settings_SetToNull_DoesNotCrash()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                TextLabels = new List<string> { "DASH", "TC2", "MAP" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act - set settings to null
            viewModel.Settings = null!;

            // Assert - should not crash and should have valid settings
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);
        }

        // Test LayoutValidator with null ViewModel
        // Requirements: 7.4, 7.5
        [Fact]
        public void LayoutValidator_NullViewModel_ReturnsFalse()
        {
            // Act
            var result = LayoutValidator.ValidateVerticalLayout(null!);

            // Assert
            Assert.False(result);
        }

        // Test LayoutValidator with null Settings
        // Requirements: 7.4, 7.5
        [Fact]
        public void LayoutValidator_NullSettings_ReturnsFalse()
        {
            // Arrange - create ViewModel with null settings (will be replaced with defaults)
            var viewModel = new OverlayViewModel(null!);
            
            // Manually set settings to null using reflection to test validation
            var settingsField = typeof(OverlayViewModel).GetField("_settings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            settingsField?.SetValue(viewModel, null);

            // Act
            var result = LayoutValidator.ValidateVerticalLayout(viewModel);

            // Assert
            Assert.False(result);
        }

        // Test LayoutValidator with valid ViewModel
        // Requirements: 7.4, 7.5
        [Fact]
        public void LayoutValidator_ValidViewModel_ReturnsTrue()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                TextLabels = new List<string> { "DASH", "TC2", "MAP" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            var result = LayoutValidator.ValidateVerticalLayout(viewModel);

            // Assert
            Assert.True(result);
        }

        // Test LayoutValidator for all layout types
        // Requirements: 7.4, 7.5
        [Theory]
        [InlineData(DisplayLayout.Vertical)]
        [InlineData(DisplayLayout.Horizontal)]
        [InlineData(DisplayLayout.Grid)]
        [InlineData(DisplayLayout.Single)]
        public void LayoutValidator_AllLayoutTypes_ValidatesCorrectly(DisplayLayout layout)
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = layout,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL" },
                GridRows = 2,
                GridColumns = 2
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            var result = LayoutValidator.ValidateLayout(viewModel, layout);

            // Assert
            Assert.True(result);
        }

        // Test LayoutValidator with invalid grid dimensions
        // Requirements: 7.4, 7.5
        [Fact]
        public void LayoutValidator_InvalidGridDimensions_ReturnsFalse()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL" },
                GridRows = 0,  // Invalid
                GridColumns = 0  // Invalid
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            var result = LayoutValidator.ValidateGridLayout(viewModel);

            // Assert
            Assert.False(result);
        }

        // Test fresh install scenario with vertical layout
        // Requirements: 7.1, 7.2, 7.6
        [Fact]
        public void FreshInstall_VerticalLayout_DoesNotCrash()
        {
            // Arrange - simulate fresh install with no settings file
            var settings = AppSettings.Load(); // This simulates first run

            // Act - create ViewModel and select vertical layout
            var viewModel = new OverlayViewModel(settings);
            viewModel.Settings.ActiveProfile!.Layout = DisplayLayout.Vertical;

            // Assert - should not crash and should have valid data
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.NotNull(viewModel.PopulatedPositionLabels);
            Assert.True(LayoutValidator.ValidateVerticalLayout(viewModel));
        }
    }
}
