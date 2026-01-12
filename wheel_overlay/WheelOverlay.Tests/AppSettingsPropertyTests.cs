using System;
using System.Collections.Generic;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class AppSettingsPropertyTests
    {
        // Feature: v0.5.0-enhancements, Property 7: Grid Dimension Persistence
        // Validates: Requirements 2.7
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_GridDimensionPersistence()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 10)),  // rows (1-10)
                Arb.From(Gen.Choose(1, 10)),  // columns (1-10)
                Arb.From(Gen.Choose(2, 20)),  // positionCount (2-20)
                (rows, columns, positionCount) =>
                {
                    // Arrange - Create profile with specific grid dimensions
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        GridRows = rows,
                        GridColumns = columns,
                        PositionCount = positionCount
                    };
                    
                    // Ensure grid is valid for this test
                    if (!profile.IsValidGridConfiguration())
                    {
                        profile.AdjustGridToDefault();
                    }
                    
                    var originalRows = profile.GridRows;
                    var originalColumns = profile.GridColumns;
                    
                    var settings = new AppSettings();
                    settings.Profiles.Add(profile);
                    settings.SelectedProfileId = profile.Id;

                    // Act - Serialize and deserialize (simulating save/load)
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    var loadedSettings = AppSettings.FromJson(json);
                    var loadedProfile = loadedSettings.Profiles[0];

                    // Assert - Grid dimensions should be preserved
                    bool rowsPreserved = loadedProfile.GridRows == originalRows;
                    bool columnsPreserved = loadedProfile.GridColumns == originalColumns;

                    return (rowsPreserved && columnsPreserved)
                        .Label($"Grid dimensions {originalRows}Ã—{originalColumns} should be preserved after save/load, but got {loadedProfile.GridRows}Ã—{loadedProfile.GridColumns}");
                });
        }

        // Feature: v0.5.0-enhancements, Property 14: Position Count Persistence
        // Validates: Requirements 4.8
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_PositionCountPersistence()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)),  // positionCount (2-20)
                positionCount =>
                {
                    // Arrange - Create profile with specific position count
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        PositionCount = positionCount
                    };
                    
                    // Ensure grid is valid
                    if (!profile.IsValidGridConfiguration())
                    {
                        profile.AdjustGridToDefault();
                    }
                    
                    var settings = new AppSettings();
                    settings.Profiles.Add(profile);
                    settings.SelectedProfileId = profile.Id;

                    // Act - Serialize and deserialize (simulating save/load)
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    var loadedSettings = AppSettings.FromJson(json);
                    var loadedProfile = loadedSettings.Profiles[0];

                    // Assert - Position count should be preserved
                    return (loadedProfile.PositionCount == positionCount)
                        .Label($"Position count {positionCount} should be preserved after save/load, but got {loadedProfile.PositionCount}");
                });
        }
    }
}
