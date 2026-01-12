using System;
using System.Collections.Generic;
using System.IO;
using WheelOverlay.Models;
using WheelOverlay.Tests.Infrastructure;
using WheelOverlay.ViewModels;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for fresh install scenarios where no settings file exists.
    /// Verifies that the application handles first-run scenarios correctly,
    /// particularly for the vertical layout which previously crashed on fresh installs.
    /// 
    /// Requirements: 7.1, 7.2, 7.6
    /// </summary>
    [Collection("SettingsFile")]
    public class FreshInstallTests : UITestBase
    {
        private readonly string _settingsPath;

        public FreshInstallTests()
        {
            // Get the settings file path
            _settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WheelOverlay",
                "settings.json");
        }

        /// <summary>
        /// Verifies that vertical layout works correctly on a fresh install.
        /// Simulates a fresh install by ensuring no settings file exists,
        /// then creates a ViewModel with vertical layout and verifies no crash occurs.
        /// 
        /// Requirements: 7.1, 7.2, 7.6
        /// </summary>
        [Fact]
        public void VerticalLayout_OnFreshInstall_DoesNotCrash()
        {
            // Arrange - Backup existing settings if they exist
            string? backupPath = null;
            int maxRetries = 3;
            int retryDelayMs = 100;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(_settingsPath))
                    {
                        backupPath = _settingsPath + $".backup.{Guid.NewGuid()}";
                        File.Copy(_settingsPath, backupPath, true);
                        
                        // Wait a bit before deleting to ensure file handle is released
                        System.Threading.Thread.Sleep(retryDelayMs);
                        File.Delete(_settingsPath);
                    }
                    break; // Success
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    // Wait and retry
                    System.Threading.Thread.Sleep(retryDelayMs * (attempt + 1));
                }
            }

            try
            {
                // Act - Load settings (simulates fresh install)
                var settings = AppSettings.Load();
                
                // Ensure we have a profile (should be created automatically)
                Assert.NotNull(settings);
                Assert.NotEmpty(settings.Profiles);
                Assert.NotNull(settings.ActiveProfile);

                // Set vertical layout
                if (settings.ActiveProfile != null)
                {
                    settings.ActiveProfile.Layout = DisplayLayout.Vertical;
                }

                // Create ViewModel - this should not throw
                var exception = Record.Exception(() =>
                {
                    var viewModel = new OverlayViewModel(settings);

                    // Verify basic properties are accessible
                    Assert.NotNull(viewModel.Settings);
                    Assert.Equal(DisplayLayout.Vertical, viewModel.Settings.ActiveProfile?.Layout);
                    Assert.NotNull(viewModel.PopulatedPositionLabels);
                    Assert.NotNull(viewModel.DisplayItems);
                    
                    // Verify PopulatedPositionLabels is not empty
                    Assert.NotEmpty(viewModel.PopulatedPositionLabels);
                });

                // Assert - No exception should occur
                Assert.Null(exception);
            }
            finally
            {
                // Cleanup - Restore original settings if they existed
                if (backupPath != null && File.Exists(backupPath))
                {
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(retryDelayMs);
                            File.Copy(backupPath, _settingsPath, true);
                            System.Threading.Thread.Sleep(retryDelayMs);
                            File.Delete(backupPath);
                            break; // Success
                        }
                        catch (IOException) when (attempt < maxRetries - 1)
                        {
                            // Wait and retry
                            System.Threading.Thread.Sleep(retryDelayMs * (attempt + 1));
                        }
                    }
                }
                else if (File.Exists(_settingsPath))
                {
                    // Delete the test-created settings file
                    for (int attempt = 0; attempt < maxRetries; attempt++)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(retryDelayMs);
                            File.Delete(_settingsPath);
                            break; // Success
                        }
                        catch (IOException) when (attempt < maxRetries - 1)
                        {
                            // Wait and retry
                            System.Threading.Thread.Sleep(retryDelayMs * (attempt + 1));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Property test: Vertical Layout Fresh Install
        /// For any fresh installation scenario, selecting vertical layout should not cause a crash.
        /// Generates various fresh install scenarios with different configurations.
        /// 
        /// Property 10: Vertical Layout Fresh Install
        /// Validates: Requirements 7.1, 7.2, 7.6
        /// </summary>
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 10: Vertical Layout Fresh Install")]
        public Property Property_VerticalLayoutFreshInstall()
        {
            return Prop.ForAll(
                GenerateFreshInstallScenario(),
                scenario =>
                {
                    bool noException = true;
                    string errorMessage = "";

                    try
                    {
                        // Act - Create settings for fresh install scenario
                        // Don't manipulate the actual settings file - just create in-memory settings
                        var settings = CreateFreshInstallSettings(scenario);

                        // Set vertical layout
                        if (settings.ActiveProfile != null)
                        {
                            settings.ActiveProfile.Layout = DisplayLayout.Vertical;
                        }

                        // Create ViewModel - this should not throw
                        var viewModel = new OverlayViewModel(settings);

                        // Verify basic properties are accessible
                        _ = viewModel.Settings;
                        _ = viewModel.DisplayItems;
                        _ = viewModel.PopulatedPositionLabels;
                        _ = viewModel.CurrentItem;

                        // Verify PopulatedPositionLabels handles empty positions
                        var populatedLabels = viewModel.PopulatedPositionLabels;
                        Assert.NotNull(populatedLabels);

                        // Test position changes
                        for (int i = 0; i < scenario.PositionCount; i++)
                        {
                            viewModel.CurrentPosition = i;
                            _ = viewModel.CurrentItem;
                        }
                    }
                    catch (Exception ex)
                    {
                        noException = false;
                        errorMessage = ex.Message;
                    }

                    return noException
                        .Label($"Fresh install with {scenario.PositionCount} positions, {scenario.EmptyPositionCount} empty: {errorMessage}");
                });
        }

        /// <summary>
        /// Generator for fresh install scenarios.
        /// Generates various configurations that might occur on a fresh install,
        /// including different position counts and empty position scenarios.
        /// </summary>
        private static Arbitrary<FreshInstallScenario> GenerateFreshInstallScenario()
        {
            return Arb.From(
                from positionCount in Gen.Elements(4, 8, 12, 16, 20)
                from emptyCount in Gen.Choose(0, positionCount / 2) // 0 to half positions can be empty
                from hasDefaultLabels in Arb.Generate<bool>()
                select new FreshInstallScenario
                {
                    PositionCount = positionCount,
                    EmptyPositionCount = emptyCount,
                    HasDefaultLabels = hasDefaultLabels
                });
        }

        /// <summary>
        /// Creates settings for a fresh install scenario.
        /// Simulates what happens when the application runs for the first time.
        /// </summary>
        private static AppSettings CreateFreshInstallSettings(FreshInstallScenario scenario)
        {
            var textLabels = GenerateTextLabels(scenario.PositionCount, scenario.EmptyPositionCount, scenario.HasDefaultLabels);

            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Default",
                DeviceName = "BavarianSimTec Alpha",
                Layout = DisplayLayout.Vertical, // Will be set in the test
                PositionCount = scenario.PositionCount,
                TextLabels = textLabels
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            return settings;
        }

        /// <summary>
        /// Generates text labels for a fresh install scenario.
        /// Can generate default labels, empty labels, or a mix.
        /// </summary>
        private static List<string> GenerateTextLabels(int count, int emptyCount, bool hasDefaultLabels)
        {
            var labels = new List<string>();
            var random = new System.Random();
            var emptyIndices = new HashSet<int>();

            // Randomly select which positions should be empty
            while (emptyIndices.Count < emptyCount)
            {
                emptyIndices.Add(random.Next(count));
            }

            for (int i = 0; i < count; i++)
            {
                if (emptyIndices.Contains(i))
                {
                    // Randomly choose between empty string, whitespace, or null
                    var emptyType = random.Next(3);
                    labels.Add(emptyType switch
                    {
                        0 => "",
                        1 => "   ",
                        _ => ""
                    });
                }
                else if (hasDefaultLabels)
                {
                    // Use default label pattern
                    labels.Add($"POS{i + 1}");
                }
                else
                {
                    // Use custom label pattern
                    labels.Add($"BTN{i + 1}");
                }
            }

            return labels;
        }

        /// <summary>
        /// Represents a fresh install scenario configuration.
        /// </summary>
        private class FreshInstallScenario
        {
            public int PositionCount { get; set; }
            public int EmptyPositionCount { get; set; }
            public bool HasDefaultLabels { get; set; }
        }
    }
}
