using System.Reflection;

namespace WheelOverlay
{
    /// <summary>
    /// Centralized version information for the WheelOverlay application.
    /// This class provides a single source of truth for version numbers,
    /// product name, and copyright information.
    /// Version is automatically read from the assembly's AssemblyVersion attribute.
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// The semantic version of the application (MAJOR.MINOR.PATCH).
        /// Automatically read from the assembly's version at runtime.
        /// </summary>
        public static readonly string Version = GetAssemblyVersion();

        /// <summary>
        /// The product name displayed in the About dialog and other UI elements.
        /// </summary>
        public const string ProductName = "Wheel Overlay";

        /// <summary>
        /// Copyright information for the application.
        /// </summary>
        public const string Copyright = "Copyright © 2025-2026 Gavin Adams & Contributors. Licensed under MIT License.";

        /// <summary>
        /// Gets the full version string in the format "ProductName vVersion".
        /// </summary>
        /// <returns>A formatted version string (e.g., "Wheel Overlay v0.5.3").</returns>
        public static string GetFullVersionString() => $"{ProductName} v{Version}";

        /// <summary>
        /// Reads the version from the assembly's AssemblyVersion attribute.
        /// </summary>
        /// <returns>The version string (e.g., "0.5.3").</returns>
        private static string GetAssemblyVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            
            // Return MAJOR.MINOR.PATCH format (ignore build number)
            return version != null 
                ? $"{version.Major}.{version.Minor}.{version.Build}" 
                : "0.0.0";
        }
    }
}
