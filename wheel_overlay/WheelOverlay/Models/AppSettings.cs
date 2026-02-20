using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WheelOverlay.Models
{
    public enum DisplayLayout
    {
        Single,
        Vertical,
        Horizontal,
        Grid
    }

    public class AppSettings
    {
        // Display Layout
        public DisplayLayout Layout { get; set; } = DisplayLayout.Grid;

        // Text Labels
        public string[] TextLabels { get; set; } = { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" };

        // Text Appearance
        public string SelectedTextColor { get; set; } = "#FFFFFF"; // White
        public string NonSelectedTextColor { get; set; } = "#808080"; // Gray
        public int FontSize { get; set; } = 20;
        public string FontFamily { get; set; } = "Segoe UI";

        // Move Overlay Appearance
        public string MoveOverlayBackgroundColor { get; set; } = "#CC808080"; // Semi-transparent gray
        public int MoveOverlayOpacity { get; set; } = 80; // Percentage

        // Layout Spacing
        public int ItemSpacing { get; set; } = 0;
        public int ItemPadding { get; set; } = 5;

        // Window Behavior
        public bool MinimizeToTaskbar { get; set; } = false;
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;

        // Device Selection
        public string SelectedDeviceName { get; set; } = "BavarianSimTec Alpha";

        // Animation Settings (New in v0.5.0)
        public bool EnableAnimations { get; set; } = true;

        // Profiles (New in v0.2.0)
        public List<Profile> Profiles { get; set; } = new List<Profile>();
        public Guid SelectedProfileId { get; set; } = Guid.Empty;
        
        [JsonIgnore]
        public Profile? ActiveProfile => Profiles.FirstOrDefault(p => p.Id == SelectedProfileId) ?? Profiles.FirstOrDefault();

        public static readonly string[] DefaultDeviceNames = new[]
        {
            "BavarianSimTec Alpha"
        };

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WheelOverlay",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var loadedSettings = FromJson(json);
                    Services.LogService.Info("Settings loaded successfully");
                    return loadedSettings;
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to load settings file, using defaults", ex);
            }
            
            // First run - no config file exists, use sensible defaults
            Services.LogService.Info("No settings file found, creating default settings");
            var settings = new AppSettings();
            settings.SetDefaultWindowPosition();
            
            // Create default profile with default text labels
            var defaultProfile = new Profile
            {
                Name = "Default",
                DeviceName = settings.SelectedDeviceName,
                Layout = settings.Layout,
                TextLabels = new List<string>(settings.TextLabels)
            };
            settings.Profiles.Add(defaultProfile);
            settings.SelectedProfileId = defaultProfile.Id;
            
            return settings;
        }

        /// <summary>
        /// Sets the window position to the center of the primary screen.
        /// Called on first run when no config file exists.
        /// </summary>
        private void SetDefaultWindowPosition()
        {
            try
            {
                // Get primary screen dimensions
                var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
                if (primaryScreen != null)
                {
                    var workingArea = primaryScreen.WorkingArea;
                    
                    // Estimate overlay size (approximate, actual size depends on content)
                    const double estimatedWidth = 400;
                    const double estimatedHeight = 100;
                    
                    // Center on primary screen
                    WindowLeft = workingArea.Left + (workingArea.Width - estimatedWidth) / 2;
                    WindowTop = workingArea.Top + (workingArea.Height - estimatedHeight) / 2;
                }
            }
            catch
            {
                // Fallback to original defaults if screen detection fails
                WindowLeft = 100;
                WindowTop = 100;
            }
        }

        public static AppSettings FromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var settings = JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();

                // Migration Logic: If no profiles exist, migrate legacy settings
                if (settings.Profiles.Count == 0)
                {
                    Services.LogService.Info("Migrating legacy settings to profile-based format");
                    var defaultProfile = new Profile
                    {
                        Name = "Default",
                        DeviceName = settings.SelectedDeviceName ?? "BavarianSimTec Alpha",
                        Layout = settings.Layout,
                        TextLabels = new List<string>(settings.TextLabels ?? new string[8])
                    };
                    settings.Profiles.Add(defaultProfile);
                    settings.SelectedProfileId = defaultProfile.Id;
                }
                
                // Normalize all profiles on load
                foreach (var profile in settings.Profiles)
                {
                    profile.NormalizeTextLabels();
                    
                    // Validate and auto-correct grid configurations
                    if (!profile.IsValidGridConfiguration())
                    {
                        Services.LogService.Info($"Invalid grid configuration in profile '{profile.Name}', adjusting to default");
                        profile.AdjustGridToDefault();
                    }
                }
                
                return settings;
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to parse settings JSON, using defaults", ex);
                // Return default settings if parsing fails
                var defaultSettings = new AppSettings();
                var defaultProfile = new Profile
                {
                    Name = "Default",
                    DeviceName = defaultSettings.SelectedDeviceName,
                    Layout = defaultSettings.Layout,
                    TextLabels = new List<string>(defaultSettings.TextLabels)
                };
                defaultSettings.Profiles.Add(defaultProfile);
                defaultSettings.SelectedProfileId = defaultProfile.Id;
                return defaultSettings;
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Normalize profiles before saving
                foreach (var profile in Profiles)
                {
                    profile.NormalizeTextLabels();
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
                Services.LogService.Info("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to save settings", ex);
                // Don't throw - allow application to continue with in-memory settings
            }
        }
    }
}
