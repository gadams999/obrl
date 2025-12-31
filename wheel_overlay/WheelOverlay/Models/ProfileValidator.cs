using System;
using System.Collections.Generic;
using System.Linq;

namespace WheelOverlay.Models
{
    /// <summary>
    /// Provides validation and suggestion services for Profile grid configurations.
    /// </summary>
    public static class ProfileValidator
    {
        /// <summary>
        /// Validates that the grid dimensions can accommodate the position count.
        /// </summary>
        /// <param name="profile">The profile to validate.</param>
        /// <returns>A ValidationResult indicating whether the configuration is valid.</returns>
        public static ValidationResult ValidateGridDimensions(Profile profile)
        {
            if (profile == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Profile cannot be null"
                };
            }

            int capacity = profile.GridRows * profile.GridColumns;
            
            if (capacity < profile.PositionCount)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = $"Grid capacity ({capacity}) must be >= position count ({profile.PositionCount})"
                };
            }
            
            if (profile.GridRows < 1 || profile.GridRows > 10)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Grid rows must be between 1 and 10"
                };
            }
            
            if (profile.GridColumns < 1 || profile.GridColumns > 10)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Message = "Grid columns must be between 1 and 10"
                };
            }
            
            return new ValidationResult { IsValid = true };
        }
        
        /// <summary>
        /// Gets suggested grid dimensions for a given position count.
        /// Includes 2×N, 3×N, 4×N, N×2, N×3, N×4 patterns and square-ish configurations.
        /// </summary>
        /// <param name="positionCount">The number of positions to accommodate.</param>
        /// <returns>A list of suggested grid dimensions, ordered by capacity.</returns>
        public static List<GridDimension> GetSuggestedDimensions(int positionCount)
        {
            if (positionCount < 1)
            {
                return new List<GridDimension>();
            }

            var suggestions = new List<GridDimension>();
            
            // 2×N configurations
            suggestions.Add(new GridDimension(2, (int)Math.Ceiling(positionCount / 2.0)));
            
            // 3×N configurations
            if (positionCount > 3)
            {
                suggestions.Add(new GridDimension(3, (int)Math.Ceiling(positionCount / 3.0)));
            }
            
            // 4×N configurations
            if (positionCount > 4)
            {
                suggestions.Add(new GridDimension(4, (int)Math.Ceiling(positionCount / 4.0)));
            }
            
            // N×2, N×3, N×4 configurations
            suggestions.Add(new GridDimension((int)Math.Ceiling(positionCount / 2.0), 2));
            
            if (positionCount > 3)
            {
                suggestions.Add(new GridDimension((int)Math.Ceiling(positionCount / 3.0), 3));
            }
            
            if (positionCount > 4)
            {
                suggestions.Add(new GridDimension((int)Math.Ceiling(positionCount / 4.0), 4));
            }
            
            // Square-ish configurations
            int sqrt = (int)Math.Ceiling(Math.Sqrt(positionCount));
            suggestions.Add(new GridDimension(sqrt, sqrt));
            
            // Remove duplicates and sort by capacity
            return suggestions
                .Distinct()
                .OrderBy(d => d.Rows * d.Columns)
                .ToList();
        }
    }
    
    /// <summary>
    /// Represents a grid dimension configuration with rows and columns.
    /// </summary>
    public record GridDimension(int Rows, int Columns);
    
    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public record ValidationResult
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = "";
    }
}
