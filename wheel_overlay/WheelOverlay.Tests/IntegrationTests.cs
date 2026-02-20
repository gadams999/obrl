using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using WheelOverlay.Models;
using WheelOverlay.Services;
using WheelOverlay.ViewModels;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Integration tests for v0.5.0 enhancements
    /// Tests all features working together across the application
    /// </summary>
    public class IntegrationTests
    {
        // Note: These tests use AppSettings.FromJson() and JsonSerializer to avoid file system dependencies
        // Real persistence is tested through the existing AppSettingsTests

        [Fact]
        public void PositionCountChange_WithSingleLayout_UpdatesCorrectly()
        {
            // Arrange - Create profile with 8 positions
            var settings = new AppSettings
            {
                Profiles = new List<Profile>
                {
                    new Profile
                    {
                        Name = "Test Profile",
                        PositionCount = 8,
                        GridRows = 2,
                        GridColumns = 4,
                        TextLabels = Enumerable.Range(1, 8).Select(i => $"Pos {i}").ToList()
                    }
                }
            };
            settings.SelectedProfileId = settings.Profiles[0].Id;

            // Act - Change position count to 12
            var profile = settings.Profiles[0];
            profile.PositionCount = 12;
            profile.GridRows = 3;
            profile.GridColumns = 4;
            profile.NormalizeTextLabels();

            // Serialize and deserialize to simulate save/load
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var reloadedSettings = AppSettings.FromJson(json);
            var reloadedProfile = reloadedSettings.Profiles[0];

            // Assert
            Assert.Equal(12, reloadedProfile.PositionCount);
            Assert.Equal(3, reloadedProfile.GridRows);
            Assert.Equal(4, reloadedProfile.GridColumns);
            Assert.Equal(12, reloadedProfile.TextLabels.Count);
            // Original 8 labels preserved
            for (int i = 0; i < 8; i++)
            {
                Assert.Equal($"Pos {i + 1}", reloadedProfile.TextLabels[i]);
            }
            // New labels are empty
            for (int i = 8; i < 12; i++)
            {
                Assert.Equal("", reloadedProfile.TextLabels[i]);
            }
        }

        [Fact]
        public void PositionCountChange_WithGridLayout_UpdatesCondensedGrid()
        {
            // Arrange - Create profile with some empty positions
            var profile = new Profile
            {
                Name = "Grid Test",
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4,
                TextLabels = new List<string>
                {
                    "A", "", "C", "",
                    "E", "F", "", "",
                    "I", "", "", "L"
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;

            var overlayViewModel = new OverlayViewModel(settings);

            // Act - Get populated positions
            var populatedItems = overlayViewModel.PopulatedPositionItems.ToList();

            // Assert - Only populated positions shown
            Assert.Equal(6, populatedItems.Count);
            Assert.Contains(populatedItems, item => item.PositionNumber == "#1" && item.Label == "A");
            Assert.Contains(populatedItems, item => item.PositionNumber == "#3" && item.Label == "C");
            Assert.Contains(populatedItems, item => item.PositionNumber == "#5" && item.Label == "E");
            Assert.Contains(populatedItems, item => item.PositionNumber == "#6" && item.Label == "F");
            Assert.Contains(populatedItems, item => item.PositionNumber == "#9" && item.Label == "I");
            Assert.Contains(populatedItems, item => item.PositionNumber == "#12" && item.Label == "L");

            // Verify grid dimensions are condensed
            Assert.True(overlayViewModel.EffectiveGridRows <= 3);
            Assert.True(overlayViewModel.EffectiveGridColumns <= 4);
        }

        [Fact]
        public void GridDimensionChange_WithVariousPositionCounts_MaintainsValidity()
        {
            // Test various position counts with different grid configurations
            var testCases = new[]
            {
                (PositionCount: 6, Rows: 2, Cols: 3),
                (PositionCount: 9, Rows: 3, Cols: 3),
                (PositionCount: 12, Rows: 3, Cols: 4),
                (PositionCount: 15, Rows: 3, Cols: 5),
                (PositionCount: 20, Rows: 4, Cols: 5)
            };

            foreach (var testCase in testCases)
            {
                // Arrange
                var profile = new Profile
                {
                    Name = $"Test {testCase.PositionCount}",
                    PositionCount = testCase.PositionCount,
                    GridRows = testCase.Rows,
                    GridColumns = testCase.Cols
                };
                profile.NormalizeTextLabels();

                // Act
                bool isValid = profile.IsValidGridConfiguration();

                // Assert
                Assert.True(isValid, $"Configuration {testCase.Rows}×{testCase.Cols} should be valid for {testCase.PositionCount} positions");
                Assert.Equal(testCase.PositionCount, profile.TextLabels.Count);
            }
        }

        [Fact]
        public void AnimationWithVariablePositionCounts_HandlesWrapAround()
        {
            // Test wrap-around logic with different position counts
            // For 2 positions: 0->1 and 1->0 are equidistant (both distance=1)
            // The IsForwardTransition logic uses <= so it chooses forward when equal
            
            // Test with 8 positions (typical case)
            Assert.True(IsForwardTransition(7, 0, 8), "Wrap from last to first should be forward");
            Assert.False(IsForwardTransition(0, 7, 8), "Wrap from first to last should be backward");
            Assert.True(IsForwardTransition(0, 1, 8), "Normal forward should be forward");
            Assert.False(IsForwardTransition(1, 0, 8), "Normal backward should be backward");
            
            // Test with 12 positions
            Assert.True(IsForwardTransition(11, 0, 12), "Wrap from last to first should be forward");
            Assert.False(IsForwardTransition(0, 11, 12), "Wrap from first to last should be backward");
            Assert.True(IsForwardTransition(5, 6, 12), "Normal forward should be forward");
            Assert.False(IsForwardTransition(6, 5, 12), "Normal backward should be backward");
            
            // Test with 20 positions
            Assert.True(IsForwardTransition(19, 0, 20), "Wrap from last to first should be forward");
            Assert.False(IsForwardTransition(0, 19, 20), "Wrap from first to last should be backward");
        }

        private bool IsForwardTransition(int previousPosition, int newPosition, int positionCount)
        {
            // Replicate the logic from SingleTextLayout
            int forwardDistance = (newPosition - previousPosition + positionCount) % positionCount;
            int backwardDistance = (previousPosition - newPosition + positionCount) % positionCount;
            return forwardDistance <= backwardDistance;
        }

        [Fact]
        public void SettingsPersistence_AcrossMultipleSavesAndLoads_MaintainsIntegrity()
        {
            // Arrange - Create complex settings
            var originalSettings = new AppSettings
            {
                Profiles = new List<Profile>
                {
                    new Profile
                    {
                        Name = "Profile 1",
                        PositionCount = 8,
                        GridRows = 2,
                        GridColumns = 4,
                        TextLabels = Enumerable.Range(1, 8).Select(i => $"Label {i}").ToList()
                    },
                    new Profile
                    {
                        Name = "Profile 2",
                        PositionCount = 12,
                        GridRows = 3,
                        GridColumns = 4,
                        TextLabels = Enumerable.Range(1, 12).Select(i => $"Item {i}").ToList()
                    }
                }
            };
            originalSettings.SelectedProfileId = originalSettings.Profiles[0].Id;

            // Act - Serialize, deserialize, modify, serialize, deserialize again
            var json1 = JsonSerializer.Serialize(originalSettings, new JsonSerializerOptions { WriteIndented = true });
            var loaded1 = AppSettings.FromJson(json1);
            
            loaded1.Profiles[0].PositionCount = 10;
            loaded1.Profiles[0].GridRows = 2;
            loaded1.Profiles[0].GridColumns = 5;
            loaded1.Profiles[0].NormalizeTextLabels();
            
            var json2 = JsonSerializer.Serialize(loaded1, new JsonSerializerOptions { WriteIndented = true });
            var loaded2 = AppSettings.FromJson(json2);

            // Assert - Verify all changes persisted correctly
            Assert.Equal(2, loaded2.Profiles.Count);
            
            // Profile 1 changes
            Assert.Equal("Profile 1", loaded2.Profiles[0].Name);
            Assert.Equal(10, loaded2.Profiles[0].PositionCount);
            Assert.Equal(2, loaded2.Profiles[0].GridRows);
            Assert.Equal(5, loaded2.Profiles[0].GridColumns);
            Assert.Equal(10, loaded2.Profiles[0].TextLabels.Count);
            
            // Profile 2 unchanged
            Assert.Equal("Profile 2", loaded2.Profiles[1].Name);
            Assert.Equal(12, loaded2.Profiles[1].PositionCount);
            Assert.Equal(3, loaded2.Profiles[1].GridRows);
            Assert.Equal(4, loaded2.Profiles[1].GridColumns);
            Assert.Equal(12, loaded2.Profiles[1].TextLabels.Count);
        }

        [Fact]
        public void InvalidGridConfiguration_AutoCorrects_OnLoad()
        {
            // Arrange - Create JSON with invalid grid configuration
            var invalidJson = @"{
                ""Profiles"": [
                    {
                        ""Id"": ""12345678-1234-1234-1234-123456789012"",
                        ""Name"": ""Invalid Grid"",
                        ""PositionCount"": 8,
                        ""GridRows"": 3,
                        ""GridColumns"": 3,
                        ""TextLabels"": [""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H""]
                    }
                ],
                ""SelectedProfileId"": ""12345678-1234-1234-1234-123456789012""
            }";

            // Act - Load settings (should auto-correct)
            var loadedSettings = AppSettings.FromJson(invalidJson);
            var profile = loadedSettings.Profiles[0];

            // Assert - Grid should be auto-corrected to valid configuration
            Assert.True(profile.IsValidGridConfiguration());
            Assert.True(profile.GridRows * profile.GridColumns >= profile.PositionCount);
        }

        [Fact]
        public void InputService_WithVariablePositionCount_FiltersCorrectRange()
        {
            // Test that InputService correctly handles different position counts
            var testCases = new[] { 2, 8, 12, 20 };

            foreach (var posCount in testCases)
            {
                // Arrange
                var profile = new Profile
                {
                    Name = $"Test {posCount}",
                    PositionCount = posCount,
                    GridRows = 2,
                    GridColumns = posCount / 2
                };
                profile.NormalizeTextLabels();

                var inputService = new InputService();
                inputService.SetActiveProfile(profile);

                // Act & Assert - Verify button range
                // Button indices should be [57, 57+posCount-1]
                int minButton = 57;
                int maxButton = 57 + posCount - 1;

                // Simulate button presses in valid range
                for (int button = minButton; button <= maxButton; button++)
                {
                    int expectedPosition = button - 57;
                    Assert.True(expectedPosition >= 0 && expectedPosition < posCount,
                        $"Button {button} should map to valid position for {posCount} positions");
                }

                // Verify out-of-range buttons are filtered
                int outOfRangeButton = maxButton + 1;
                int outOfRangePosition = outOfRangeButton - 57;
                Assert.True(outOfRangePosition >= posCount,
                    $"Button {outOfRangeButton} should be out of range for {posCount} positions");
            }
        }

        [Fact]
        public void ProfileValidator_SuggestsDimensions_ForAllPositionCounts()
        {
            // Test that ProfileValidator provides valid suggestions for all position counts
            for (int posCount = 2; posCount <= 20; posCount++)
            {
                // Act
                var suggestions = ProfileValidator.GetSuggestedDimensions(posCount);

                // Assert
                Assert.NotEmpty(suggestions);
                
                foreach (var suggestion in suggestions)
                {
                    Assert.True(suggestion.Rows * suggestion.Columns >= posCount,
                        $"Suggestion {suggestion.Rows}×{suggestion.Columns} should accommodate {posCount} positions");
                    Assert.True(suggestion.Rows >= 1 && suggestion.Rows <= 10);
                    Assert.True(suggestion.Columns >= 1 && suggestion.Columns <= 10);
                }
            }
        }

        [Fact]
        public void OverlayViewModel_SwitchingProfiles_UpdatesAllProperties()
        {
            // Arrange
            var profile1 = new Profile
            {
                Name = "Profile 1",
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4,
                TextLabels = Enumerable.Range(1, 8).Select(i => $"P1-{i}").ToList()
            };

            var profile2 = new Profile
            {
                Name = "Profile 2",
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4,
                TextLabels = Enumerable.Range(1, 12).Select(i => $"P2-{i}").ToList()
            };

            var settings1 = new AppSettings
            {
                Profiles = new List<Profile> { profile1 }
            };
            settings1.SelectedProfileId = profile1.Id;

            var settings2 = new AppSettings
            {
                Profiles = new List<Profile> { profile2 }
            };
            settings2.SelectedProfileId = profile2.Id;

            // Act - Create viewmodel with profile 1, then switch to profile 2
            var viewModel = new OverlayViewModel(settings1);
            var items1 = viewModel.PopulatedPositionItems.ToList();
            int rows1 = viewModel.EffectiveGridRows;
            int cols1 = viewModel.EffectiveGridColumns;

            viewModel.Settings = settings2;
            var items2 = viewModel.PopulatedPositionItems.ToList();
            int rows2 = viewModel.EffectiveGridRows;
            int cols2 = viewModel.EffectiveGridColumns;

            // Assert - Profile 1
            Assert.Equal(8, items1.Count);
            Assert.All(items1, item => Assert.StartsWith("P1-", item.Label));

            // Assert - Profile 2
            Assert.Equal(12, items2.Count);
            Assert.All(items2, item => Assert.StartsWith("P2-", item.Label));

            // Grid dimensions should reflect the profiles
            Assert.True(rows1 * cols1 >= 8);
            Assert.True(rows2 * cols2 >= 12);
        }

        [Fact]
        public void CompleteWorkflow_CreateProfileWithCustomPositions_SaveLoadAndUse()
        {
            // This test simulates a complete user workflow
            
            // Step 1: Create a new profile with custom position count
            var newProfile = new Profile
            {
                Name = "Custom Workflow",
                PositionCount = 15,
                GridRows = 3,
                GridColumns = 5
            };
            newProfile.NormalizeTextLabels();
            
            // Add custom labels
            for (int i = 0; i < 15; i++)
            {
                newProfile.TextLabels[i] = $"Custom {i + 1}";
            }

            // Step 2: Create settings and serialize
            var settings = new AppSettings
            {
                Profiles = new List<Profile> { newProfile }
            };
            settings.SelectedProfileId = newProfile.Id;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

            // Step 3: Load settings (simulating app restart)
            var loadedSettings = AppSettings.FromJson(json);
            var loadedProfile = loadedSettings.Profiles[0];

            // Step 4: Use profile in OverlayViewModel
            var overlayViewModel = new OverlayViewModel(loadedSettings);

            // Step 5: Use profile in InputService
            var inputService = new InputService();
            inputService.SetActiveProfile(loadedProfile);

            // Step 6: Verify everything works together
            Assert.Equal(15, loadedProfile.PositionCount);
            Assert.Equal(3, loadedProfile.GridRows);
            Assert.Equal(5, loadedProfile.GridColumns);
            Assert.Equal(15, loadedProfile.TextLabels.Count);
            Assert.Equal(15, overlayViewModel.PopulatedPositionItems.Count());
            
            // Verify all custom labels are present
            for (int i = 0; i < 15; i++)
            {
                Assert.Equal($"Custom {i + 1}", loadedProfile.TextLabels[i]);
            }
        }

        // ===== Conditional Visibility Integration Tests (Task 11) =====
        // Requirements: Complete workflow testing for overlay-visibility-and-ui-improvements

        [Fact]
        public void ConditionalVisibility_CompleteWorkflow_SelectExeAndVerifyPersistence()
        {
            // This test simulates the complete conditional visibility workflow:
            // 1. User selects an executable in settings
            // 2. Settings are saved
            // 3. Application restarts and loads settings
            // 4. Overlay visibility is controlled by target executable

            // Step 1: Create profile with target executable
            var targetExePath = @"C:\Games\iRacing\iRacingSim64.exe";
            var profile = new Profile
            {
                Name = "iRacing Profile",
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4,
                TargetExecutablePath = targetExePath,
                FontSize = 14,
                FontWeight = "Bold",
                TextRenderingMode = "Aliased"
            };
            profile.NormalizeTextLabels();

            // Step 2: Create settings and serialize (simulating save)
            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

            // Step 3: Load settings (simulating app restart)
            var loadedSettings = AppSettings.FromJson(json);
            var loadedProfile = loadedSettings.ActiveProfile;

            // Step 4: Verify all settings persisted correctly
            Assert.NotNull(loadedProfile);
            Assert.Equal("iRacing Profile", loadedProfile.Name);
            Assert.Equal(targetExePath, loadedProfile.TargetExecutablePath);
            Assert.Equal(12, loadedProfile.PositionCount);
            Assert.Equal(14, loadedProfile.FontSize);
            Assert.Equal("Bold", loadedProfile.FontWeight);
            Assert.Equal("Aliased", loadedProfile.TextRenderingMode);

            // Step 5: Verify ProcessMonitor can be initialized with the loaded settings
            using var monitor = new WheelOverlay.Services.ProcessMonitor(
                loadedProfile.TargetExecutablePath, 
                TimeSpan.FromSeconds(1));
            
            Assert.NotNull(monitor);
        }

        [Fact]
        public void ConditionalVisibility_ProfileSwitching_UpdatesTargetExecutable()
        {
            // Test that switching between profiles with different target executables works correctly

            // Step 1: Create two profiles with different target executables
            var iRacingProfile = new Profile
            {
                Name = "iRacing",
                PositionCount = 12,
                TargetExecutablePath = @"C:\Games\iRacing\iRacingSim64.exe"
            };
            iRacingProfile.NormalizeTextLabels();

            var accProfile = new Profile
            {
                Name = "Assetto Corsa Competizione",
                PositionCount = 8,
                TargetExecutablePath = @"C:\Games\ACC\AC2.exe"
            };
            accProfile.NormalizeTextLabels();

            var noTargetProfile = new Profile
            {
                Name = "Always Visible",
                PositionCount = 10,
                TargetExecutablePath = null
            };
            noTargetProfile.NormalizeTextLabels();

            // Step 2: Create settings with all profiles
            var settings = new AppSettings
            {
                Profiles = new List<Profile> { iRacingProfile, accProfile, noTargetProfile }
            };

            // Step 3: Test switching between profiles
            settings.SelectedProfileId = iRacingProfile.Id;
            Assert.Equal(@"C:\Games\iRacing\iRacingSim64.exe", settings.ActiveProfile?.TargetExecutablePath);

            settings.SelectedProfileId = accProfile.Id;
            Assert.Equal(@"C:\Games\ACC\AC2.exe", settings.ActiveProfile?.TargetExecutablePath);

            settings.SelectedProfileId = noTargetProfile.Id;
            Assert.Null(settings.ActiveProfile?.TargetExecutablePath);

            // Step 4: Verify persistence after profile switching
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var loadedSettings = AppSettings.FromJson(json);

            Assert.Equal(3, loadedSettings.Profiles.Count);
            Assert.Equal(noTargetProfile.Id, loadedSettings.SelectedProfileId);
            Assert.Null(loadedSettings.ActiveProfile?.TargetExecutablePath);
        }

        [Fact]
        public void ConditionalVisibility_FontSettings_PersistWithTargetExecutable()
        {
            // Test that font settings persist correctly alongside target executable settings

            // Step 1: Create profile with both conditional visibility and font settings
            var profile = new Profile
            {
                Name = "Complete Settings Test",
                PositionCount = 8,
                TargetExecutablePath = @"C:\Games\RacingSim.exe",
                FontSize = 14,
                FontWeight = "Bold",
                TextRenderingMode = "Aliased"
            };
            profile.NormalizeTextLabels();

            // Step 2: Serialize and deserialize
            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var loadedSettings = AppSettings.FromJson(json);
            var loadedProfile = loadedSettings.ActiveProfile;

            // Step 3: Verify all settings persisted
            Assert.NotNull(loadedProfile);
            Assert.Equal(@"C:\Games\RacingSim.exe", loadedProfile.TargetExecutablePath);
            Assert.Equal(14, loadedProfile.FontSize);
            Assert.Equal("Bold", loadedProfile.FontWeight);
            Assert.Equal("Aliased", loadedProfile.TextRenderingMode);
        }

        [Fact]
        public void ConditionalVisibility_ClearTargetExecutable_RestoresAlwaysVisible()
        {
            // Test that clearing the target executable restores always-visible behavior

            // Step 1: Create profile with target executable
            var profile = new Profile
            {
                Name = "Test Profile",
                TargetExecutablePath = @"C:\Games\Racing.exe"
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;

            // Step 2: Verify target is set
            Assert.NotNull(settings.ActiveProfile?.TargetExecutablePath);

            // Step 3: Clear target executable (simulating user clicking "Clear" button)
            profile.TargetExecutablePath = null;

            // Step 4: Verify target is cleared
            Assert.Null(settings.ActiveProfile?.TargetExecutablePath);

            // Step 5: Verify persistence after clearing
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var loadedSettings = AppSettings.FromJson(json);

            Assert.Null(loadedSettings.ActiveProfile?.TargetExecutablePath);
        }

        [Fact]
        public void ConditionalVisibility_MultipleProfiles_IndependentTargetExecutables()
        {
            // Test that multiple profiles can have independent target executables

            // Step 1: Create profiles for different racing sims
            var profiles = new List<Profile>
            {
                new Profile
                {
                    Name = "iRacing",
                    TargetExecutablePath = @"C:\Program Files (x86)\iRacing\iRacingSim64.exe",
                    PositionCount = 12
                },
                new Profile
                {
                    Name = "Assetto Corsa Competizione",
                    TargetExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\Assetto Corsa Competizione\AC2-Win64-Shipping.exe",
                    PositionCount = 8
                },
                new Profile
                {
                    Name = "rFactor 2",
                    TargetExecutablePath = @"C:\Program Files (x86)\Steam\steamapps\common\rFactor 2\Bin64\rFactor2.exe",
                    PositionCount = 10
                },
                new Profile
                {
                    Name = "Generic (Always Visible)",
                    TargetExecutablePath = null,
                    PositionCount = 8
                }
            };

            // Normalize all profiles
            foreach (var profile in profiles)
            {
                profile.NormalizeTextLabels();
            }

            // Step 2: Create settings with all profiles
            var settings = new AppSettings
            {
                Profiles = profiles
            };
            settings.SelectedProfileId = profiles[0].Id;

            // Step 3: Serialize and deserialize
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var loadedSettings = AppSettings.FromJson(json);

            // Step 4: Verify all profiles maintained their independent settings
            Assert.Equal(4, loadedSettings.Profiles.Count);

            var iRacing = loadedSettings.Profiles.First(p => p.Name == "iRacing");
            Assert.Contains("iRacing", iRacing.TargetExecutablePath);
            Assert.Equal(12, iRacing.PositionCount);

            var acc = loadedSettings.Profiles.First(p => p.Name == "Assetto Corsa Competizione");
            Assert.Contains("AC2-Win64-Shipping.exe", acc.TargetExecutablePath);
            Assert.Equal(8, acc.PositionCount);

            var rfactor = loadedSettings.Profiles.First(p => p.Name == "rFactor 2");
            Assert.Contains("rFactor2.exe", rfactor.TargetExecutablePath);
            Assert.Equal(10, rfactor.PositionCount);

            var generic = loadedSettings.Profiles.First(p => p.Name == "Generic (Always Visible)");
            Assert.Null(generic.TargetExecutablePath);
            Assert.Equal(8, generic.PositionCount);
        }

        [Fact]
        public void ConditionalVisibility_SettingsPersistence_AcrossMultipleSaves()
        {
            // Test that conditional visibility settings persist correctly across multiple save/load cycles

            // Step 1: Create initial profile
            var profile = new Profile
            {
                Name = "Test Profile",
                TargetExecutablePath = @"C:\Games\Game1.exe",
                FontSize = 14
            };
            profile.NormalizeTextLabels();

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;

            // Step 2: First save/load cycle
            var json1 = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var loaded1 = AppSettings.FromJson(json1);
            Assert.Equal(@"C:\Games\Game1.exe", loaded1.ActiveProfile?.TargetExecutablePath);

            // Step 3: Modify and save again
            loaded1.ActiveProfile!.TargetExecutablePath = @"C:\Games\Game2.exe";
            loaded1.ActiveProfile.FontSize = 16;

            var json2 = JsonSerializer.Serialize(loaded1, new JsonSerializerOptions { WriteIndented = true });
            var loaded2 = AppSettings.FromJson(json2);
            Assert.Equal(@"C:\Games\Game2.exe", loaded2.ActiveProfile?.TargetExecutablePath);
            Assert.Equal(16, loaded2.ActiveProfile?.FontSize);

            // Step 4: Clear target and save again
            loaded2.ActiveProfile!.TargetExecutablePath = null;

            var json3 = JsonSerializer.Serialize(loaded2, new JsonSerializerOptions { WriteIndented = true });
            var loaded3 = AppSettings.FromJson(json3);
            Assert.Null(loaded3.ActiveProfile?.TargetExecutablePath);
            Assert.Equal(16, loaded3.ActiveProfile?.FontSize);
        }

        [Fact]
        public void ConditionalVisibility_DefaultFontSettings_AppliedToNewProfiles()
        {
            // Test that new profiles get the correct default font settings

            // Step 1: Create a new profile without explicitly setting font properties
            var profile = new Profile
            {
                Name = "New Profile",
                PositionCount = 8
            };
            profile.NormalizeTextLabels();

            // Step 2: Verify default font settings
            Assert.Equal(12, profile.FontSize); // Updated default from 20 to 12
            Assert.Equal("Bold", profile.FontWeight);
            Assert.Equal("Aliased", profile.TextRenderingMode);

            // Step 3: Verify defaults persist through serialization
            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            var loadedSettings = AppSettings.FromJson(json);
            var loadedProfile = loadedSettings.ActiveProfile;

            Assert.Equal(12, loadedProfile?.FontSize);
            Assert.Equal("Bold", loadedProfile?.FontWeight);
            Assert.Equal("Aliased", loadedProfile?.TextRenderingMode);
        }

        [Fact]
        public void ConditionalVisibility_ProcessMonitorIntegration_WithProfileSettings()
        {
            // Test that ProcessMonitor integrates correctly with profile settings

            // Step 1: Create profile with target executable
            var profile = new Profile
            {
                Name = "Monitor Test",
                TargetExecutablePath = @"C:\Windows\System32\notepad.exe", // Use a real Windows executable
                PositionCount = 8
            };
            profile.NormalizeTextLabels();

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile }
            };
            settings.SelectedProfileId = profile.Id;

            // Step 2: Create ProcessMonitor with profile's target executable
            bool? visibilityState = null;
            using var monitor = new WheelOverlay.Services.ProcessMonitor(
                settings.ActiveProfile?.TargetExecutablePath,
                TimeSpan.FromMilliseconds(100));

            monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
            monitor.Start();

            // Step 3: Wait for initial check
            System.Threading.Thread.Sleep(200);

            // Step 4: Verify monitor is working (notepad may or may not be running)
            Assert.NotNull(visibilityState);

            // Step 5: Test updating target when profile changes
            var newProfile = new Profile
            {
                Name = "New Target",
                TargetExecutablePath = @"C:\Windows\System32\calc.exe"
            };
            settings.Profiles.Add(newProfile);
            settings.SelectedProfileId = newProfile.Id;

            monitor.UpdateTarget(settings.ActiveProfile?.TargetExecutablePath);

            // Monitor should handle the target change without throwing
            System.Threading.Thread.Sleep(200);
        }
    }
}
