using System;
using System.IO;
using System.Text.Json;

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
        public DisplayLayout Layout { get; set; } = DisplayLayout.Single;

        // Text Labels
        public string[] TextLabels { get; set; } = { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" };

        // Text Appearance
        public string SelectedTextColor { get; set; } = "#FFFFFF"; // White
        public string NonSelectedTextColor { get; set; } = "#808080"; // Gray
        public int FontSize { get; set; } = 48;
        public string FontFamily { get; set; } = "Segoe UI";
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
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

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
