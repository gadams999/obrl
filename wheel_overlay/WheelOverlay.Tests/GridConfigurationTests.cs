using System;
using System.Collections.Generic;
using WheelOverlay.Models;
using WheelOverlay.Tests.Infrastructure;
using WheelOverlay.ViewModels;
using Xunit;
using FsCheck;
using FsCheck.Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for grid layout configuration.
    /// Verifies that grid dimensions (rows and columns) can be configured correctly
    /// and that the grid layout handles different position counts properly.
    /// 
    /// Requirements: 10.1, 10.2, 10.3, 10.5, 10.6
    /// </summary>
    public class GridConfigurationTests : UITestBase
    {
        /// <summary>
        /// Verifies that grid row configuration works correctly for all valid values (1-4).
        /// Tests that setting rows 1-4 results in the correct row count in the ViewModel.
        /// Note: Grid condensing logic means we need all positions populated to see full dimensions.
        /// 
        /// Requirements: 10.1, 10.5
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void GridRows_Configuration_SetsCorrectRowCount(int rows)
        {
            // Arrange
            SetupTestViewModel();
            
            // Calculate position count to fill the grid completely
            int columns = 4;
            int positionCount = rows * columns;
            
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Grid;
                TestSettings.ActiveProfile.GridRows = rows;
                TestSettings.ActiveProfile.GridColumns = columns;
                TestSettings.ActiveProfile.PositionCount = positionCount;
                
                // Ensure all positions are populated (no empty labels)
                TestSettings.ActiveProfile.TextLabels = GenerateTextLabels(positionCount);
            }

            // Act
            var viewModel = new OverlayViewModel(TestSettings!);

            // Assert
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.Equal(rows, viewModel.Settings.ActiveProfile.GridRows);
            
            // With all positions populated, effective rows should match configured rows
            Assert.Equal(rows, viewModel.EffectiveGridRows);
            
            // Verify the grid renders without exceptions
            var exception = Record.Exception(() =>
            {
                _ = viewModel.DisplayItems;
                _ = viewModel.PopulatedPositionItems;
            });
            
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that grid column configuration works correctly for all valid values (1-4).
        /// Tests that setting columns 1-4 results in the correct column count in the ViewModel.
        /// Note: Grid condensing logic means we need all positions populated to see full dimensions.
        /// 
        /// Requirements: 10.2, 10.5
        /// </summary>
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void GridColumns_Configuration_SetsCorrectColumnCount(int columns)
        {
            // Arrange
            SetupTestViewModel();
            
            // Calculate position count to fill the grid completely
            int rows = 2;
            int positionCount = rows * columns;
            
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Grid;
                TestSettings.ActiveProfile.GridRows = rows;
                TestSettings.ActiveProfile.GridColumns = columns;
                TestSettings.ActiveProfile.PositionCount = positionCount;
                
                // Ensure all positions are populated (no empty labels)
                TestSettings.ActiveProfile.TextLabels = GenerateTextLabels(positionCount);
            }

            // Act
            var viewModel = new OverlayViewModel(TestSettings!);

            // Assert
            Assert.NotNull(viewModel.Settings);
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.Equal(columns, viewModel.Settings.ActiveProfile.GridColumns);
            
            // With all positions populated, effective columns should match configured columns
            Assert.Equal(columns, viewModel.EffectiveGridColumns);
            
            // Verify the grid renders without exceptions
            var exception = Record.Exception(() =>
            {
                _ = viewModel.DisplayItems;
                _ = viewModel.PopulatedPositionItems;
            });
            
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that changing grid dimensions triggers a re-layout.
        /// Tests that the ViewModel correctly updates when grid dimensions are changed.
        /// Note: Grid condensing logic means we need all positions populated to see full dimensions.
        /// 
        /// Requirements: 10.3, 10.5
        /// </summary>
        [Fact]
        public void GridDimensions_Change_TriggersReLayout()
        {
            // Arrange
            SetupTestViewModel();
            
            // Start with 2x4 grid (8 positions)
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Grid;
                TestSettings.ActiveProfile.GridRows = 2;
                TestSettings.ActiveProfile.GridColumns = 4;
                TestSettings.ActiveProfile.PositionCount = 8;
                TestSettings.ActiveProfile.TextLabels = GenerateTextLabels(8);
            }

            var viewModel = new OverlayViewModel(TestSettings!);
            
            // Get initial grid dimensions
            var initialRows = viewModel.EffectiveGridRows;
            var initialColumns = viewModel.EffectiveGridColumns;
            
            Assert.Equal(2, initialRows);
            Assert.Equal(4, initialColumns);

            // Act - Change grid dimensions to 4x2 (still 8 positions)
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.GridRows = 4;
                TestSettings.ActiveProfile.GridColumns = 2;
            }
            
            // Trigger property update by reassigning settings
            viewModel.Settings = TestSettings!;

            // Assert - Verify dimensions changed
            Assert.Equal(4, viewModel.EffectiveGridRows);
            Assert.Equal(2, viewModel.EffectiveGridColumns);
            
            // Verify the grid still renders without exceptions after dimension change
            var exception = Record.Exception(() =>
            {
                _ = viewModel.DisplayItems;
                _ = viewModel.PopulatedPositionItems;
            });
            
            Assert.Null(exception);
        }

        /// <summary>
        /// Property test: Grid Dimension Validity
        /// For any grid configuration with rows R and columns C where 1 â‰¤ R â‰¤ 4 and 1 â‰¤ C â‰¤ 4,
        /// the grid layout should render correctly with RÃ—C cells.
        /// Note: Tests with all positions populated to verify full configured dimensions.
        /// 
        /// Property 13: Grid Dimension Validity
        /// Validates: Requirements 10.1, 10.2, 10.3, 10.5
        /// </summary>
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 13: Grid Dimension Validity")]
        public Property Property_GridDimensionValidity()
        {
            return Prop.ForAll(
                GenerateGridDimensionConfiguration(),
                config =>
                {
                    bool noException = true;
                    string errorMessage = "";

                    try
                    {
                        // Arrange - Ensure position count fills the grid completely
                        int positionCount = config.Rows * config.Columns;
                        
                        var profile = new Profile
                        {
                            Id = Guid.NewGuid(),
                            Name = "Test Profile",
                            DeviceName = "Test Device",
                            Layout = DisplayLayout.Grid,
                            PositionCount = positionCount,
                            TextLabels = GenerateTextLabels(positionCount),
                            GridRows = config.Rows,
                            GridColumns = config.Columns
                        };

                        var settings = new AppSettings
                        {
                            Profiles = new List<Profile> { profile },
                            SelectedProfileId = profile.Id
                        };

                        // Act
                        var viewModel = new OverlayViewModel(settings);

                        // Assert - With all positions populated, effective dimensions should match configured
                        Assert.Equal(config.Rows, viewModel.EffectiveGridRows);
                        Assert.Equal(config.Columns, viewModel.EffectiveGridColumns);

                        // Verify grid renders without exceptions
                        _ = viewModel.DisplayItems;
                        _ = viewModel.PopulatedPositionItems;
                        
                        // Verify grid capacity matches position count
                        int gridCapacity = viewModel.EffectiveGridRows * viewModel.EffectiveGridColumns;
                        Assert.Equal(positionCount, gridCapacity);
                    }
                    catch (Exception ex)
                    {
                        noException = false;
                        errorMessage = ex.Message;
                    }

                    return noException
                        .Label($"Grid {config.Rows}Ã—{config.Columns}: {errorMessage}");
                });
        }

        /// <summary>
        /// Generator for valid grid dimension configurations.
        /// Generates all valid row/column combinations (1-4 Ã— 1-4).
        /// Position count is calculated to fill the grid completely.
        /// </summary>
        private static Arbitrary<GridDimensionConfiguration> GenerateGridDimensionConfiguration()
        {
            return Arb.From(
                from rows in Gen.Choose(1, 4)
                from columns in Gen.Choose(1, 4)
                select new GridDimensionConfiguration
                {
                    Rows = rows,
                    Columns = columns
                });
        }

        /// <summary>
        /// Generates text labels for positions.
        /// </summary>
        private static List<string> GenerateTextLabels(int count)
        {
            var labels = new List<string>();
            for (int i = 0; i < count; i++)
            {
                labels.Add($"POS{i + 1}");
            }
            return labels;
        }

        private class GridDimensionConfiguration
        {
            public int Rows { get; set; }
            public int Columns { get; set; }
            public int PositionCount { get; set; }
        }

        /// <summary>
        /// Property test: Grid with Different Position Counts
        /// For any position count (4, 8, 12, 16), the grid layout should handle all counts correctly.
        /// Tests that the grid adapts to different position counts and renders without errors.
        /// 
        /// Validates: Requirements 10.6
        /// </summary>
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Grid Position Count Handling")]
        public Property Property_GridHandlesDifferentPositionCounts()
        {
            return Prop.ForAll(
                GenerateGridWithVariablePositionCount(),
                config =>
                {
                    bool noException = true;
                    string errorMessage = "";

                    try
                    {
                        // Arrange
                        var profile = new Profile
                        {
                            Id = Guid.NewGuid(),
                            Name = "Test Profile",
                            DeviceName = "Test Device",
                            Layout = DisplayLayout.Grid,
                            PositionCount = config.PositionCount,
                            TextLabels = GenerateTextLabels(config.PositionCount),
                            GridRows = config.Rows,
                            GridColumns = config.Columns
                        };

                        var settings = new AppSettings
                        {
                            Profiles = new List<Profile> { profile },
                            SelectedProfileId = profile.Id
                        };

                        // Act
                        var viewModel = new OverlayViewModel(settings);

                        // Assert - Verify grid handles the position count
                        _ = viewModel.DisplayItems;
                        _ = viewModel.PopulatedPositionItems;
                        
                        // Verify all positions can be accessed
                        for (int i = 0; i < config.PositionCount; i++)
                        {
                            viewModel.CurrentPosition = i;
                            _ = viewModel.CurrentItem;
                        }
                        
                        // Verify grid capacity
                        int gridCapacity = viewModel.EffectiveGridRows * viewModel.EffectiveGridColumns;
                        Assert.True(gridCapacity >= config.PositionCount,
                            $"Grid capacity {gridCapacity} should accommodate {config.PositionCount} positions");
                        
                        // Verify populated position items count
                        var populatedItems = viewModel.PopulatedPositionItems;
                        Assert.NotNull(populatedItems);
                        Assert.Equal(config.PositionCount, populatedItems.Count);
                    }
                    catch (Exception ex)
                    {
                        noException = false;
                        errorMessage = ex.Message;
                    }

                    return noException
                        .Label($"Grid {config.Rows}Ã—{config.Columns} with {config.PositionCount} positions: {errorMessage}");
                });
        }

        /// <summary>
        /// Generator for grid configurations with variable position counts.
        /// Generates position counts (4, 8, 12, 16) with appropriate grid dimensions.
        /// </summary>
        private static Arbitrary<GridDimensionConfiguration> GenerateGridWithVariablePositionCount()
        {
            return Arb.From(
                from positionCount in Gen.Elements(4, 8, 12, 16)
                from rows in Gen.Choose(1, 4)
                from columns in Gen.Choose(1, 4)
                where rows * columns >= positionCount // Ensure grid can accommodate positions
                select new GridDimensionConfiguration
                {
                    Rows = rows,
                    Columns = columns,
                    PositionCount = positionCount
                });
        }
    }
}
