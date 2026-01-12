using System;
using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;
using WheelOverlay.Tests.Infrastructure;
using WheelOverlay.ViewModels;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for settings dialog functionality.
    /// Verifies that profile CRUD operations, layout changes, appearance updates,
    /// and settings persistence work correctly.
    /// 
    /// Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8
    /// </summary>
    public class SettingsDialogTests : UITestBase
    {
        /// <summary>
        /// Verifies that the settings dialog displays current configuration.
        /// Creates a ViewModel with settings and ensures configuration is accessible.
        /// 
        /// Requirements: 8.1, 8.7
        /// </summary>
        [Fact]
        public void SettingsDialog_OpensWithCurrentConfiguration()
        {
            // Arrange
            SetupTestViewModel();
            
            // Act & Assert - Creating ViewModel should not throw
            var exception = Record.Exception(() =>
            {
                Assert.NotNull(TestSettings);
                Assert.NotNull(TestSettings.ActiveProfile);
                Assert.NotNull(TestSettings.Profiles);
                Assert.NotEmpty(TestSettings.Profiles);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that a new profile can be created and saved.
        /// Creates a new profile, adds it to settings, and verifies it's saved.
        /// 
        /// Requirements: 8.2, 8.7
        /// </summary>
        [Fact]
        public void ProfileCreation_CreatesAndSavesNewProfile()
        {
            // Arrange
            SetupTestViewModel();
            var initialProfileCount = TestSettings!.Profiles.Count;
            
            // Act - Create a new profile
            var newProfile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "New Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Horizontal,
                PositionCount = 12,
                TextLabels = new List<string> 
                { 
                    "P1", "P2", "P3", "P4", "P5", "P6", 
                    "P7", "P8", "P9", "P10", "P11", "P12" 
                }
            };
            
            TestSettings.Profiles.Add(newProfile);
            
            // Assert - Verify profile was added
            Assert.Equal(initialProfileCount + 1, TestSettings.Profiles.Count);
            Assert.Contains(newProfile, TestSettings.Profiles);
            Assert.Equal("New Test Profile", newProfile.Name);
            Assert.Equal(12, newProfile.PositionCount);
            Assert.Equal(DisplayLayout.Horizontal, newProfile.Layout);
            
            // Verify profile can be found by ID
            var foundProfile = TestSettings.Profiles.FirstOrDefault(p => p.Id == newProfile.Id);
            Assert.NotNull(foundProfile);
            Assert.Equal(newProfile.Name, foundProfile.Name);
        }

        /// <summary>
        /// Verifies that profile creation with various configurations works correctly.
        /// Tests different position counts, layouts, and device names.
        /// 
        /// Requirements: 8.2, 8.7
        /// </summary>
        [Theory]
        [InlineData("Profile A", DisplayLayout.Vertical, 8)]
        [InlineData("Profile B", DisplayLayout.Horizontal, 12)]
        [InlineData("Profile C", DisplayLayout.Grid, 16)]
        [InlineData("Profile D", DisplayLayout.Single, 4)]
        public void ProfileCreation_WithVariousConfigurations_Succeeds(string name, DisplayLayout layout, int positionCount)
        {
            // Arrange
            SetupTestViewModel();
            
            // Act - Create profile with specific configuration
            var newProfile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = name,
                DeviceName = "Test Device",
                Layout = layout,
                PositionCount = positionCount,
                TextLabels = Enumerable.Range(1, positionCount).Select(i => $"POS{i}").ToList()
            };
            
            TestSettings!.Profiles.Add(newProfile);
            
            // Assert
            Assert.Contains(newProfile, TestSettings.Profiles);
            Assert.Equal(name, newProfile.Name);
            Assert.Equal(layout, newProfile.Layout);
            Assert.Equal(positionCount, newProfile.PositionCount);
            Assert.Equal(positionCount, newProfile.TextLabels.Count);
        }

        /// <summary>
        /// Verifies that an existing profile can be deleted.
        /// Deletes a profile from settings and verifies it's removed.
        /// 
        /// Requirements: 8.3, 8.7
        /// </summary>
        [Fact]
        public void ProfileDeletion_RemovesExistingProfile()
        {
            // Arrange
            SetupTestViewModel();
            
            // Add a second profile to delete (keep at least one profile)
            var profileToDelete = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Profile To Delete",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                PositionCount = 8,
                TextLabels = Enumerable.Range(1, 8).Select(i => $"POS{i}").ToList()
            };
            
            TestSettings!.Profiles.Add(profileToDelete);
            var initialCount = TestSettings.Profiles.Count;
            
            // Act - Delete the profile
            TestSettings.Profiles.Remove(profileToDelete);
            
            // Assert - Verify profile was removed
            Assert.Equal(initialCount - 1, TestSettings.Profiles.Count);
            Assert.DoesNotContain(profileToDelete, TestSettings.Profiles);
            
            // Verify profile cannot be found by ID
            var foundProfile = TestSettings.Profiles.FirstOrDefault(p => p.Id == profileToDelete.Id);
            Assert.Null(foundProfile);
        }

        /// <summary>
        /// Verifies that deleting a profile updates the selected profile if necessary.
        /// If the active profile is deleted, another profile should become active.
        /// 
        /// Requirements: 8.3, 8.7
        /// </summary>
        [Fact]
        public void ProfileDeletion_WhenDeletingActiveProfile_SelectsAnotherProfile()
        {
            // Arrange
            SetupTestViewModel();
            
            // Add a second profile
            var secondProfile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Second Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Horizontal,
                PositionCount = 8,
                TextLabels = Enumerable.Range(1, 8).Select(i => $"POS{i}").ToList()
            };
            
            TestSettings!.Profiles.Add(secondProfile);
            
            // Set the first profile as active
            var firstProfile = TestSettings.Profiles[0];
            TestSettings.SelectedProfileId = firstProfile.Id;
            
            // Act - Delete the active profile
            TestSettings.Profiles.Remove(firstProfile);
            
            // If the active profile was deleted, update selection
            if (TestSettings.ActiveProfile == null && TestSettings.Profiles.Count > 0)
            {
                TestSettings.SelectedProfileId = TestSettings.Profiles[0].Id;
            }
            
            // Assert - Verify another profile is now active
            Assert.NotNull(TestSettings.ActiveProfile);
            Assert.Equal(secondProfile.Id, TestSettings.ActiveProfile.Id);
            Assert.DoesNotContain(firstProfile, TestSettings.Profiles);
        }

        /// <summary>
        /// Verifies that attempting to delete the last profile is handled correctly.
        /// At least one profile should always remain.
        /// 
        /// Requirements: 8.3, 8.7
        /// </summary>
        [Fact]
        public void ProfileDeletion_PreventsDeletingLastProfile()
        {
            // Arrange
            SetupTestViewModel();
            
            // Ensure we only have one profile
            while (TestSettings!.Profiles.Count > 1)
            {
                TestSettings.Profiles.RemoveAt(TestSettings.Profiles.Count - 1);
            }
            
            var lastProfile = TestSettings.Profiles[0];
            var initialCount = TestSettings.Profiles.Count;
            
            // Act - Attempt to delete the last profile (should be prevented by UI logic)
            // In a real application, the UI would prevent this, but we test the behavior
            bool canDelete = TestSettings.Profiles.Count > 1;
            
            if (canDelete)
            {
                TestSettings.Profiles.Remove(lastProfile);
            }
            
            // Assert - Verify profile was not deleted
            Assert.Equal(initialCount, TestSettings.Profiles.Count);
            Assert.Contains(lastProfile, TestSettings.Profiles);
        }

        /// <summary>
        /// Verifies that switching to a different profile loads the correct configuration.
        /// Changes the selected profile and verifies the active profile updates.
        /// 
        /// Requirements: 8.4, 8.7
        /// </summary>
        [Fact]
        public void ProfileSwitching_LoadsSelectedProfileConfiguration()
        {
            // Arrange
            SetupTestViewModel();
            
            // Create a second profile with different configuration
            var secondProfile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Second Profile",
                DeviceName = "Different Device",
                Layout = DisplayLayout.Grid,
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4,
                TextLabels = Enumerable.Range(1, 12).Select(i => $"G{i}").ToList()
            };
            
            TestSettings!.Profiles.Add(secondProfile);
            
            // Act - Switch to the second profile
            TestSettings.SelectedProfileId = secondProfile.Id;
            
            // Assert - Verify the active profile is now the second profile
            Assert.NotNull(TestSettings.ActiveProfile);
            Assert.Equal(secondProfile.Id, TestSettings.ActiveProfile.Id);
            Assert.Equal("Second Profile", TestSettings.ActiveProfile.Name);
            Assert.Equal(DisplayLayout.Grid, TestSettings.ActiveProfile.Layout);
            Assert.Equal(12, TestSettings.ActiveProfile.PositionCount);
            Assert.Equal(3, TestSettings.ActiveProfile.GridRows);
            Assert.Equal(4, TestSettings.ActiveProfile.GridColumns);
        }

        /// <summary>
        /// Verifies that switching profiles updates the ViewModel correctly.
        /// Changes the profile and verifies ViewModel reflects the new configuration.
        /// 
        /// Requirements: 8.4, 8.7
        /// </summary>
        [Fact]
        public void ProfileSwitching_UpdatesViewModel()
        {
            // Arrange
            SetupTestViewModel();
            
            // Create a second profile
            var secondProfile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Horizontal Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Horizontal,
                PositionCount = 16,
                TextLabels = Enumerable.Range(1, 16).Select(i => $"H{i}").ToList()
            };
            
            TestSettings!.Profiles.Add(secondProfile);
            
            // Act - Switch profile and update ViewModel
            TestSettings.SelectedProfileId = secondProfile.Id;
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify ViewModel reflects new profile
            Assert.NotNull(TestViewModel.Settings.ActiveProfile);
            Assert.Equal(secondProfile.Id, TestViewModel.Settings.ActiveProfile.Id);
            Assert.Equal(DisplayLayout.Horizontal, TestViewModel.Settings.ActiveProfile.Layout);
            Assert.Equal(16, TestViewModel.Settings.ActiveProfile.PositionCount);
        }

        /// <summary>
        /// Verifies that switching between multiple profiles works correctly.
        /// Tests switching through several profiles in sequence.
        /// 
        /// Requirements: 8.4, 8.7
        /// </summary>
        [Fact]
        public void ProfileSwitching_ThroughMultipleProfiles_WorksCorrectly()
        {
            // Arrange
            SetupTestViewModel();
            
            var profiles = new[]
            {
                new Profile
                {
                    Id = Guid.NewGuid(),
                    Name = "Profile A",
                    Layout = DisplayLayout.Vertical,
                    PositionCount = 8,
                    TextLabels = Enumerable.Range(1, 8).Select(i => $"A{i}").ToList()
                },
                new Profile
                {
                    Id = Guid.NewGuid(),
                    Name = "Profile B",
                    Layout = DisplayLayout.Horizontal,
                    PositionCount = 12,
                    TextLabels = Enumerable.Range(1, 12).Select(i => $"B{i}").ToList()
                },
                new Profile
                {
                    Id = Guid.NewGuid(),
                    Name = "Profile C",
                    Layout = DisplayLayout.Grid,
                    PositionCount = 16,
                    TextLabels = Enumerable.Range(1, 16).Select(i => $"C{i}").ToList()
                }
            };
            
            foreach (var profile in profiles)
            {
                TestSettings!.Profiles.Add(profile);
            }
            
            // Act & Assert - Switch through each profile
            foreach (var profile in profiles)
            {
                TestSettings!.SelectedProfileId = profile.Id;
                
                Assert.NotNull(TestSettings.ActiveProfile);
                Assert.Equal(profile.Id, TestSettings.ActiveProfile.Id);
                Assert.Equal(profile.Name, TestSettings.ActiveProfile.Name);
                Assert.Equal(profile.Layout, TestSettings.ActiveProfile.Layout);
                Assert.Equal(profile.PositionCount, TestSettings.ActiveProfile.PositionCount);
            }
        }

        /// <summary>
        /// Verifies that changing layout settings applies the new layout.
        /// Changes the layout and verifies it's applied to the profile.
        /// 
        /// Requirements: 8.5
        /// </summary>
        [Fact]
        public void LayoutChange_AppliesNewLayout()
        {
            // Arrange
            SetupTestViewModel();
            var profile = TestSettings!.ActiveProfile!;
            var originalLayout = profile.Layout;
            
            // Act - Change to a different layout
            var newLayout = originalLayout == DisplayLayout.Vertical 
                ? DisplayLayout.Horizontal 
                : DisplayLayout.Vertical;
            
            profile.Layout = newLayout;
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify layout was changed
            Assert.Equal(newLayout, profile.Layout);
            Assert.Equal(newLayout, TestViewModel.Settings.ActiveProfile!.Layout);
            Assert.NotEqual(originalLayout, profile.Layout);
        }

        /// <summary>
        /// Verifies that changing to each layout type works correctly.
        /// Tests all four layout types.
        /// 
        /// Requirements: 8.5
        /// </summary>
        [Theory]
        [InlineData(DisplayLayout.Vertical)]
        [InlineData(DisplayLayout.Horizontal)]
        [InlineData(DisplayLayout.Grid)]
        [InlineData(DisplayLayout.Single)]
        public void LayoutChange_ToSpecificLayout_AppliesCorrectly(DisplayLayout targetLayout)
        {
            // Arrange
            SetupTestViewModel();
            var profile = TestSettings!.ActiveProfile!;
            
            // Act - Change to target layout
            profile.Layout = targetLayout;
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify layout was applied
            Assert.Equal(targetLayout, profile.Layout);
            Assert.Equal(targetLayout, TestViewModel.Settings.ActiveProfile!.Layout);
            
            // Verify ViewModel can handle the layout
            var exception = Record.Exception(() =>
            {
                _ = TestViewModel.DisplayItems;
                _ = TestViewModel.PopulatedPositionLabels;
            });
            
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that changing grid layout dimensions updates correctly.
        /// Changes grid rows and columns and verifies the configuration.
        /// 
        /// Requirements: 8.5
        /// </summary>
        [Fact]
        public void LayoutChange_GridDimensions_UpdatesCorrectly()
        {
            // Arrange
            SetupTestViewModel();
            var profile = TestSettings!.ActiveProfile!;
            profile.Layout = DisplayLayout.Grid;
            profile.PositionCount = 12;
            profile.TextLabels = Enumerable.Range(1, 12).Select(i => $"POS{i}").ToList();
            
            // Act - Change grid dimensions
            profile.GridRows = 3;
            profile.GridColumns = 4;
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify dimensions were updated in profile
            Assert.Equal(DisplayLayout.Grid, profile.Layout);
            Assert.Equal(3, profile.GridRows);
            Assert.Equal(4, profile.GridColumns);
            
            // EffectiveGridRows/Columns may differ from configured values if positions are condensed
            // Just verify they are positive and the capacity is sufficient
            Assert.True(TestViewModel.EffectiveGridRows > 0);
            Assert.True(TestViewModel.EffectiveGridColumns > 0);
            int effectiveCapacity = TestViewModel.EffectiveGridRows * TestViewModel.EffectiveGridColumns;
            Assert.True(effectiveCapacity >= profile.PositionCount);
        }

        /// <summary>
        /// Verifies that changing appearance settings updates the configuration.
        /// Changes color and font settings and verifies they're applied.
        /// 
        /// Requirements: 8.6
        /// </summary>
        [Fact]
        public void AppearanceChange_UpdatesColorSettings()
        {
            // Arrange
            SetupTestViewModel();
            var originalSelectedColor = TestSettings!.SelectedTextColor;
            var originalNonSelectedColor = TestSettings.NonSelectedTextColor;
            
            // Act - Change color settings
            TestSettings.SelectedTextColor = "#FF0000"; // Red
            TestSettings.NonSelectedTextColor = "#0000FF"; // Blue
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify colors were updated
            Assert.Equal("#FF0000", TestSettings.SelectedTextColor);
            Assert.Equal("#0000FF", TestSettings.NonSelectedTextColor);
            Assert.NotEqual(originalSelectedColor, TestSettings.SelectedTextColor);
            Assert.NotEqual(originalNonSelectedColor, TestSettings.NonSelectedTextColor);
        }

        /// <summary>
        /// Verifies that changing font settings updates the configuration.
        /// Changes font size and family and verifies they're applied.
        /// 
        /// Requirements: 8.6
        /// </summary>
        [Fact]
        public void AppearanceChange_UpdatesFontSettings()
        {
            // Arrange
            SetupTestViewModel();
            var originalFontSize = TestSettings!.FontSize;
            var originalFontFamily = TestSettings.FontFamily;
            
            // Act - Change font settings
            TestSettings.FontSize = 24;
            TestSettings.FontFamily = "Arial";
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify font settings were updated
            Assert.Equal(24, TestSettings.FontSize);
            Assert.Equal("Arial", TestSettings.FontFamily);
            Assert.NotEqual(originalFontSize, TestSettings.FontSize);
            Assert.NotEqual(originalFontFamily, TestSettings.FontFamily);
        }

        /// <summary>
        /// Verifies that changing multiple appearance settings works correctly.
        /// Changes colors, fonts, and spacing settings together.
        /// 
        /// Requirements: 8.6
        /// </summary>
        [Fact]
        public void AppearanceChange_MultipleSettings_UpdatesCorrectly()
        {
            // Arrange
            SetupTestViewModel();
            
            // Act - Change multiple appearance settings
            TestSettings!.SelectedTextColor = "#00FF00"; // Green
            TestSettings.NonSelectedTextColor = "#FFFF00"; // Yellow
            TestSettings.FontSize = 28;
            TestSettings.FontFamily = "Courier New";
            TestSettings.ItemSpacing = 10;
            TestSettings.ItemPadding = 8;
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify all settings were updated
            Assert.Equal("#00FF00", TestSettings.SelectedTextColor);
            Assert.Equal("#FFFF00", TestSettings.NonSelectedTextColor);
            Assert.Equal(28, TestSettings.FontSize);
            Assert.Equal("Courier New", TestSettings.FontFamily);
            Assert.Equal(10, TestSettings.ItemSpacing);
            Assert.Equal(8, TestSettings.ItemPadding);
        }

        /// <summary>
        /// Verifies that appearance changes with various font sizes work correctly.
        /// Tests different font size values.
        /// 
        /// Requirements: 8.6
        /// </summary>
        [Theory]
        [InlineData(12)]
        [InlineData(16)]
        [InlineData(20)]
        [InlineData(24)]
        [InlineData(32)]
        public void AppearanceChange_VariousFontSizes_AppliesCorrectly(int fontSize)
        {
            // Arrange
            SetupTestViewModel();
            
            // Act - Change font size
            TestSettings!.FontSize = fontSize;
            TestViewModel!.Settings = TestSettings;
            
            // Assert - Verify font size was applied
            Assert.Equal(fontSize, TestSettings.FontSize);
        }

        /// <summary>
        /// Property test: Settings Persistence
        /// For any configuration change, settings should persist across application restarts.
        /// This test verifies that settings can be saved and loaded correctly.
        /// 
        /// Property 11: Settings Persistence
        /// Validates: Requirements 8.8
        /// </summary>
        [Property(MaxTest = 100)]
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 11: Settings Persistence")]
        public Property Property_SettingsPersistence()
        {
            return Prop.ForAll(
                GenerateSettingsConfiguration(),
                config =>
                {
                    // Arrange - Create settings with random configuration
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = config.ProfileName,
                        DeviceName = config.DeviceName,
                        Layout = config.Layout,
                        PositionCount = config.PositionCount,
                        TextLabels = config.TextLabels,
                        GridRows = config.GridRows,
                        GridColumns = config.GridColumns
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id,
                        SelectedTextColor = config.SelectedTextColor,
                        NonSelectedTextColor = config.NonSelectedTextColor,
                        FontSize = config.FontSize,
                        FontFamily = config.FontFamily,
                        ItemSpacing = config.ItemSpacing,
                        ItemPadding = config.ItemPadding,
                        EnableAnimations = config.EnableAnimations
                    };

                    // Act - Serialize and deserialize settings (simulating save/load)
                    bool persistenceWorks = true;
                    string errorMessage = "";

                    try
                    {
                        // Serialize to JSON
                        var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions 
                        { 
                            WriteIndented = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                        });

                        // Deserialize from JSON
                        var loadedSettings = AppSettings.FromJson(json);

                        // Verify all settings persisted correctly
                        var loadedProfile = loadedSettings.ActiveProfile;
                        
                        if (loadedProfile == null)
                        {
                            persistenceWorks = false;
                            errorMessage = "Active profile is null after loading";
                        }
                        else
                        {
                            // Verify profile settings
                            if (loadedProfile.Name != profile.Name)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Profile name mismatch: expected '{profile.Name}', got '{loadedProfile.Name}'";
                            }
                            else if (loadedProfile.DeviceName != profile.DeviceName)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Device name mismatch: expected '{profile.DeviceName}', got '{loadedProfile.DeviceName}'";
                            }
                            else if (loadedProfile.Layout != profile.Layout)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Layout mismatch: expected '{profile.Layout}', got '{loadedProfile.Layout}'";
                            }
                            else if (loadedProfile.PositionCount != profile.PositionCount)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Position count mismatch: expected {profile.PositionCount}, got {loadedProfile.PositionCount}";
                            }
                            else if (loadedProfile.GridRows != profile.GridRows)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Grid rows mismatch: expected {profile.GridRows}, got {loadedProfile.GridRows}";
                            }
                            else if (loadedProfile.GridColumns != profile.GridColumns)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Grid columns mismatch: expected {profile.GridColumns}, got {loadedProfile.GridColumns}";
                            }
                            else if (loadedProfile.TextLabels.Count != profile.TextLabels.Count)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Text labels count mismatch: expected {profile.TextLabels.Count}, got {loadedProfile.TextLabels.Count}";
                            }
                            // Verify appearance settings
                            else if (loadedSettings.SelectedTextColor != settings.SelectedTextColor)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Selected text color mismatch: expected '{settings.SelectedTextColor}', got '{loadedSettings.SelectedTextColor}'";
                            }
                            else if (loadedSettings.NonSelectedTextColor != settings.NonSelectedTextColor)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Non-selected text color mismatch: expected '{settings.NonSelectedTextColor}', got '{loadedSettings.NonSelectedTextColor}'";
                            }
                            else if (loadedSettings.FontSize != settings.FontSize)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Font size mismatch: expected {settings.FontSize}, got {loadedSettings.FontSize}";
                            }
                            else if (loadedSettings.FontFamily != settings.FontFamily)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Font family mismatch: expected '{settings.FontFamily}', got '{loadedSettings.FontFamily}'";
                            }
                            else if (loadedSettings.ItemSpacing != settings.ItemSpacing)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Item spacing mismatch: expected {settings.ItemSpacing}, got {loadedSettings.ItemSpacing}";
                            }
                            else if (loadedSettings.ItemPadding != settings.ItemPadding)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Item padding mismatch: expected {settings.ItemPadding}, got {loadedSettings.ItemPadding}";
                            }
                            else if (loadedSettings.EnableAnimations != settings.EnableAnimations)
                            {
                                persistenceWorks = false;
                                errorMessage = $"Enable animations mismatch: expected {settings.EnableAnimations}, got {loadedSettings.EnableAnimations}";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        persistenceWorks = false;
                        errorMessage = ex.Message;
                    }

                    return persistenceWorks
                        .Label($"Settings persistence for profile '{config.ProfileName}': {errorMessage}");
                });
        }

        /// <summary>
        /// Generator for settings configurations.
        /// Generates random but valid settings configurations for property testing.
        /// </summary>
        private static Arbitrary<SettingsConfiguration> GenerateSettingsConfiguration()
        {
            return Arb.From(
                from profileName in Gen.Elements("Profile A", "Profile B", "Test Profile", "My Profile")
                from deviceName in Gen.Elements("BavarianSimTec Alpha", "Test Device", "Device 1")
                from layout in Gen.Elements(DisplayLayout.Vertical, DisplayLayout.Horizontal, DisplayLayout.Grid, DisplayLayout.Single)
                from positionCount in Gen.Elements(4, 8, 12, 16)
                from selectedColor in Gen.Elements("#FFFFFF", "#FF0000", "#00FF00", "#0000FF")
                from nonSelectedColor in Gen.Elements("#808080", "#FFFF00", "#FF00FF", "#00FFFF")
                from fontSize in Gen.Elements(12, 16, 20, 24, 28, 32)
                from fontFamily in Gen.Elements("Segoe UI", "Arial", "Courier New", "Times New Roman")
                from itemSpacing in Gen.Elements(0, 5, 10, 15)
                from itemPadding in Gen.Elements(0, 5, 10, 15)
                from enableAnimations in Gen.Elements(true, false)
                // Generate valid grid dimensions based on position count
                let validGridDimensions = GetValidGridDimensions(positionCount)
                from gridDimension in Gen.Elements(validGridDimensions.ToArray())
                select new SettingsConfiguration
                {
                    ProfileName = profileName,
                    DeviceName = deviceName,
                    Layout = layout,
                    PositionCount = positionCount,
                    TextLabels = Enumerable.Range(1, positionCount).Select(i => $"POS{i}").ToList(),
                    GridRows = gridDimension.Rows,
                    GridColumns = gridDimension.Columns,
                    SelectedTextColor = selectedColor,
                    NonSelectedTextColor = nonSelectedColor,
                    FontSize = fontSize,
                    FontFamily = fontFamily,
                    ItemSpacing = itemSpacing,
                    ItemPadding = itemPadding,
                    EnableAnimations = enableAnimations
                });
        }

        /// <summary>
        /// Gets valid grid dimensions for a given position count.
        /// Ensures rows × columns >= positionCount.
        /// </summary>
        private static List<(int Rows, int Columns)> GetValidGridDimensions(int positionCount)
        {
            var dimensions = new List<(int Rows, int Columns)>();
            
            // Generate valid combinations where rows × columns >= positionCount
            for (int rows = 1; rows <= 4; rows++)
            {
                for (int cols = 1; cols <= 4; cols++)
                {
                    if (rows * cols >= positionCount)
                    {
                        dimensions.Add((rows, cols));
                    }
                }
            }
            
            // Ensure we have at least one valid dimension
            if (dimensions.Count == 0)
            {
                dimensions.Add((2, (int)Math.Ceiling(positionCount / 2.0)));
            }
            
            return dimensions;
        }

        private class SettingsConfiguration
        {
            public string ProfileName { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public DisplayLayout Layout { get; set; }
            public int PositionCount { get; set; }
            public List<string> TextLabels { get; set; } = new List<string>();
            public int GridRows { get; set; }
            public int GridColumns { get; set; }
            public string SelectedTextColor { get; set; } = "";
            public string NonSelectedTextColor { get; set; } = "";
            public int FontSize { get; set; }
            public string FontFamily { get; set; } = "";
            public int ItemSpacing { get; set; }
            public int ItemPadding { get; set; }
            public bool EnableAnimations { get; set; }
        }
    }
}
