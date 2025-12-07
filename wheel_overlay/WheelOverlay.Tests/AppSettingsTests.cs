using Xunit;
using System.Linq;
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

        [Fact]
        public void LegacyJson_MigratesToprofile()
        {
            var legacyJson = @"{
                ""Layout"": ""Vertical"",
                ""TextLabels"": [""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H""],
                ""SelectedDeviceName"": ""BavarianSimTec Alpha""
            }";

            var settings = AppSettings.FromJson(legacyJson);

            // Assert
            Assert.Single(settings.Profiles);
            var profile = settings.Profiles[0];
            
            Assert.Equal("Default", profile.Name);
            Assert.Equal("BavarianSimTec Alpha", profile.DeviceName);
            Assert.Equal(DisplayLayout.Vertical, profile.Layout);
            Assert.Equal("A", profile.TextLabels[0]);
            
            Assert.Equal(profile.Id, settings.SelectedProfileId);
            Assert.Equal(profile.Id, settings.ActiveProfile.Id);
        }

        [Fact]
        public void NewProfile_ActiveProfileUpdates()
        {
            var settings = new AppSettings();
            // Default ctor migration
            Assert.Empty(settings.Profiles); 
            // NOTE: new AppSettings() doesn't run migration, Load/FromJson does. 
            // Let's mimic what FromJson does or just manually Add.
            
            var p1 = new Profile { Name = "P1" };
            settings.Profiles.Add(p1);
            settings.SelectedProfileId = p1.Id;

            Assert.Equal("P1", settings.ActiveProfile.Name);
        }
    }
}
