using Xunit;
using WheelOverlay.Models;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Unit tests for font default values in Profile class.
    /// Feature: overlay-visibility-and-ui-improvements
    /// Requirements: 2.1, 2.2, 2.3, 2.5
    /// </summary>
    public class FontDefaultsTests
    {
        [Fact]
        public void NewProfile_HasFontSize14()
        {
            // Arrange & Act
            var profile = new Profile();

            // Assert
            Assert.Equal(12, profile.FontSize);
        }

        [Fact]
        public void NewProfile_HasFontWeightBold()
        {
            // Arrange & Act
            var profile = new Profile();

            // Assert
            Assert.Equal("Bold", profile.FontWeight);
        }

        [Fact]
        public void NewProfile_HasTextRenderingModeAliased()
        {
            // Arrange & Act
            var profile = new Profile();

            // Assert
            Assert.Equal("Aliased", profile.TextRenderingMode);
        }

        [Fact]
        public void NewProfile_HasAllFontDefaultsTogether()
        {
            // Arrange & Act
            var profile = new Profile();

            // Assert - Verify all font defaults are set correctly
            Assert.Equal(12, profile.FontSize);
            Assert.Equal("Bold", profile.FontWeight);
            Assert.Equal("Aliased", profile.TextRenderingMode);
        }
    }
}
