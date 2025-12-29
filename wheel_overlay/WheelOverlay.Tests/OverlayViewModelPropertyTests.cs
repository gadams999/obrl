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
    public class OverlayViewModelPropertyTests
    {
        // Feature: about-dialog, Property 2: Populated Position Filtering
        // Validates: Requirements 6.1, 6.3
        [Property(MaxTest = 100)]
        public Property Property_PopulatedPositionFiltering()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Act
                    var populatedCount = viewModel.PopulatedPositions.Count;
                    var expectedCount = profile.TextLabels.Count(label => !string.IsNullOrWhiteSpace(label));

                    // Assert
                    return (populatedCount == expectedCount)
                        .Label($"Expected {expectedCount} populated positions, got {populatedCount}");
                });
        }

        // Feature: about-dialog, Property 3: Position Number Preservation
        // Validates: Requirements 6.4
        [Property(MaxTest = 100)]
        public Property Property_PositionNumberPreservation()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Act
                    var populatedPositions = viewModel.PopulatedPositions;
                    var populatedLabels = viewModel.PopulatedPositionLabels;

                    // Assert - verify that each position maps to the correct label
                    bool allPositionsCorrect = true;
                    for (int i = 0; i < populatedPositions.Count; i++)
                    {
                        var position = populatedPositions[i];
                        var expectedLabel = profile.TextLabels[position];
                        var actualLabel = populatedLabels[i];
                        
                        if (expectedLabel != actualLabel)
                        {
                            allPositionsCorrect = false;
                            break;
                        }
                    }

                    return allPositionsCorrect
                        .Label("Position numbers should be preserved and map to correct labels");
                });
        }

        // Feature: about-dialog, Property 4: Configuration Change Reactivity
        // Validates: Requirements 6.5
        [Property(MaxTest = 100)]
        public Property Property_ConfigurationChangeReactivity()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                GenerateProfileWithMixedLabels(),
                (profile1, profile2) =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile1 },
                        SelectedProfileId = profile1.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    var initialCount = viewModel.PopulatedPositions.Count;

                    // Act - change configuration
                    profile2.Id = profile1.Id; // Keep same ID to maintain active profile
                    settings.Profiles[0] = profile2;
                    viewModel.Settings = settings;

                    var newCount = viewModel.PopulatedPositions.Count;
                    var expectedNewCount = profile2.TextLabels.Count(label => !string.IsNullOrWhiteSpace(label));

                    // Assert
                    return (newCount == expectedNewCount)
                        .Label($"After configuration change, expected {expectedNewCount} populated positions, got {newCount}");
                });
        }

        // Generator for profiles with mixed empty and populated labels
        private static Arbitrary<Profile> GenerateProfileWithMixedLabels()
        {
            return Arb.From(
                from labelCount in Gen.Choose(1, 16) // 1 to 16 positions
                from labels in Gen.ListOf(labelCount, GenerateLabelOrEmpty())
                select new Profile
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Profile",
                    DeviceName = "Test Device",
                    Layout = DisplayLayout.Vertical,
                    TextLabels = labels.ToList()
                });
        }

        // Generator for labels that can be empty or populated
        private static Gen<string> GenerateLabelOrEmpty()
        {
            return Gen.Frequency(
                Tuple.Create(3, GenerateNonEmptyLabel()), // 60% chance of non-empty
                Tuple.Create(1, Gen.Constant("")),         // 20% chance of empty string
                Tuple.Create(1, Gen.Constant("   "))       // 20% chance of whitespace
            );
        }

        // Generator for non-empty labels
        private static Gen<string> GenerateNonEmptyLabel()
        {
            return Gen.Elements("DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF", "ABS", "BRAKE");
        }

        // Feature: about-dialog, Property 5: Empty Position Flash Trigger
        // Validates: Requirements 7.1, 8.3
        [Property(MaxTest = 100)]
        public Property Property_EmptyPositionFlashTrigger()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Find an empty position and a populated position
                    var emptyPositions = new List<int>();
                    var populatedPositions = new List<int>();
                    for (int i = 0; i < profile.TextLabels.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                        {
                            emptyPositions.Add(i);
                        }
                        else
                        {
                            populatedPositions.Add(i);
                        }
                    }

                    // If no empty positions exist, property is trivially true
                    if (emptyPositions.Count == 0)
                    {
                        return true.Label("No empty positions to test");
                    }

                    // If no populated positions exist, we can't set up the test properly
                    if (populatedPositions.Count == 0)
                    {
                        return true.Label("Need at least one populated position to set up test");
                    }

                    // Start at a populated position to ensure we're changing position
                    viewModel.CurrentPosition = populatedPositions[0];
                    
                    // Act - select an empty position
                    var emptyPosition = emptyPositions[0];
                    viewModel.CurrentPosition = emptyPosition;

                    // Assert - flash should be triggered
                    return viewModel.IsFlashing
                        .Label($"Flash should be triggered when selecting empty position {emptyPosition}");
                });
        }

        // Feature: about-dialog, Property 7: Flash Termination on Populated Selection
        // Validates: Requirements 7.4
        [Property(MaxTest = 100)]
        public Property Property_FlashTerminationOnPopulatedSelection()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Find an empty position and a populated position
                    var emptyPositions = new List<int>();
                    var populatedPositions = new List<int>();
                    for (int i = 0; i < profile.TextLabels.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                        {
                            emptyPositions.Add(i);
                        }
                        else
                        {
                            populatedPositions.Add(i);
                        }
                    }

                    // If no empty or no populated positions exist, property is trivially true
                    if (emptyPositions.Count == 0 || populatedPositions.Count == 0)
                    {
                        return true.Label("Need both empty and populated positions to test");
                    }

                    // Act - select empty position first, then populated position
                    viewModel.CurrentPosition = emptyPositions[0];
                    // Flash should be triggered (but we don't check here due to async)
                    
                    viewModel.CurrentPosition = populatedPositions[0];

                    // Assert - flash should be stopped
                    return (!viewModel.IsFlashing)
                        .Label($"Flash should stop when selecting populated position {populatedPositions[0]} after empty position {emptyPositions[0]}");
                });
        }

        // Feature: about-dialog, Property 8: Flash Restart on Empty Selection
        // Validates: Requirements 7.5
        [Property(MaxTest = 100)]
        public Property Property_FlashRestartOnEmptySelection()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Find at least two empty positions
                    var emptyPositions = new List<int>();
                    for (int i = 0; i < profile.TextLabels.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                        {
                            emptyPositions.Add(i);
                        }
                    }

                    // If less than 2 empty positions exist, property is trivially true
                    if (emptyPositions.Count < 2)
                    {
                        return true.Label("Need at least 2 empty positions to test flash restart");
                    }

                    // Act - select first empty position, then second empty position
                    viewModel.CurrentPosition = emptyPositions[0];
                    // Flash should be triggered
                    
                    viewModel.CurrentPosition = emptyPositions[1];

                    // Assert - flash should still be active (restarted)
                    return viewModel.IsFlashing
                        .Label($"Flash should restart when selecting another empty position {emptyPositions[1]} after empty position {emptyPositions[0]}");
                });
        }

        // Feature: about-dialog, Property 9: Single Layout Last Position Display
        // Validates: Requirements 8.1
        [Property(MaxTest = 100)]
        public Property Property_SingleLayoutLastPositionDisplay()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Find an empty position and a populated position
                    var emptyPositions = new List<int>();
                    var populatedPositions = new List<int>();
                    for (int i = 0; i < profile.TextLabels.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                        {
                            emptyPositions.Add(i);
                        }
                        else
                        {
                            populatedPositions.Add(i);
                        }
                    }

                    // If no empty or no populated positions exist, property is trivially true
                    if (emptyPositions.Count == 0 || populatedPositions.Count == 0)
                    {
                        return true.Label("Need both empty and populated positions to test");
                    }

                    // Act - select a populated position first, then an empty position
                    var populatedPosition = populatedPositions[0];
                    viewModel.CurrentPosition = populatedPosition;
                    var expectedText = profile.TextLabels[populatedPosition];

                    // Now select an empty position
                    viewModel.CurrentPosition = emptyPositions[0];

                    // Assert - DisplayedText should still show the last populated position's text
                    return (viewModel.DisplayedText == expectedText)
                        .Label($"When selecting empty position {emptyPositions[0]}, should display last populated text '{expectedText}', but got '{viewModel.DisplayedText}'");
                });
        }

        // Feature: about-dialog, Property 10: Single Layout Empty Position Color
        // Validates: Requirements 8.2
        [Property(MaxTest = 100)]
        public Property Property_SingleLayoutEmptyPositionColor()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Find an empty position and a populated position
                    var emptyPositions = new List<int>();
                    var populatedPositions = new List<int>();
                    for (int i = 0; i < profile.TextLabels.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                        {
                            emptyPositions.Add(i);
                        }
                        else
                        {
                            populatedPositions.Add(i);
                        }
                    }

                    // If no empty or no populated positions exist, property is trivially true
                    if (emptyPositions.Count == 0 || populatedPositions.Count == 0)
                    {
                        return true.Label("Need both empty and populated positions to test");
                    }

                    // Act - select a populated position first, then an empty position
                    viewModel.CurrentPosition = populatedPositions[0];
                    viewModel.CurrentPosition = emptyPositions[0];

                    // Assert - IsDisplayingEmptyPosition should be true when on empty position
                    return viewModel.IsDisplayingEmptyPosition
                        .Label($"When selecting empty position {emptyPositions[0]}, IsDisplayingEmptyPosition should be true");
                });
        }

        // Feature: about-dialog, Property 11: Populated Selection No Flash
        // Validates: Requirements 8.5
        [Property(MaxTest = 100)]
        public Property Property_PopulatedSelectionNoFlash()
        {
            return Prop.ForAll(
                GenerateProfileWithMixedLabels(),
                profile =>
                {
                    // Arrange
                    var settings = new AppSettings
                    {
                        Profiles = new List<Profile> { profile },
                        SelectedProfileId = profile.Id
                    };
                    var viewModel = new OverlayViewModel(settings);

                    // Find an empty position and at least two populated positions
                    var emptyPositions = new List<int>();
                    var populatedPositions = new List<int>();
                    for (int i = 0; i < profile.TextLabels.Count; i++)
                    {
                        if (string.IsNullOrWhiteSpace(profile.TextLabels[i]))
                        {
                            emptyPositions.Add(i);
                        }
                        else
                        {
                            populatedPositions.Add(i);
                        }
                    }

                    // If no empty positions or less than 2 populated positions, property is trivially true
                    if (emptyPositions.Count == 0 || populatedPositions.Count < 2)
                    {
                        return true.Label("Need at least one empty and two populated positions to test");
                    }

                    // Act - select empty position first, then a populated position
                    viewModel.CurrentPosition = emptyPositions[0];
                    // Flash should be triggered (but we don't check here due to async)
                    
                    viewModel.CurrentPosition = populatedPositions[0];

                    // Assert - IsDisplayingEmptyPosition should be false when on populated position
                    return (!viewModel.IsDisplayingEmptyPosition)
                        .Label($"When selecting populated position {populatedPositions[0]} after empty position, IsDisplayingEmptyPosition should be false");
                });
        }
    }
}
