using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class TestModePropertyTests
    {
        // Feature: v0.5.0-enhancements, Property 23: Test Mode Grid Support
        // Validates: Requirements 10.5
        [Property(MaxTest = 100)]
        public Property Property_TestModeGridSupport()
        {
            return Prop.ForAll(
                GenerateGridConfiguration(),
                config =>
                {
                    // Arrange
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Profile",
                        DeviceName = "Test Device",
                        Layout = DisplayLayout.Grid,
                        PositionCount = config.PositionCount,
                        GridRows = config.Rows,
                        GridColumns = config.Columns,
                        TextLabels = GenerateTextLabels(config.PositionCount)
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };

                    var viewModel = new OverlayViewModel(settings);
                    viewModel.IsTestMode = true;

                    // Act - Test that grid dimensions are calculated correctly
                    int effectiveRows = viewModel.EffectiveGridRows;
                    int effectiveColumns = viewModel.EffectiveGridColumns;
                    int gridCapacity = effectiveRows * effectiveColumns;

                    // Test that position changes work correctly
                    bool allPositionsWork = true;
                    for (int pos = 0; pos < config.PositionCount; pos++)
                    {
                        viewModel.CurrentPosition = pos;
                        
                        // Verify test mode indicator updates
                        string expectedText = $"TEST MODE - Position {pos + 1}";
                        if (viewModel.TestModeIndicatorText != expectedText)
                        {
                            allPositionsWork = false;
                            break;
                        }
                    }

                    // Assert
                    bool gridDimensionsValid = effectiveRows >= 1 && effectiveColumns >= 1;
                    bool gridCapacityValid = gridCapacity >= viewModel.PopulatedPositions.Count;
                    bool gridWithinConfigured = effectiveRows <= config.Rows && effectiveColumns <= config.Columns;

                    return (gridDimensionsValid && gridCapacityValid && gridWithinConfigured && allPositionsWork)
                        .Label($"For grid {config.Rows}×{config.Columns} with {config.PositionCount} positions: " +
                               $"effective grid {effectiveRows}×{effectiveColumns} (capacity {gridCapacity}), " +
                               $"populated positions: {viewModel.PopulatedPositions.Count}, " +
                               $"all positions work: {allPositionsWork}");
                });
        }

        // Generator for valid grid configurations
        private static Arbitrary<GridConfiguration> GenerateGridConfiguration()
        {
            return Arb.From(
                from positionCount in Gen.Choose(2, 20)
                from rows in Gen.Choose(1, 10)
                from columns in Gen.Choose(1, 10)
                where rows * columns >= positionCount
                select new GridConfiguration
                {
                    PositionCount = positionCount,
                    Rows = rows,
                    Columns = columns
                });
        }

        private static List<string> GenerateTextLabels(int count)
        {
            var labels = new List<string>();
            for (int i = 0; i < count; i++)
            {
                labels.Add($"Label{i + 1}");
            }
            return labels;
        }

        private class GridConfiguration
        {
            public int PositionCount { get; set; }
            public int Rows { get; set; }
            public int Columns { get; set; }
        }
    }
}
