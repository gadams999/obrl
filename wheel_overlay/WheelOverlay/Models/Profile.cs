using System;
using System.Collections.Generic;

namespace WheelOverlay.Models
{
    public class Profile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Default";
        public string DeviceName { get; set; } = "BavarianSimTec Alpha";
        public DisplayLayout Layout { get; set; } = DisplayLayout.Grid;
        public List<string> TextLabels { get; set; } = new List<string>();
        
        // v0.5.0 additions
        public int PositionCount { get; set; } = 8;
        public int GridRows { get; set; } = 2;
        public int GridColumns { get; set; } = 4;
        
        // Conditional Visibility (overlay-visibility-and-ui-improvements)
        public string? TargetExecutablePath { get; set; } = null;
        
        // Font Settings (overlay-visibility-and-ui-improvements)
        public int FontSize { get; set; } = 12;
        public string FontWeight { get; set; } = "Bold";
        public string TextRenderingMode { get; set; } = "Aliased";
        
        /// <summary>
        /// Validates that the grid configuration can accommodate the position count.
        /// </summary>
        /// <returns>True if rows × columns >= PositionCount, false otherwise.</returns>
        public bool IsValidGridConfiguration()
        {
            return GridRows * GridColumns >= PositionCount;
        }
        
        /// <summary>
        /// Adjusts grid dimensions to the default 2×N configuration based on PositionCount.
        /// </summary>
        public void AdjustGridToDefault()
        {
            GridRows = 2;
            GridColumns = (int)Math.Ceiling(PositionCount / 2.0);
        }
        
        /// <summary>
        /// Ensures TextLabels list matches PositionCount by adding empty labels or removing excess labels.
        /// </summary>
        public void NormalizeTextLabels()
        {
            // Add empty labels if needed
            while (TextLabels.Count < PositionCount)
            {
                TextLabels.Add("");
            }
            
            // Remove excess labels if needed
            if (TextLabels.Count > PositionCount)
            {
                TextLabels.RemoveRange(PositionCount, TextLabels.Count - PositionCount);
            }
        }
    }
}
