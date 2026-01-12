using System;
using WheelOverlay.Tests.Infrastructure;
using Xunit;

namespace WheelOverlay.Tests.Infrastructure
{
    /// <summary>
    /// Example test class demonstrating how to use UITestBase.
    /// This serves as documentation and a basic smoke test for the infrastructure.
    /// 
    /// Note: These tests use SetupTestViewModel() which doesn't require STA threading.
    /// Full UI automation tests with MainWindow will be implemented in future tasks
    /// and will require [STAFact] attributes.
    /// </summary>
    public class UITestBaseExample : UITestBase
    {
        [Fact]
        public void UITestBase_SetupTestViewModel_CreatesValidTestEnvironment()
        {
            // Arrange & Act
            SetupTestViewModel();

            // Assert
            Assert.NotNull(TestSettings);
            Assert.NotNull(TestViewModel);
            Assert.NotNull(TestSettings.ActiveProfile);
            Assert.Equal(8, TestSettings.ActiveProfile.PositionCount);
            Assert.Equal(8, TestSettings.ActiveProfile.TextLabels.Count);
        }

        [Fact]
        public void UITestBase_CreateTestSettings_ReturnsValidSettings()
        {
            // Act
            var settings = CreateTestSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.NotNull(settings.Profiles);
            Assert.Single(settings.Profiles);
            Assert.NotNull(settings.ActiveProfile);
            Assert.Equal("Test Profile", settings.ActiveProfile.Name);
            Assert.Equal("Test Device", settings.ActiveProfile.DeviceName);
            Assert.Equal(8, settings.ActiveProfile.PositionCount);
        }

        [Fact]
        public void UITestBase_TestViewModel_CanAccessProperties()
        {
            // Arrange
            SetupTestViewModel();

            // Act & Assert
            Assert.NotNull(TestViewModel);
            Assert.Equal(0, TestViewModel.CurrentPosition);
            Assert.NotNull(TestViewModel.Settings);
            Assert.Equal("POS1", TestViewModel.GetTextForPosition(0));
            Assert.Equal("POS2", TestViewModel.GetTextForPosition(1));
        }
    }
}
