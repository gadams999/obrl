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
    /// Tests for layout mode rendering and switching.
    /// Verifies that all layout modes (Vertical, Horizontal, Grid, Single) render correctly
    /// and handle position changes without exceptions.
    /// 
    /// Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.7, 6.8, 7.3
    /// </summary>
    public class LayoutModeTests : UITestBase
    {
        /// <summary>
        /// Verifies that vertical layout renders without exceptions.
        /// Creates a ViewModel with vertical layout and ensures no errors occur.
        /// 
        /// Requirements: 6.1, 6.5
        /// </summary>
        [Fact]
        public void VerticalLayout_Renders_WithoutException()
        {
            // Arrange
            SetupTestViewModel();
            
            // Ensure we have a valid profile with vertical layout
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Vertical;
            }

            // Act & Assert - Creating the ViewModel should not throw
            var exception = Record.Exception(() =>
            {
                var viewModel = new OverlayViewModel(TestSettings!);
                
                // Verify basic properties are accessible
                Assert.NotNull(viewModel.Settings);
                Assert.Equal(DisplayLayout.Vertical, viewModel.Settings.ActiveProfile?.Layout);
                Assert.NotNull(viewModel.PopulatedPositionLabels);
                Assert.NotNull(viewModel.DisplayItems);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that horizontal layout renders without exceptions.
        /// Creates a ViewModel with horizontal layout and ensures no errors occur.
        /// 
        /// Requirements: 6.2, 6.5
        /// </summary>
        [Fact]
        public void HorizontalLayout_Renders_WithoutException()
        {
            // Arrange
            SetupTestViewModel();
            
            // Ensure we have a valid profile with horizontal layout
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Horizontal;
            }

            // Act & Assert - Creating the ViewModel should not throw
            var exception = Record.Exception(() =>
            {
                var viewModel = new OverlayViewModel(TestSettings!);
                
                // Verify basic properties are accessible
                Assert.NotNull(viewModel.Settings);
                Assert.Equal(DisplayLayout.Horizontal, viewModel.Settings.ActiveProfile?.Layout);
                Assert.NotNull(viewModel.PopulatedPositionLabels);
                Assert.NotNull(viewModel.DisplayItems);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that grid layout renders without exceptions.
        /// Creates a ViewModel with grid layout and ensures no errors occur.
        /// 
        /// Requirements: 6.3, 6.5
        /// </summary>
        [Fact]
        public void GridLayout_Renders_WithoutException()
        {
            // Arrange
            SetupTestViewModel();
            
            // Ensure we have a valid profile with grid layout
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Grid;
                TestSettings.ActiveProfile.GridRows = 2;
                TestSettings.ActiveProfile.GridColumns = 4;
            }

            // Act & Assert - Creating the ViewModel should not throw
            var exception = Record.Exception(() =>
            {
                var viewModel = new OverlayViewModel(TestSettings!);
                
                // Verify basic properties are accessible
                Assert.NotNull(viewModel.Settings);
                Assert.Equal(DisplayLayout.Grid, viewModel.Settings.ActiveProfile?.Layout);
                Assert.NotNull(viewModel.PopulatedPositionLabels);
                Assert.NotNull(viewModel.DisplayItems);
                
                // Verify grid-specific properties
                Assert.True(viewModel.EffectiveGridRows > 0);
                Assert.True(viewModel.EffectiveGridColumns > 0);
                Assert.NotNull(viewModel.PopulatedPositionItems);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that single layout renders without exceptions.
        /// Creates a ViewModel with single layout and ensures no errors occur.
        /// 
        /// Requirements: 6.4, 6.5
        /// </summary>
        [Fact]
        public void SingleLayout_Renders_WithoutException()
        {
            // Arrange
            SetupTestViewModel();
            
            // Ensure we have a valid profile with single layout
            if (TestSettings?.ActiveProfile != null)
            {
                TestSettings.ActiveProfile.Layout = DisplayLayout.Single;
            }

            // Act & Assert - Creating the ViewModel should not throw
            var exception = Record.Exception(() =>
            {
                var viewModel = new OverlayViewModel(TestSettings!);
                
                // Verify basic properties are accessible
                Assert.NotNull(viewModel.Settings);
                Assert.Equal(DisplayLayout.Single, viewModel.Settings.ActiveProfile?.Layout);
                Assert.NotNull(viewModel.DisplayedText);
                Assert.False(viewModel.IsDisplayingEmptyPosition);
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that switching between layout modes does not cause crashes.
        /// Tests all possible layout transitions.
        /// 
        /// Requirements: 6.8
        /// </summary>
        [Fact]
        public void LayoutSwitch_DoesNotCrash()
        {
            // Arrange
            SetupTestViewModel();
            var viewModel = new OverlayViewModel(TestSettings!);
            
            var layouts = new[] 
            { 
                DisplayLayout.Vertical, 
                DisplayLayout.Horizontal, 
                DisplayLayout.Grid, 
                DisplayLayout.Single 
            };

            // Act & Assert - Switching between all layouts should not throw
            var exception = Record.Exception(() =>
            {
                foreach (var layout in layouts)
                {
                    if (TestSettings?.ActiveProfile != null)
                    {
                        TestSettings.ActiveProfile.Layout = layout;
                        
                        // Trigger property updates
                        viewModel.Settings = TestSettings;
                        
                        // Verify ViewModel is still functional
                        Assert.NotNull(viewModel.Settings);
                        Assert.Equal(layout, viewModel.Settings.ActiveProfile?.Layout);
                        Assert.NotNull(viewModel.DisplayItems);
                    }
                }
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Property test: Layout Rendering Stability
        /// For any layout mode with valid configuration, rendering should not throw exceptions.
        /// 
        /// Property 8: Layout Rendering Stability
        /// Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5, 6.8
        /// </summary>
        [Property(MaxTest = 100)]
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 8: Layout Rendering Stability")]
        public Property Property_LayoutRenderingStability()
        {
            return Prop.ForAll(
                GenerateLayoutConfiguration(),
                config =>
                {
                    // Arrange
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Profile",
                        DeviceName = "Test Device",
                        Layout = config.Layout,
                        PositionCount = config.PositionCount,
                        TextLabels = config.TextLabels,
                        GridRows = config.GridRows,
                        GridColumns = config.GridColumns
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };

                    // Act & Assert - Creating ViewModel should not throw
                    bool noException = true;
                    string errorMessage = "";

                    try
                    {
                        var viewModel = new OverlayViewModel(settings);

                        // Verify basic properties are accessible
                        _ = viewModel.Settings;
                        _ = viewModel.DisplayItems;
                        _ = viewModel.PopulatedPositionLabels;
                        _ = viewModel.CurrentItem;

                        // For grid layout, verify grid-specific properties
                        if (config.Layout == DisplayLayout.Grid)
                        {
                            _ = viewModel.EffectiveGridRows;
                            _ = viewModel.EffectiveGridColumns;
                            _ = viewModel.PopulatedPositionItems;
                        }

                        // For single layout, verify single-specific properties
                        if (config.Layout == DisplayLayout.Single)
                        {
                            _ = viewModel.DisplayedText;
                            _ = viewModel.IsDisplayingEmptyPosition;
                        }
                    }
                    catch (Exception ex)
                    {
                        noException = false;
                        errorMessage = ex.Message;
                    }

                    return noException
                        .Label($"Layout {config.Layout} with {config.PositionCount} positions: {errorMessage}");
                });
        }

        /// <summary>
        /// Property test: Empty Position Handling
        /// For any layout mode with empty positions, rendering should not throw errors.
        /// 
        /// Property 9: Empty Position Handling
        /// Validates: Requirements 6.7, 7.3
        /// </summary>
        [Property(MaxTest = 100)]
        [Trait("Feature", "dotnet10-upgrade-and-testing")]
        [Trait("Property", "Property 9: Empty Position Handling")]
        public Property Property_EmptyPositionHandling()
        {
            return Prop.ForAll(
                GenerateLayoutConfigurationWithEmptyPositions(),
                config =>
                {
                    // Arrange
                    var profile = new Profile
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Profile",
                        DeviceName = "Test Device",
                        Layout = config.Layout,
                        PositionCount = config.PositionCount,
                        TextLabels = config.TextLabels,
                        GridRows = config.GridRows,
                        GridColumns = config.GridColumns
                    };

                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };

                    // Act & Assert - Creating ViewModel should not throw
                    bool noException = true;
                    string errorMessage = "";

                    try
                    {
                        var viewModel = new OverlayViewModel(settings);

                        // Verify basic properties are accessible
                        _ = viewModel.Settings;
                        _ = viewModel.DisplayItems;
                        
                        // PopulatedPositionLabels should filter out empty positions
                        var populatedLabels = viewModel.PopulatedPositionLabels;
                        Assert.NotNull(populatedLabels);
                        
                        // Verify no empty labels in populated list
                        foreach (var label in populatedLabels)
                        {
                            Assert.False(string.IsNullOrWhiteSpace(label));
                        }

                        // Test position changes with empty positions
                        for (int i = 0; i < config.PositionCount; i++)
                        {
                            viewModel.CurrentPosition = i;
                            _ = viewModel.CurrentItem;
                            
                            // For single layout, verify DisplayedText handles empty positions
                            if (config.Layout == DisplayLayout.Single)
                            {
                                _ = viewModel.DisplayedText;
                                _ = viewModel.IsDisplayingEmptyPosition;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        noException = false;
                        errorMessage = ex.Message;
                    }

                    return noException
                        .Label($"Layout {config.Layout} with {config.EmptyPositionCount} empty positions: {errorMessage}");
                });
        }

        /// <summary>
        /// Generator for valid layout configurations.
        /// Generates all layout types with various position counts and grid dimensions.
        /// </summary>
        private static Arbitrary<LayoutConfiguration> GenerateLayoutConfiguration()
        {
            return Arb.From(
                from layout in Gen.Elements(DisplayLayout.Vertical, DisplayLayout.Horizontal, DisplayLayout.Grid, DisplayLayout.Single)
                from positionCount in Gen.Elements(4, 8, 12, 16, 20)
                from gridRows in Gen.Elements(1, 2, 3, 4)
                from gridColumns in Gen.Elements(1, 2, 3, 4)
                select new LayoutConfiguration
                {
                    Layout = layout,
                    PositionCount = positionCount,
                    TextLabels = GenerateTextLabels(positionCount, 0),
                    GridRows = gridRows,
                    GridColumns = gridColumns
                });
        }

        /// <summary>
        /// Generator for layout configurations with empty positions.
        /// Generates configurations where some positions have empty or whitespace labels.
        /// </summary>
        private static Arbitrary<LayoutConfiguration> GenerateLayoutConfigurationWithEmptyPositions()
        {
            return Arb.From(
                from layout in Gen.Elements(DisplayLayout.Vertical, DisplayLayout.Horizontal, DisplayLayout.Grid, DisplayLayout.Single)
                from positionCount in Gen.Elements(4, 8, 12, 16)
                from emptyCount in Gen.Choose(1, positionCount - 1) // At least 1 empty, at least 1 populated
                from gridRows in Gen.Elements(1, 2, 3, 4)
                from gridColumns in Gen.Elements(1, 2, 3, 4)
                select new LayoutConfiguration
                {
                    Layout = layout,
                    PositionCount = positionCount,
                    TextLabels = GenerateTextLabels(positionCount, emptyCount),
                    GridRows = gridRows,
                    GridColumns = gridColumns,
                    EmptyPositionCount = emptyCount
                });
        }

        /// <summary>
        /// Generates text labels for positions, with a specified number of empty positions.
        /// </summary>
        /// <param name="count">Total number of positions</param>
        /// <param name="emptyCount">Number of positions that should be empty</param>
        /// <returns>List of text labels</returns>
        private static List<string> GenerateTextLabels(int count, int emptyCount)
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
                else
                {
                    labels.Add($"POS{i + 1}");
                }
            }

            return labels;
        }

        private class LayoutConfiguration
        {
            public DisplayLayout Layout { get; set; }
            public int PositionCount { get; set; }
            public List<string> TextLabels { get; set; } = new List<string>();
            public int GridRows { get; set; }
            public int GridColumns { get; set; }
            public int EmptyPositionCount { get; set; }
        }
    }
}
