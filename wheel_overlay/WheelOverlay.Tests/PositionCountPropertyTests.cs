using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class PositionCountPropertyTests
    {
        // Feature: v0.5.0-enhancements, Property 11: Position Count Range Support
        // Validates: Requirements 4.2
        [Property(MaxTest = 100)]
        public Property Property_PositionCountRangeSupport()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)), // Generate position counts 2-20
                positionCount =>
                {
                    // Arrange & Act - Create profile with specific position count
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        PositionCount = positionCount
                    };
                    
                    // Ensure grid is valid for the position count
                    if (!profile.IsValidGridConfiguration())
                    {
                        profile.AdjustGridToDefault();
                    }
                    
                    // Normalize text labels to match position count
                    profile.NormalizeTextLabels();
                    
                    // Assert - Profile should accept the position count and configure correctly
                    bool positionCountSet = profile.PositionCount == positionCount;
                    bool textLabelsMatch = profile.TextLabels.Count == positionCount;
                    bool gridIsValid = profile.IsValidGridConfiguration();
                    
                    return (positionCountSet && textLabelsMatch && gridIsValid)
                        .Label($"Position count {positionCount} should be accepted and configured correctly. " +
                               $"PositionCount: {profile.PositionCount}, TextLabels.Count: {profile.TextLabels.Count}, " +
                               $"GridValid: {gridIsValid} (Grid: {profile.GridRows}×{profile.GridColumns})");
                });
        }

        // Feature: v0.5.0-enhancements, Property 12: Position Count Increase Preservation
        // Validates: Requirements 4.5
        [Property(MaxTest = 100)]
        public Property Property_PositionCountIncreasePreservation()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 19)),  // Initial position count (2-19)
                Arb.From(Gen.Choose(1, 10)),  // Increase amount (1-10)
                (initialCount, increaseAmount) =>
                {
                    // Ensure we don't exceed maximum
                    int newCount = Math.Min(initialCount + increaseAmount, 20);
                    
                    // Skip if no actual increase
                    if (newCount <= initialCount)
                        return true.ToProperty();
                    
                    // Arrange - Create profile with initial position count and some labels
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        PositionCount = initialCount
                    };
                    
                    profile.NormalizeTextLabels();
                    
                    // Add some text to the initial labels
                    var originalLabels = new List<string>();
                    for (int i = 0; i < initialCount; i++)
                    {
                        string label = $"Label{i + 1}";
                        profile.TextLabels[i] = label;
                        originalLabels.Add(label);
                    }
                    
                    // Act - Increase position count
                    profile.PositionCount = newCount;
                    profile.NormalizeTextLabels();
                    
                    // Assert - Original labels should be preserved
                    bool allOriginalLabelsPreserved = true;
                    for (int i = 0; i < initialCount; i++)
                    {
                        if (profile.TextLabels[i] != originalLabels[i])
                        {
                            allOriginalLabelsPreserved = false;
                            break;
                        }
                    }
                    
                    // New labels should be empty
                    bool newLabelsAreEmpty = true;
                    for (int i = initialCount; i < newCount; i++)
                    {
                        if (!string.IsNullOrEmpty(profile.TextLabels[i]))
                        {
                            newLabelsAreEmpty = false;
                            break;
                        }
                    }
                    
                    bool correctCount = profile.TextLabels.Count == newCount;
                    
                    return (allOriginalLabelsPreserved && newLabelsAreEmpty && correctCount)
                        .Label($"Increasing position count from {initialCount} to {newCount} should preserve original labels. " +
                               $"Original labels preserved: {allOriginalLabelsPreserved}, " +
                               $"New labels empty: {newLabelsAreEmpty}, " +
                               $"Correct count: {correctCount} (expected {newCount}, got {profile.TextLabels.Count})");
                });
        }

        // Feature: v0.5.0-enhancements, Property 13: Position Count Decrease Removal
        // Validates: Requirements 4.7
        [Property(MaxTest = 100)]
        public Property Property_PositionCountDecreaseRemoval()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(3, 20)),  // Initial position count (3-20)
                Arb.From(Gen.Choose(1, 10)),  // Decrease amount (1-10)
                (initialCount, decreaseAmount) =>
                {
                    // Ensure we don't go below minimum
                    int newCount = Math.Max(initialCount - decreaseAmount, 2);
                    
                    // Skip if no actual decrease
                    if (newCount >= initialCount)
                        return true.ToProperty();
                    
                    // Arrange - Create profile with initial position count and labels
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        PositionCount = initialCount
                    };
                    
                    profile.NormalizeTextLabels();
                    
                    // Add text to all labels
                    var originalLabels = new List<string>();
                    for (int i = 0; i < initialCount; i++)
                    {
                        string label = $"Label{i + 1}";
                        profile.TextLabels[i] = label;
                        originalLabels.Add(label);
                    }
                    
                    // Act - Decrease position count
                    profile.PositionCount = newCount;
                    profile.NormalizeTextLabels();
                    
                    // Assert - Labels 0 to newCount-1 should be preserved
                    bool preservedLabelsCorrect = true;
                    for (int i = 0; i < newCount; i++)
                    {
                        if (profile.TextLabels[i] != originalLabels[i])
                        {
                            preservedLabelsCorrect = false;
                            break;
                        }
                    }
                    
                    // Labels beyond newCount should be removed
                    bool correctCount = profile.TextLabels.Count == newCount;
                    bool noExtraLabels = profile.TextLabels.Count <= newCount;
                    
                    return (preservedLabelsCorrect && correctCount && noExtraLabels)
                        .Label($"Decreasing position count from {initialCount} to {newCount} should preserve labels 0-{newCount - 1} and remove labels {newCount}-{initialCount - 1}. " +
                               $"Preserved labels correct: {preservedLabelsCorrect}, " +
                               $"Correct count: {correctCount} (expected {newCount}, got {profile.TextLabels.Count}), " +
                               $"No extra labels: {noExtraLabels}");
                });
        }

        // Feature: v0.5.0-enhancements, Property 19: Grid Auto-Adjustment
        // Validates: Requirements 6.3
        [Property(MaxTest = 100)]
        public Property Property_GridAutoAdjustment()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)),  // Position count (2-20)
                Arb.From(Gen.Choose(1, 5)),   // Initial rows (1-5)
                Arb.From(Gen.Choose(1, 5)),   // Initial columns (1-5)
                (positionCount, initialRows, initialColumns) =>
                {
                    // Arrange - Create profile with grid that might be too small
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        PositionCount = positionCount,
                        GridRows = initialRows,
                        GridColumns = initialColumns
                    };
                    
                    bool initiallyValid = profile.IsValidGridConfiguration();
                    
                    // Act - Adjust grid if invalid
                    if (!initiallyValid)
                    {
                        profile.AdjustGridToDefault();
                    }
                    
                    // Assert - After adjustment, grid should be valid
                    bool nowValid = profile.IsValidGridConfiguration();
                    
                    // Check that adjustment follows the 2×N pattern
                    bool followsDefaultPattern = true;
                    if (!initiallyValid)
                    {
                        int expectedRows = 2;
                        int expectedColumns = (int)Math.Ceiling(positionCount / 2.0);
                        followsDefaultPattern = profile.GridRows == expectedRows && 
                                               profile.GridColumns == expectedColumns;
                    }
                    
                    return (nowValid && followsDefaultPattern)
                        .Label($"Position count {positionCount} with initial grid {initialRows}×{initialColumns} (capacity: {initialRows * initialColumns}, valid: {initiallyValid}). " +
                               $"After adjustment: {profile.GridRows}×{profile.GridColumns} (capacity: {profile.GridRows * profile.GridColumns}, valid: {nowValid}). " +
                               $"Follows 2×N pattern: {followsDefaultPattern}");
                });
        }
    }
}
