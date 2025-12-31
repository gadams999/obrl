using System;
using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Unit tests for GridLayout view functionality.
    /// Tests grid rendering, position number preservation, and grid expansion/condensing.
    /// </summary>
    public class GridLayoutTests
    {
        /// <summary>
        /// Test that grid renders correct number of cells for various configurations.
        /// Requirements: 2.5, 3.1
        /// </summary>
        [Theory]
        [InlineData(2, 4, 8, 8)] // 2x4 grid, 8 positions, all populated -> 8 cells
        [InlineData(2, 4, 8, 5)] // 2x4 grid, 8 positions, 5 populated -> 5 cells
        [InlineData(3, 3, 9, 9)] // 3x3 grid, 9 positions, all populated -> 9 cells
        [InlineData(3, 3, 9, 4)] // 3x3 grid, 9 positions, 4 populated -> 4 cells
        [InlineData(4, 5, 20, 20)] // 4x5 grid, 20 positions, all populated -> 20 cells
        [InlineData(4, 5, 20, 12)] // 4x5 grid, 20 positions, 12 populated -> 12 cells
        [InlineData(2, 3, 6, 3)] // 2x3 grid, 6 positions, 3 populated -> 3 cells
        public void GridLayout_RendersCorrectNumberOfCells(int rows, int columns, int positionCount, int populatedCount)
        {
            // Arrange - create profile with specified grid dimensions
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = positionCount,
                GridRows = rows,
                GridColumns = columns,
                TextLabels = new List<string>()
            };

            // Add populated positions at the beginning
            for (int i = 0; i < populatedCount; i++)
            {
                profile.TextLabels.Add($"Label{i + 1}");
            }

            // Fill remaining positions with empty strings
            for (int i = populatedCount; i < positionCount; i++)
            {
                profile.TextLabels.Add("");
            }

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert - PopulatedPositionItems should contain only populated positions
            Assert.Equal(populatedCount, viewModel.PopulatedPositionItems.Count);

            // Verify each item has correct data
            for (int i = 0; i < populatedCount; i++)
            {
                var item = viewModel.PopulatedPositionItems[i];
                Assert.Equal($"#{i + 1}", item.PositionNumber);
                Assert.Equal($"Label{i + 1}", item.Label);
            }
        }

        /// <summary>
        /// Test that grid maintains position numbers after condensing.
        /// Requirements: 3.4
        /// </summary>
        [Fact]
        public void GridLayout_MaintainsPositionNumbers_AfterCondensing()
        {
            // Arrange - create profile with non-contiguous populated positions
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4,
                TextLabels = new List<string>
                {
                    "Pos1",  // Position 0 - populated
                    "",      // Position 1 - empty
                    "Pos3",  // Position 2 - populated
                    "",      // Position 3 - empty
                    "Pos5",  // Position 4 - populated
                    "",      // Position 5 - empty
                    "Pos7",  // Position 6 - populated
                    ""       // Position 7 - empty
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert - should have 4 populated positions
            Assert.Equal(4, viewModel.PopulatedPositionItems.Count);

            // Verify position numbers are preserved (not renumbered)
            Assert.Equal("#1", viewModel.PopulatedPositionItems[0].PositionNumber);
            Assert.Equal("Pos1", viewModel.PopulatedPositionItems[0].Label);

            Assert.Equal("#3", viewModel.PopulatedPositionItems[1].PositionNumber);
            Assert.Equal("Pos3", viewModel.PopulatedPositionItems[1].Label);

            Assert.Equal("#5", viewModel.PopulatedPositionItems[2].PositionNumber);
            Assert.Equal("Pos5", viewModel.PopulatedPositionItems[2].Label);

            Assert.Equal("#7", viewModel.PopulatedPositionItems[3].PositionNumber);
            Assert.Equal("Pos7", viewModel.PopulatedPositionItems[3].Label);
        }

        /// <summary>
        /// Test that grid expands when all positions become populated.
        /// Requirements: 3.5
        /// </summary>
        [Fact]
        public void GridLayout_Expands_WhenAllPositionsPopulated()
        {
            // Arrange - create profile with some empty positions
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4,
                TextLabels = new List<string>
                {
                    "Pos1", "Pos2", "Pos3", "", "", "", "", ""
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act - initially should have condensed grid
            int initialRows = viewModel.EffectiveGridRows;
            int initialColumns = viewModel.EffectiveGridColumns;
            int initialCapacity = initialRows * initialColumns;

            // Verify condensed grid has capacity for 3 items
            Assert.True(initialCapacity >= 3, $"Initial capacity {initialCapacity} should be >= 3");
            Assert.True(initialCapacity < 8, $"Initial capacity {initialCapacity} should be < 8 (condensed)");

            // Now populate all positions
            profile.TextLabels = new List<string>
            {
                "Pos1", "Pos2", "Pos3", "Pos4", "Pos5", "Pos6", "Pos7", "Pos8"
            };

            // Trigger update
            viewModel.Settings = settings;

            // Assert - grid should expand to full configured dimensions
            Assert.Equal(2, viewModel.EffectiveGridRows);
            Assert.Equal(4, viewModel.EffectiveGridColumns);
            Assert.Equal(8, viewModel.PopulatedPositionItems.Count);
        }

        /// <summary>
        /// Test that grid condensing maintains aspect ratio.
        /// Requirements: 3.3
        /// </summary>
        [Theory]
        [InlineData(2, 4, 3)] // 2x4 grid (aspect ratio 0.5), 3 items
        [InlineData(3, 3, 5)] // 3x3 grid (aspect ratio 1.0), 5 items
        [InlineData(4, 2, 4)] // 4x2 grid (aspect ratio 2.0), 4 items
        public void GridLayout_MaintainsAspectRatio_WhenCondensing(int configuredRows, int configuredColumns, int populatedCount)
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = configuredRows * configuredColumns,
                GridRows = configuredRows,
                GridColumns = configuredColumns,
                TextLabels = new List<string>()
            };

            // Add populated positions
            for (int i = 0; i < populatedCount; i++)
            {
                profile.TextLabels.Add($"Label{i + 1}");
            }

            // Fill remaining with empty
            for (int i = populatedCount; i < profile.PositionCount; i++)
            {
                profile.TextLabels.Add("");
            }

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert - condensed grid should maintain similar aspect ratio
            double configuredAspectRatio = (double)configuredRows / configuredColumns;
            double effectiveAspectRatio = (double)viewModel.EffectiveGridRows / viewModel.EffectiveGridColumns;

            // Aspect ratio should be within ±0.5 of configured ratio
            double aspectRatioDiff = Math.Abs(effectiveAspectRatio - configuredAspectRatio);
            Assert.True(aspectRatioDiff <= 0.5, 
                $"Aspect ratio difference {aspectRatioDiff:F2} should be <= 0.5. " +
                $"Configured: {configuredAspectRatio:F2}, Effective: {effectiveAspectRatio:F2}");

            // Verify capacity is sufficient
            int effectiveCapacity = viewModel.EffectiveGridRows * viewModel.EffectiveGridColumns;
            Assert.True(effectiveCapacity >= populatedCount,
                $"Effective capacity {effectiveCapacity} should be >= populated count {populatedCount}");
        }

        /// <summary>
        /// Test that selected item is properly marked in grid.
        /// Requirements: 2.5, 2.6
        /// </summary>
        [Fact]
        public void GridLayout_MarksSelectedItem_Correctly()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4,
                TextLabels = new List<string>
                {
                    "Pos1", "Pos2", "Pos3", "Pos4", "Pos5", "Pos6", "Pos7", "Pos8"
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            var viewModel = new OverlayViewModel(settings);

            // Act - select position 3 (0-indexed)
            viewModel.CurrentPosition = 3;

            // Assert - position 3 should be marked as selected
            var selectedItems = viewModel.PopulatedPositionItems.Where(item => item.IsSelected).ToList();
            Assert.Single(selectedItems);
            Assert.Equal("#4", selectedItems[0].PositionNumber);
            Assert.Equal("Pos4", selectedItems[0].Label);

            // Change selection to position 6
            viewModel.CurrentPosition = 6;

            // Assert - position 6 should now be selected
            selectedItems = viewModel.PopulatedPositionItems.Where(item => item.IsSelected).ToList();
            Assert.Single(selectedItems);
            Assert.Equal("#7", selectedItems[0].PositionNumber);
            Assert.Equal("Pos7", selectedItems[0].Label);
        }

        /// <summary>
        /// Test that grid handles edge case of single populated position.
        /// Requirements: 3.1
        /// </summary>
        [Fact]
        public void GridLayout_HandlesSinglePopulatedPosition()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4,
                TextLabels = new List<string>
                {
                    "", "", "", "OnlyOne", "", "", "", ""
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert
            Assert.Single(viewModel.PopulatedPositionItems);
            Assert.Equal("#4", viewModel.PopulatedPositionItems[0].PositionNumber);
            Assert.Equal("OnlyOne", viewModel.PopulatedPositionItems[0].Label);

            // Grid should be condensed to minimal size
            Assert.True(viewModel.EffectiveGridRows >= 1);
            Assert.True(viewModel.EffectiveGridColumns >= 1);
            int capacity = viewModel.EffectiveGridRows * viewModel.EffectiveGridColumns;
            Assert.True(capacity >= 1);
        }

        /// <summary>
        /// Test that grid handles edge case of no populated positions.
        /// Requirements: 3.1
        /// </summary>
        [Fact]
        public void GridLayout_HandlesNoPopulatedPositions()
        {
            // Arrange
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Grid,
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4,
                TextLabels = new List<string>
                {
                    "", "", "", "", "", "", "", ""
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            // Act
            var viewModel = new OverlayViewModel(settings);

            // Assert
            Assert.Empty(viewModel.PopulatedPositionItems);

            // Grid should have minimal dimensions
            Assert.Equal(1, viewModel.EffectiveGridRows);
            Assert.Equal(1, viewModel.EffectiveGridColumns);
        }
    }
}
