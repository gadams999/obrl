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
            Assert.Equal(DisplayLayout.Grid, settings.Layout);
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
            Assert.NotNull(settings.ActiveProfile);
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

            Assert.NotNull(settings.ActiveProfile);
            Assert.Equal("P1", settings.ActiveProfile.Name);
        }

        [Fact]
        public void FirstRun_CreatesDefaultProfileWithTextLabels()
        {
            // This test simulates first run behavior
            // Note: We can't easily test AppSettings.Load() without file system access
            // but we can verify the logic by creating settings and checking defaults
            
            var settings = new AppSettings();
            
            // Simulate what Load() does on first run
            var defaultProfile = new Profile
            {
                Name = "Default",
                DeviceName = settings.SelectedDeviceName,
                Layout = settings.Layout,
                TextLabels = new System.Collections.Generic.List<string>(settings.TextLabels)
            };
            settings.Profiles.Add(defaultProfile);
            settings.SelectedProfileId = defaultProfile.Id;

            // Assert
            Assert.Single(settings.Profiles);
            Assert.NotNull(settings.ActiveProfile);
            Assert.Equal("Default", settings.ActiveProfile.Name);
            Assert.Equal(8, settings.ActiveProfile.TextLabels.Count);
            Assert.Equal("DASH", settings.ActiveProfile.TextLabels[0]);
            Assert.Equal("TC2", settings.ActiveProfile.TextLabels[1]);
            Assert.Equal("MAP", settings.ActiveProfile.TextLabels[2]);
            Assert.Equal("FUEL", settings.ActiveProfile.TextLabels[3]);
            Assert.Equal("BRGT", settings.ActiveProfile.TextLabels[4]);
            Assert.Equal("VOL", settings.ActiveProfile.TextLabels[5]);
            Assert.Equal("BOX", settings.ActiveProfile.TextLabels[6]);
            Assert.Equal("DIFF", settings.ActiveProfile.TextLabels[7]);
        }

        [Fact]
        public void ProfileWithMissingV050Fields_LoadsWithDefaults()
        {
            // Arrange - JSON without v0.5.0 fields (PositionCount, GridRows, GridColumns)
            var jsonWithoutV050Fields = @"{
                ""Profiles"": [
                    {
                        ""Id"": ""12345678-1234-1234-1234-123456789012"",
                        ""Name"": ""TestProfile"",
                        ""DeviceName"": ""BavarianSimTec Alpha"",
                        ""Layout"": ""Grid"",
                        ""TextLabels"": [""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H""]
                    }
                ],
                ""SelectedProfileId"": ""12345678-1234-1234-1234-123456789012""
            }";

            // Act
            var settings = AppSettings.FromJson(jsonWithoutV050Fields);
            var profile = settings.Profiles[0];

            // Assert - Should have default v0.5.0 values
            Assert.Equal(8, profile.PositionCount);
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(4, profile.GridColumns);
            Assert.Equal("TestProfile", profile.Name);
            Assert.Equal(8, profile.TextLabels.Count);
        }

        [Fact]
        public void ProfileWithInvalidGridDimensions_IsAutoCorrectedOnLoad()
        {
            // Arrange - Profile with grid that's too small for position count
            var jsonWithInvalidGrid = @"{
                ""Profiles"": [
                    {
                        ""Id"": ""12345678-1234-1234-1234-123456789012"",
                        ""Name"": ""TestProfile"",
                        ""DeviceName"": ""BavarianSimTec Alpha"",
                        ""Layout"": ""Grid"",
                        ""TextLabels"": [""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H"", ""I"", ""J""],
                        ""PositionCount"": 10,
                        ""GridRows"": 2,
                        ""GridColumns"": 3
                    }
                ],
                ""SelectedProfileId"": ""12345678-1234-1234-1234-123456789012""
            }";

            // Act
            var settings = AppSettings.FromJson(jsonWithInvalidGrid);
            var profile = settings.Profiles[0];

            // Assert - Grid should be auto-corrected to 2×5 (default for 10 positions)
            Assert.Equal(10, profile.PositionCount);
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(5, profile.GridColumns);
            Assert.True(profile.IsValidGridConfiguration());
        }

        [Fact]
        public void ProfileWithFewerTextLabelsThanPositionCount_IsNormalizedOnLoad()
        {
            // Arrange - Profile with only 5 text labels but PositionCount of 8
            var jsonWithFewerLabels = @"{
                ""Profiles"": [
                    {
                        ""Id"": ""12345678-1234-1234-1234-123456789012"",
                        ""Name"": ""TestProfile"",
                        ""DeviceName"": ""BavarianSimTec Alpha"",
                        ""Layout"": ""Single"",
                        ""TextLabels"": [""A"", ""B"", ""C"", ""D"", ""E""],
                        ""PositionCount"": 8,
                        ""GridRows"": 2,
                        ""GridColumns"": 4
                    }
                ],
                ""SelectedProfileId"": ""12345678-1234-1234-1234-123456789012""
            }";

            // Act
            var settings = AppSettings.FromJson(jsonWithFewerLabels);
            var profile = settings.Profiles[0];

            // Assert - Should have 8 labels (5 original + 3 empty)
            Assert.Equal(8, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
            Assert.Equal("C", profile.TextLabels[2]);
            Assert.Equal("D", profile.TextLabels[3]);
            Assert.Equal("E", profile.TextLabels[4]);
            Assert.Equal("", profile.TextLabels[5]);
            Assert.Equal("", profile.TextLabels[6]);
            Assert.Equal("", profile.TextLabels[7]);
        }

        [Fact]
        public void ProfileWithMoreTextLabelsThanPositionCount_IsTruncatedOnLoad()
        {
            // Arrange - Profile with 10 text labels but PositionCount of 6
            var jsonWithExtraLabels = @"{
                ""Profiles"": [
                    {
                        ""Id"": ""12345678-1234-1234-1234-123456789012"",
                        ""Name"": ""TestProfile"",
                        ""DeviceName"": ""BavarianSimTec Alpha"",
                        ""Layout"": ""Single"",
                        ""TextLabels"": [""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H"", ""I"", ""J""],
                        ""PositionCount"": 6,
                        ""GridRows"": 2,
                        ""GridColumns"": 3
                    }
                ],
                ""SelectedProfileId"": ""12345678-1234-1234-1234-123456789012""
            }";

            // Act
            var settings = AppSettings.FromJson(jsonWithExtraLabels);
            var profile = settings.Profiles[0];

            // Assert - Should have only 6 labels (truncated)
            Assert.Equal(6, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
            Assert.Equal("C", profile.TextLabels[2]);
            Assert.Equal("D", profile.TextLabels[3]);
            Assert.Equal("E", profile.TextLabels[4]);
            Assert.Equal("F", profile.TextLabels[5]);
        }
    }
}
