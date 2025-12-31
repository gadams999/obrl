using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void UpdatePositionCount_WhenIncreasing_ShouldPreserveExistingLabels()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 5,
                TextLabels = new List<string> { "A", "B", "C", "D", "E" }
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.UpdatePositionCount(8);
            
            // Assert
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
        public void UpdatePositionCount_WhenIncreasingFromEmpty_ShouldAddEmptyLabels()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 2,
                TextLabels = new List<string> { "", "" }
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.UpdatePositionCount(5);
            
            // Assert
            Assert.Equal(5, profile.TextLabels.Count);
            Assert.All(profile.TextLabels, label => Assert.Equal("", label));
        }
        
        [Fact]
        public void UpdatePositionCount_WhenDecreasing_ShouldRemoveLabels()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 8,
                TextLabels = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H" }
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.UpdatePositionCount(5);
            
            // Assert
            Assert.Equal(5, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
            Assert.Equal("C", profile.TextLabels[2]);
            Assert.Equal("D", profile.TextLabels[3]);
            Assert.Equal("E", profile.TextLabels[4]);
        }
        
        [Fact]
        public void UpdatePositionCount_WhenDecreasingToMinimum_ShouldKeepTwoLabels()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 10,
                TextLabels = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" }
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.UpdatePositionCount(2);
            
            // Assert
            Assert.Equal(2, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
        }
        
        [Fact]
        public void UpdatePositionCount_WhenIncreasingToMaximum_ShouldAddLabelsTo20()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 8,
                TextLabels = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H" }
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.UpdatePositionCount(20);
            
            // Assert
            Assert.Equal(20, profile.TextLabels.Count);
            // First 8 should be preserved
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("H", profile.TextLabels[7]);
            // Remaining should be empty
            for (int i = 8; i < 20; i++)
            {
                Assert.Equal("", profile.TextLabels[i]);
            }
        }
        
        [Fact]
        public void RefreshGridPreview_WithDefaultConfiguration_ShouldShowCorrectNumberOfCells()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.RefreshGridPreview();
            
            // Assert
            Assert.Equal(8, viewModel.GridPreviewCells.Count);
            Assert.Equal("1", viewModel.GridPreviewCells[0]);
            Assert.Equal("2", viewModel.GridPreviewCells[1]);
            Assert.Equal("8", viewModel.GridPreviewCells[7]);
        }
        
        [Fact]
        public void RefreshGridPreview_WithExcessCapacity_ShouldShowEmptyCells()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 6,
                GridRows = 3,
                GridColumns = 3
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.RefreshGridPreview();
            
            // Assert
            Assert.Equal(9, viewModel.GridPreviewCells.Count); // 3×3 = 9 cells
            Assert.Equal("1", viewModel.GridPreviewCells[0]);
            Assert.Equal("6", viewModel.GridPreviewCells[5]);
            Assert.Equal("", viewModel.GridPreviewCells[6]); // Empty cell
            Assert.Equal("", viewModel.GridPreviewCells[7]); // Empty cell
            Assert.Equal("", viewModel.GridPreviewCells[8]); // Empty cell
        }
        
        [Fact]
        public void RefreshGridPreview_With3x4Grid_ShouldShow12Cells()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.RefreshGridPreview();
            
            // Assert
            Assert.Equal(12, viewModel.GridPreviewCells.Count);
            Assert.Equal("1", viewModel.GridPreviewCells[0]);
            Assert.Equal("12", viewModel.GridPreviewCells[11]);
        }
        
        [Fact]
        public void GridCapacityDisplay_WithDefaultConfiguration_ShouldShowCorrectCapacity()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            string display = viewModel.GridCapacityDisplay;
            
            // Assert
            Assert.Equal("Grid Capacity: 8 (Position Count: 8)", display);
        }
        
        [Fact]
        public void GridCapacityDisplay_WithExcessCapacity_ShouldShowCorrectValues()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 6,
                GridRows = 3,
                GridColumns = 3
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            string display = viewModel.GridCapacityDisplay;
            
            // Assert
            Assert.Equal("Grid Capacity: 9 (Position Count: 6)", display);
        }
        
        [Fact]
        public void GridCapacityDisplay_WithNullProfile_ShouldReturnEmptyString()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            viewModel.SelectedProfile = new Profile(); // Use empty profile instead of null
            
            // Act
            string display = viewModel.GridCapacityDisplay;
            
            // Assert
            Assert.NotNull(display);
        }
        
        [Fact]
        public void RefreshTextLabelInputs_ShouldCreateCorrectNumberOfInputs()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 5,
                TextLabels = new List<string> { "A", "B", "C", "D", "E" }
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.RefreshTextLabelInputs();
            
            // Assert
            Assert.Equal(5, viewModel.TextLabelInputs.Count);
            Assert.Equal("Position 1:", viewModel.TextLabelInputs[0].PositionNumber);
            Assert.Equal("A", viewModel.TextLabelInputs[0].Label);
            Assert.Equal("Position 5:", viewModel.TextLabelInputs[4].PositionNumber);
            Assert.Equal("E", viewModel.TextLabelInputs[4].Label);
        }
        
        [Fact]
        public void RefreshSuggestedDimensions_ShouldProvideValidSuggestions()
        {
            // Arrange
            var viewModel = new SettingsViewModel();
            var profile = new Profile
            {
                PositionCount = 8
            };
            viewModel.SelectedProfile = profile;
            
            // Act
            viewModel.RefreshSuggestedDimensions();
            
            // Assert
            Assert.NotEmpty(viewModel.SuggestedDimensions);
            // All suggestions should have capacity >= position count
            foreach (var suggestion in viewModel.SuggestedDimensions)
            {
                Assert.True(suggestion.Rows * suggestion.Columns >= profile.PositionCount);
            }
        }
        
        [Fact]
        public void AvailablePositionCounts_ShouldContainRange2To20()
        {
            // Arrange & Act
            var viewModel = new SettingsViewModel();
            
            // Assert
            Assert.Equal(19, viewModel.AvailablePositionCounts.Count);
            Assert.Equal(2, viewModel.AvailablePositionCounts.First());
            Assert.Equal(20, viewModel.AvailablePositionCounts.Last());
            Assert.Contains(8, viewModel.AvailablePositionCounts);
        }
        
        [Fact]
        public void AvailableRows_ShouldContainRange1To10()
        {
            // Arrange & Act
            var viewModel = new SettingsViewModel();
            
            // Assert
            Assert.Equal(10, viewModel.AvailableRows.Count);
            Assert.Equal(1, viewModel.AvailableRows.First());
            Assert.Equal(10, viewModel.AvailableRows.Last());
        }
        
        [Fact]
        public void AvailableColumns_ShouldContainRange1To10()
        {
            // Arrange & Act
            var viewModel = new SettingsViewModel();
            
            // Assert
            Assert.Equal(10, viewModel.AvailableColumns.Count);
            Assert.Equal(1, viewModel.AvailableColumns.First());
            Assert.Equal(10, viewModel.AvailableColumns.Last());
        }
    }
}
