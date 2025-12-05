using Xunit;
using WheelOverlay.Models;

namespace WheelOverlay.Tests
{
    public class AppSettingsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var settings = new AppSettings();
            Assert.Equal(DisplayLayout.Single, settings.Layout);
            Assert.Equal(8, settings.TextLabels.Length);
            Assert.Equal("#FFFFFF", settings.SelectedTextColor);
        }

        [Fact]
        public void Labels_CanBeUpdated()
        {
            var settings = new AppSettings();
            settings.TextLabels[0] = "TEST";
            Assert.Equal("TEST", settings.TextLabels[0]);
        }
    }
}
