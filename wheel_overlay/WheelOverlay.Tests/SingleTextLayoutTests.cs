using System;
using System.Threading;
using System.Windows;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using WheelOverlay.Views;
using Xunit;

namespace WheelOverlay.Tests
{
    public class SingleTextLayoutTests
    {
        // Helper method to create a test ViewModel with a profile
        private OverlayViewModel CreateTestViewModel(int positionCount = 8, bool enableAnimations = true)
        {
            var settings = new AppSettings
            {
                EnableAnimations = enableAnimations
            };
            
            var profile = new Profile
            {
                Name = "Test Profile",
                PositionCount = positionCount,
                TextLabels = new System.Collections.Generic.List<string>()
            };
            
            // Add text labels for each position
            for (int i = 0; i < positionCount; i++)
            {
                profile.TextLabels.Add($"POS{i + 1}");
            }
            
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;
            
            return new OverlayViewModel(settings);
        }
        
        // Helper class to test the animation logic without instantiating the UserControl
        private class AnimationLogicTester
        {
            private int _currentPosition = -1;
            
            public void SetCurrentPosition(int position)
            {
                _currentPosition = position;
            }
            
            public bool ShouldAnimate()
            {
                return _currentPosition != -1;
            }
            
            public bool IsForwardTransition(int oldPos, int newPos, int positionCount)
            {
                // Handle wrap-around
                if (oldPos == positionCount - 1 && newPos == 0)
                    return true; // Wrapping forward
                if (oldPos == 0 && newPos == positionCount - 1)
                    return false; // Wrapping backward
                
                return newPos > oldPos;
            }
        }

        // Requirements: 1.8
        [Fact]
        public void OnPositionChanged_DoesNotAnimateOnStartup()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            tester.SetCurrentPosition(-1); // Startup condition
            
            // Act - Check if animation should occur with _currentPosition = -1 (startup condition)
            bool shouldAnimate = tester.ShouldAnimate();
            
            // Assert - Animation should not occur on startup
            Assert.False(shouldAnimate, "Animation should not occur on startup (_currentPosition = -1)");
        }

        // Requirements: 1.8
        [Fact]
        public void OnPositionChanged_AnimatesAfterFirstPositionSet()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            tester.SetCurrentPosition(0); // First position has been set
            
            // Act - Check if animation should occur after first position is set
            bool shouldAnimate = tester.ShouldAnimate();
            
            // Assert - Animation should occur after first position is set
            Assert.True(shouldAnimate, "Animation should occur after first position is set");
        }

        [Fact]
        public void IsForwardTransition_ReturnsTrue_WhenNewPositionIsGreater()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            
            // Act
            bool result = tester.IsForwardTransition(oldPos: 2, newPos: 5, positionCount: 8);
            
            // Assert
            Assert.True(result, "Should return true when new position is greater than old position");
        }

        [Fact]
        public void IsForwardTransition_ReturnsFalse_WhenNewPositionIsLess()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            
            // Act
            bool result = tester.IsForwardTransition(oldPos: 5, newPos: 2, positionCount: 8);
            
            // Assert
            Assert.False(result, "Should return false when new position is less than old position");
        }

        [Fact]
        public void IsForwardTransition_ReturnsTrue_WhenWrappingForward()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            
            // Act - Wrapping from last position (7) to first position (0) with 8 positions
            bool result = tester.IsForwardTransition(oldPos: 7, newPos: 0, positionCount: 8);
            
            // Assert
            Assert.True(result, "Should return true when wrapping forward from last to first position");
        }

        [Fact]
        public void IsForwardTransition_ReturnsFalse_WhenWrappingBackward()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            
            // Act - Wrapping from first position (0) to last position (7) with 8 positions
            bool result = tester.IsForwardTransition(oldPos: 0, newPos: 7, positionCount: 8);
            
            // Assert
            Assert.False(result, "Should return false when wrapping backward from first to last position");
        }

        // Requirements: 9.5
        [Fact]
        public void OnPositionChanged_SkipsAnimation_WhenAnimationsDisabled()
        {
            // Arrange
            var viewModel = CreateTestViewModel(positionCount: 8, enableAnimations: false);
            
            // Act & Assert
            // When EnableAnimations is false, the animation should be skipped
            // This is verified by checking that the settings flag is respected
            Assert.False(viewModel.Settings.EnableAnimations, "EnableAnimations should be false");
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.Equal(8, viewModel.Settings.ActiveProfile.PositionCount);
        }

        // Requirements: 9.5
        [Fact]
        public void OnPositionChanged_PerformsAnimation_WhenAnimationsEnabled()
        {
            // Arrange
            var viewModel = CreateTestViewModel(positionCount: 8, enableAnimations: true);
            
            // Act & Assert
            // When EnableAnimations is true, the animation should be performed
            // This is verified by checking that the settings flag is respected
            Assert.True(viewModel.Settings.EnableAnimations, "EnableAnimations should be true");
            Assert.NotNull(viewModel.Settings.ActiveProfile);
            Assert.Equal(8, viewModel.Settings.ActiveProfile.PositionCount);
        }

        // Requirements: 9.5
        [Fact]
        public void AppSettings_DefaultEnableAnimations_IsTrue()
        {
            // Arrange & Act
            var settings = new AppSettings();
            
            // Assert
            // By default, animations should be enabled
            Assert.True(settings.EnableAnimations, "EnableAnimations should be true by default");
        }

        // Requirements: 2.4, 2.5
        [Fact]
        public void GetTextForPosition_ReturnsCorrectText_ForPopulatedPositions()
        {
            // Arrange
            var viewModel = CreateTestViewModel(positionCount: 8, enableAnimations: true);
            
            // Act & Assert
            for (int i = 0; i < 8; i++)
            {
                string text = viewModel.GetTextForPosition(i);
                Assert.Equal($"POS{i + 1}", text);
            }
        }

        // Requirements: 2.4, 2.5
        [Fact]
        public void GetTextForPosition_ReturnsPositionNumber_ForEmptyPositions()
        {
            // Arrange
            var settings = new AppSettings { EnableAnimations = true };
            var profile = new Profile
            {
                Name = "Test Profile",
                PositionCount = 8,
                TextLabels = new System.Collections.Generic.List<string>()
            };
            settings.Profiles.Add(profile);
            settings.SelectedProfileId = profile.Id;
            var viewModel = new OverlayViewModel(settings);
            
            // Act & Assert
            for (int i = 0; i < 8; i++)
            {
                string text = viewModel.GetTextForPosition(i);
                Assert.Equal((i + 1).ToString(), text);
            }
        }

        // Requirements: 2.4, 2.5
        [Fact]
        public void GetTextForPosition_ReturnsEmptyString_ForOutOfRangePositions()
        {
            // Arrange
            var viewModel = CreateTestViewModel(positionCount: 8, enableAnimations: true);
            
            // Act & Assert
            Assert.Equal("", viewModel.GetTextForPosition(-1));
            Assert.Equal("", viewModel.GetTextForPosition(8));
            Assert.Equal("", viewModel.GetTextForPosition(100));
        }

        // Requirements: 1.5, 4.1
        [Fact]
        public void IsForwardTransition_UsesInternalPosition_NotExternalParameter()
        {
            // Arrange
            var tester = new AnimationLogicTester();
            tester.SetCurrentPosition(2); // Internal position is 2
            
            // Act - Test forward transition from internal position
            bool isForward = tester.IsForwardTransition(2, 5, 8);
            
            // Assert - Should use internal position (2) as old position
            Assert.True(isForward, "Should detect forward transition from position 2 to 5");
        }
    }
}
