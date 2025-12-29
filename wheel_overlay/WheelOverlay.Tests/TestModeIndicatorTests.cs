using System;
using System.Collections.Generic;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class TestModeIndicatorTests
    {
        // Test indicator displays when test mode is active
        // Test indicator hidden when test mode is disabled
        // Requirements: 9.7
        [Fact]
        public void IsTestMode_WhenTrue_ShouldBeVisible()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            viewModel.IsTestMode = true;

            // Assert
            Assert.True(viewModel.IsTestMode, "IsTestMode should be true when set to true");
        }

        [Fact]
        public void IsTestMode_WhenFalse_ShouldBeHidden()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act
            viewModel.IsTestMode = false;

            // Assert
            Assert.False(viewModel.IsTestMode, "IsTestMode should be false when set to false");
        }

        [Fact]
        public void IsTestMode_DefaultValue_ShouldBeFalse()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert
            Assert.False(viewModel.IsTestMode, "IsTestMode should default to false");
        }

        [Fact]
        public void IsTestMode_PropertyChanged_ShouldRaiseEvent()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL" }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);
            bool propertyChangedRaised = false;
            string? changedPropertyName = null;

            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.IsTestMode))
                {
                    propertyChangedRaised = true;
                    changedPropertyName = args.PropertyName;
                }
            };

            // Act
            viewModel.IsTestMode = true;

            // Assert
            Assert.True(propertyChangedRaised, "PropertyChanged event should be raised when IsTestMode changes");
            Assert.Equal(nameof(viewModel.IsTestMode), changedPropertyName);
        }
    }
}
