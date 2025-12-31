namespace WheelOverlay.Models
{
    /// <summary>
    /// Represents a single position item in the grid layout.
    /// </summary>
    public class GridPositionItem
    {
        /// <summary>
        /// Gets or sets the position number display text (e.g., "#1", "#2").
        /// </summary>
        public string PositionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text label for this position.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this position is currently selected.
        /// </summary>
        public bool IsSelected { get; set; }
    }
}
