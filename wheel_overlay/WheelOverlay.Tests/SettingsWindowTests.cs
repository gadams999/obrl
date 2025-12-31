using System;
using System.Collections.Generic;
using System.Linq;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class SettingsWindowTests
    {
        [Fact]
        public void PositionCountChange_WithPopulatedPositionsBeingRemoved_ShouldRequireConfirmation()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                TextLabels = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H" }
            };
            
            int newCount = 5;
            int oldCount = profile.TextLabels.Count;
            
            // Act
            bool hasPopulatedPositions = profile.TextLabels
                .Skip(newCount)
                .Any(label => !string.IsNullOrWhiteSpace(label));
            
            // Assert
            Assert.True(hasPopulatedPositions, "Should detect populated positions being removed");
        }
        
        [Fact]
        public void PositionCountChange_WithEmptyPositionsBeingRemoved_ShouldNotRequireConfirmation()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                TextLabels = new List<string> { "A", "B", "C", "D", "E", "", "", "" }
            };
            
            int newCount = 5;
            
            // Act
            bool hasPopulatedPositions = profile.TextLabels
                .Skip(newCount)
                .Any(label => !string.IsNullOrWhiteSpace(label));
            
            // Assert
            Assert.False(hasPopulatedPositions, "Should not detect populated positions when only empty positions are removed");
        }
        
        [Fact]
        public void PositionCountChange_WithWhitespaceOnlyLabels_ShouldNotRequireConfirmation()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                TextLabels = new List<string> { "A", "B", "C", "D", "E", "   ", "  ", "\t" }
            };
            
            int newCount = 5;
            
            // Act
            bool hasPopulatedPositions = profile.TextLabels
                .Skip(newCount)
                .Any(label => !string.IsNullOrWhiteSpace(label));
            
            // Assert
            Assert.False(hasPopulatedPositions, "Should treat whitespace-only labels as empty");
        }
        
        [Fact]
        public void GridDimensionValidation_WithInvalidConfiguration_ShouldRejectConfiguration()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 2 // 2×2 = 4, which is < 8
            };
            
            // Act
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Grid capacity (4) must be >= position count (8)", result.Message);
        }
        
        [Fact]
        public void GridDimensionValidation_WithValidConfiguration_ShouldAcceptConfiguration()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4 // 2×4 = 8
            };
            
            // Act
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            // Assert
            Assert.True(result.IsValid);
        }
        
        [Fact]
        public void GridDimensionValidation_WithExcessCapacity_ShouldAcceptConfiguration()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 6,
                GridRows = 3,
                GridColumns = 3 // 3×3 = 9, which is > 6
            };
            
            // Act
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            // Assert
            Assert.True(result.IsValid);
        }
        
        [Fact]
        public void GridAutoAdjustment_WithInvalidConfiguration_ShouldAdjustToDefault()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 2 // Invalid: 2×2 = 4 < 8
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(4, profile.GridColumns); // 2×4 = 8
            Assert.True(profile.IsValidGridConfiguration());
        }
        
        [Fact]
        public void GridAutoAdjustment_WithOddPositionCount_ShouldRoundUpColumns()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 7,
                GridRows = 1,
                GridColumns = 1 // Invalid
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(4, profile.GridColumns); // Ceiling(7/2) = 4, so 2×4 = 8
            Assert.True(profile.IsValidGridConfiguration());
        }
        
        [Fact]
        public void GridAutoAdjustment_WithLargePositionCount_ShouldCreateWideGrid()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 20,
                GridRows = 2,
                GridColumns = 4 // Invalid: 2×4 = 8 < 20
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(10, profile.GridColumns); // Ceiling(20/2) = 10, so 2×10 = 20
            Assert.True(profile.IsValidGridConfiguration());
        }
        
        [Fact]
        public void GridAutoAdjustment_WithMinimumPositionCount_ShouldCreateSmallGrid()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 2,
                GridRows = 5,
                GridColumns = 5 // Valid but excessive
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(1, profile.GridColumns); // Ceiling(2/2) = 1, so 2×1 = 2
            Assert.True(profile.IsValidGridConfiguration());
        }
        
        [Fact]
        public void IsValidGridConfiguration_WithExactCapacity_ShouldReturnTrue()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4 // 3×4 = 12
            };
            
            // Act & Assert
            Assert.True(profile.IsValidGridConfiguration());
        }
        
        [Fact]
        public void IsValidGridConfiguration_WithInsufficientCapacity_ShouldReturnFalse()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 12,
                GridRows = 2,
                GridColumns = 5 // 2×5 = 10 < 12
            };
            
            // Act & Assert
            Assert.False(profile.IsValidGridConfiguration());
        }
        
        [Fact]
        public void GridDimensionValidation_WithRowsOutOfRange_ShouldRejectConfiguration()
        {
            // Arrange - Use rows > 10 to avoid capacity check failing first
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 11, // Invalid: > 10
                GridColumns = 1
            };
            
            // Act
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Grid rows must be between 1 and 10", result.Message);
        }
        
        [Fact]
        public void GridDimensionValidation_WithColumnsOutOfRange_ShouldRejectConfiguration()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 1,
                GridColumns = 11 // Invalid: > 10
            };
            
            // Act
            var result = ProfileValidator.ValidateGridDimensions(profile);
            
            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Grid columns must be between 1 and 10", result.Message);
        }
    }
}
