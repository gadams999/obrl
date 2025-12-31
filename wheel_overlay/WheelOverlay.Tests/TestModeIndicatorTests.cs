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

        // Test indicator shows current position number
        // Requirements: 10.4
        [Fact]
        public void TestModeIndicatorText_WhenTestModeActive_ShouldShowCurrentPosition()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "POS5", "POS6", "POS7", "POS8" },
                PositionCount = 8
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);
            viewModel.IsTestMode = true;

            // Act & Assert - Test different positions
            viewModel.CurrentPosition = 0;
            Assert.Equal("TEST MODE - Position 1", viewModel.TestModeIndicatorText);

            viewModel.CurrentPosition = 3;
            Assert.Equal("TEST MODE - Position 4", viewModel.TestModeIndicatorText);

            viewModel.CurrentPosition = 7;
            Assert.Equal("TEST MODE - Position 8", viewModel.TestModeIndicatorText);
        }

        [Fact]
        public void TestModeIndicatorText_WhenTestModeInactive_ShouldBeEmpty()
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
            viewModel.IsTestMode = false;
            viewModel.CurrentPosition = 2;

            // Act
            string indicatorText = viewModel.TestModeIndicatorText;

            // Assert
            Assert.Equal(string.Empty, indicatorText);
        }

        [Fact]
        public void TestModeIndicatorText_WhenPositionChanges_ShouldUpdateText()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Single,
                TextLabels = new List<string> { "DASH", "TC2", "MAP", "FUEL", "POS5" },
                PositionCount = 5
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);
            viewModel.IsTestMode = true;
            viewModel.CurrentPosition = 0;

            bool propertyChangedRaised = false;
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.TestModeIndicatorText))
                {
                    propertyChangedRaised = true;
                }
            };

            // Act
            viewModel.CurrentPosition = 4;

            // Assert
            Assert.True(propertyChangedRaised, "PropertyChanged should be raised for TestModeIndicatorText when position changes");
            Assert.Equal("TEST MODE - Position 5", viewModel.TestModeIndicatorText);
        }
    }
}
