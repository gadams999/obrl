using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ProfileModelTests
    {
        [Fact]
        public void Profile_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var profile = new Profile();
            
            // Assert
            Assert.Equal(8, profile.PositionCount);
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(4, profile.GridColumns);
        }
        
        [Fact]
        public void IsValidGridConfiguration_WithValidConfiguration_ShouldReturnTrue()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 2,
                GridColumns = 4
            };
            
            // Act
            bool isValid = profile.IsValidGridConfiguration();
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void IsValidGridConfiguration_WithExactCapacity_ShouldReturnTrue()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 12,
                GridRows = 3,
                GridColumns = 4
            };
            
            // Act
            bool isValid = profile.IsValidGridConfiguration();
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void IsValidGridConfiguration_WithInsufficientCapacity_ShouldReturnFalse()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 10,
                GridRows = 2,
                GridColumns = 4
            };
            
            // Act
            bool isValid = profile.IsValidGridConfiguration();
            
            // Assert
            Assert.False(isValid);
        }
        
        [Fact]
        public void IsValidGridConfiguration_WithExcessCapacity_ShouldReturnTrue()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 6,
                GridRows = 3,
                GridColumns = 3
            };
            
            // Act
            bool isValid = profile.IsValidGridConfiguration();
            
            // Assert
            Assert.True(isValid);
        }
        
        [Fact]
        public void AdjustGridToDefault_WithEvenPositionCount_ShouldSetCorrectDimensions()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 8,
                GridRows = 5,
                GridColumns = 5
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(4, profile.GridColumns);
        }
        
        [Fact]
        public void AdjustGridToDefault_WithOddPositionCount_ShouldRoundUpColumns()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 9,
                GridRows = 3,
                GridColumns = 3
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(5, profile.GridColumns); // Ceiling(9/2) = 5
        }
        
        [Fact]
        public void AdjustGridToDefault_WithMinimumPositionCount_ShouldSetCorrectDimensions()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 2,
                GridRows = 1,
                GridColumns = 1
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(1, profile.GridColumns); // Ceiling(2/2) = 1
        }
        
        [Fact]
        public void AdjustGridToDefault_WithMaximumPositionCount_ShouldSetCorrectDimensions()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 20,
                GridRows = 1,
                GridColumns = 1
            };
            
            // Act
            profile.AdjustGridToDefault();
            
            // Assert
            Assert.Equal(2, profile.GridRows);
            Assert.Equal(10, profile.GridColumns); // Ceiling(20/2) = 10
        }
        
        [Fact]
        public void NormalizeTextLabels_WithFewerLabelsThanPositions_ShouldAddEmptyLabels()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 5,
                TextLabels = new List<string> { "A", "B", "C" }
            };
            
            // Act
            profile.NormalizeTextLabels();
            
            // Assert
            Assert.Equal(5, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
            Assert.Equal("C", profile.TextLabels[2]);
            Assert.Equal("", profile.TextLabels[3]);
            Assert.Equal("", profile.TextLabels[4]);
        }
        
        [Fact]
        public void NormalizeTextLabels_WithMoreLabelsThanPositions_ShouldRemoveExcessLabels()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 3,
                TextLabels = new List<string> { "A", "B", "C", "D", "E" }
            };
            
            // Act
            profile.NormalizeTextLabels();
            
            // Assert
            Assert.Equal(3, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
            Assert.Equal("C", profile.TextLabels[2]);
        }
        
        [Fact]
        public void NormalizeTextLabels_WithMatchingLabelCount_ShouldNotModifyLabels()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 4,
                TextLabels = new List<string> { "A", "B", "C", "D" }
            };
            
            // Act
            profile.NormalizeTextLabels();
            
            // Assert
            Assert.Equal(4, profile.TextLabels.Count);
            Assert.Equal("A", profile.TextLabels[0]);
            Assert.Equal("B", profile.TextLabels[1]);
            Assert.Equal("C", profile.TextLabels[2]);
            Assert.Equal("D", profile.TextLabels[3]);
        }
        
        [Fact]
        public void NormalizeTextLabels_WithEmptyList_ShouldAddEmptyLabels()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 3,
                TextLabels = new List<string>()
            };
            
            // Act
            profile.NormalizeTextLabels();
            
            // Assert
            Assert.Equal(3, profile.TextLabels.Count);
            Assert.All(profile.TextLabels, label => Assert.Equal("", label));
        }
        
        [Fact]
        public void NormalizeTextLabels_WithZeroPositionCount_ShouldRemoveAllLabels()
        {
            // Arrange
            var profile = new Profile
            {
                PositionCount = 0,
                TextLabels = new List<string> { "A", "B", "C" }
            };
            
            // Act
            profile.NormalizeTextLabels();
            
            // Assert
            Assert.Empty(profile.TextLabels);
        }
    }
}
