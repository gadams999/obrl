using System;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ProfileValidatorPropertyTests
    {
        // Feature: v0.5.0-enhancements, Property 6: Grid Dimension Validation
        // Validates: Requirements 2.4, 6.1
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_GridDimensionValidation()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(1, 10)),  // rows (1-10)
                Arb.From(Gen.Choose(1, 10)),  // columns (1-10)
                Arb.From(Gen.Choose(2, 20)),  // positionCount (2-20)
                (rows, columns, positionCount) =>
                {
                    // Arrange
                    var profile = new Profile
                    {
                        GridRows = rows,
                        GridColumns = columns,
                        PositionCount = positionCount
                    };

                    // Act
                    var result = ProfileValidator.ValidateGridDimensions(profile);

                    // Assert
                    int capacity = rows * columns;
                    bool expectedValid = capacity >= positionCount;

                    return (result.IsValid == expectedValid)
                        .Label($"Grid {rows}Ã—{columns} (capacity {capacity}) with {positionCount} positions should be {(expectedValid ? "valid" : "invalid")}, but got {result.IsValid}");
                });
        }

        // Feature: v0.5.0-enhancements, Property 20: Grid Suggestion Validity
        // Validates: Requirements 6.5
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_GridSuggestionValidity()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(2, 20)),  // positionCount (2-20)
                positionCount =>
                {
                    // Act
                    var suggestions = ProfileValidator.GetSuggestedDimensions(positionCount);

                    // Assert - All suggestions must have capacity >= positionCount
                    bool allValid = suggestions.All(d => d.Rows * d.Columns >= positionCount);

                    if (!allValid)
                    {
                        var invalidSuggestions = suggestions
                            .Where(d => d.Rows * d.Columns < positionCount)
                            .Select(d => $"{d.Rows}Ã—{d.Columns} (capacity {d.Rows * d.Columns})")
                            .ToList();

                        return false
                            .Label($"For {positionCount} positions, found invalid suggestions: {string.Join(", ", invalidSuggestions)}");
                    }

                    return allValid
                        .Label($"All {suggestions.Count} suggestions for {positionCount} positions should have capacity >= {positionCount}");
                });
        }
    }
}
