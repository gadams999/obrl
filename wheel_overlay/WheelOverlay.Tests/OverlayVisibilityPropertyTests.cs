using System;
using System.Text.Json;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using Xunit;

namespace WheelOverlay.Tests
{
    public class OverlayVisibilityPropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 1: Executable Path Persists to Profile
        // Validates: Requirements 1.3
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_ExecutablePathPersistsToProfile()
        {
            return Prop.ForAll(
                Arb.From(Gen.Elements(
                    "C:\\Program Files\\iRacing\\iRacingSim64DX11.exe",
                    "C:\\Program Files (x86)\\Steam\\steamapps\\common\\assettocorsa\\acs.exe",
                    "D:\\Games\\ACC\\AC2\\Binaries\\Win64\\AC2-Win64-Shipping.exe",
                    "C:\\Users\\TestUser\\Documents\\Game.exe",
                    null
                )),
                executablePath =>
                {
                    // Arrange - Create profile with executable path
                    var profile = new Profile
                    {
                        Name = "TestProfile",
                        TargetExecutablePath = executablePath
                    };

                    // Act - Store the path
                    var storedPath = profile.TargetExecutablePath;

                    // Assert - Path should be preserved exactly
                    return (storedPath == executablePath)
                        .Label($"Executable path '{executablePath}' should be preserved, but got '{storedPath}'");
                });
        }

        // Feature: overlay-visibility-and-ui-improvements, Property 13: Settings Round-Trip Preservation
        // Validates: Requirements 6.1, 6.2, 6.3
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_SettingsRoundTripPreservation()
        {
            // Create a generator that combines all the properties we need
            var profileGen = from targetExe in Gen.Elements(
                                "C:\\Program Files\\iRacing\\iRacingSim64DX11.exe",
                                "C:\\Program Files (x86)\\Steam\\steamapps\\common\\assettocorsa\\acs.exe",
                                null)
                             from fontSize in Gen.Choose(8, 72)
                             from fontWeight in Gen.Elements("Normal", "Bold", "Light", "SemiBold")
                             from renderMode in Gen.Elements("Aliased", "ClearType", "Grayscale")
                             select (targetExe, fontSize, fontWeight, renderMode);
            
            return Prop.ForAll(
                Arb.From(profileGen),
                tuple =>
                {
                    var (targetExe, fontSize, fontWeight, renderMode) = tuple;
                    
                    // Arrange - Create profile with all new properties
                    var originalProfile = new Profile
                    {
                        Name = "TestProfile",
                        TargetExecutablePath = targetExe,
                        FontSize = fontSize,
                        FontWeight = fontWeight,
                        TextRenderingMode = renderMode
                    };
                    
                    // Ensure grid is valid
                    if (!originalProfile.IsValidGridConfiguration())
                    {
                        originalProfile.AdjustGridToDefault();
                    }
                    
                    var settings = new AppSettings();
                    settings.Profiles.Add(originalProfile);
                    settings.SelectedProfileId = originalProfile.Id;

                    // Act - Serialize and deserialize (simulating save/load)
                    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    var loadedSettings = AppSettings.FromJson(json);
                    var loadedProfile = loadedSettings.Profiles[0];

                    // Assert - All properties should be preserved
                    bool targetExePreserved = loadedProfile.TargetExecutablePath == originalProfile.TargetExecutablePath;
                    bool fontSizePreserved = loadedProfile.FontSize == originalProfile.FontSize;
                    bool fontWeightPreserved = loadedProfile.FontWeight == originalProfile.FontWeight;
                    bool renderModePreserved = loadedProfile.TextRenderingMode == originalProfile.TextRenderingMode;

                    return (targetExePreserved && fontSizePreserved && fontWeightPreserved && renderModePreserved)
                        .Label($"Settings should be preserved after save/load. " +
                               $"TargetExe: {originalProfile.TargetExecutablePath} -> {loadedProfile.TargetExecutablePath}, " +
                               $"FontSize: {originalProfile.FontSize} -> {loadedProfile.FontSize}, " +
                               $"FontWeight: {originalProfile.FontWeight} -> {loadedProfile.FontWeight}, " +
                               $"RenderMode: {originalProfile.TextRenderingMode} -> {loadedProfile.TextRenderingMode}");
                });
        }
    }
}
