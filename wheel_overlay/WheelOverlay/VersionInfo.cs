namespace WheelOverlay
{
    /// <summary>
    /// Centralized version information for the WheelOverlay application.
    /// This class provides a single source of truth for version numbers,
    /// product name, and copyright information.
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// The semantic version of the application (MAJOR.MINOR.PATCH).
        /// </summary>
        public const string Version = "0.5.2";

        /// <summary>
        /// The product name displayed in the About dialog and other UI elements.
        /// </summary>
        public const string ProductName = "Wheel Overlay";

        /// <summary>
        /// Copyright information for the application.
        /// </summary>
        public const string Copyright = "Copyright © 2024-2026";

        /// <summary>
        /// Gets the full version string in the format "ProductName vVersion".
        /// </summary>
        /// <returns>A formatted version string (e.g., "Wheel Overlay v0.5.2").</returns>
        public static string GetFullVersionString() => $"{ProductName} v{Version}";
    }
}
