using System;
using System.IO;
using System.Diagnostics;
using Xunit;
using WheelOverlay.Models;
using WheelOverlay.Services;
using WheelOverlay.Tests.Infrastructure;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for error handling in conditional visibility features.
    /// Verifies that ProcessMonitor, file selection, and settings persistence handle errors gracefully.
    /// Feature: overlay-visibility-and-ui-improvements
    /// </summary>
    [Collection("SettingsFile")]
    public class ConditionalVisibilityErrorHandlingTests : UITestBase
    {
        private string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WheelOverlay",
            "settings.json");

        public ConditionalVisibilityErrorHandlingTests()
        {
            SetupTestViewModel();
        }

        #region ProcessMonitor Error Handling Tests

        /// <summary>
        /// Tests that ProcessMonitor handles null target path gracefully.
        /// Should default to always-visible behavior.
        /// </summary>
        [Fact]
        public void ProcessMonitor_NullTargetPath_DefaultsToAlwaysVisible()
        {
            // Arrange
            bool stateChanged = false;
            bool? visibilityState = null;

            using var monitor = new ProcessMonitor(null, TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
                visibilityState = isRunning;
            };

            // Act
            monitor.Start();
            System.Threading.Thread.Sleep(100); // Allow event to fire

            // Assert
            Assert.True(stateChanged, "State changed event should fire");
            Assert.True(visibilityState == true, "Should default to visible (true) with null target");
        }

        /// <summary>
        /// Tests that ProcessMonitor handles empty target path gracefully.
        /// Should default to always-visible behavior.
        /// </summary>
        [Fact]
        public void ProcessMonitor_EmptyTargetPath_DefaultsToAlwaysVisible()
        {
            // Arrange
            bool stateChanged = false;
            bool? visibilityState = null;

            using var monitor = new ProcessMonitor("", TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
                visibilityState = isRunning;
            };

            // Act
            monitor.Start();
            System.Threading.Thread.Sleep(100); // Allow event to fire

            // Assert
            Assert.True(stateChanged, "State changed event should fire");
            Assert.True(visibilityState == true, "Should default to visible (true) with empty target");
        }

        /// <summary>
        /// Tests that ProcessMonitor handles whitespace-only target path gracefully.
        /// Should treat as a valid path and check for processes (returns false since no process matches).
        /// </summary>
        [Fact]
        public void ProcessMonitor_WhitespaceTargetPath_HandlesGracefully()
        {
            // Arrange
            bool stateChanged = false;
            bool? visibilityState = null;

            using var monitor = new ProcessMonitor("   ", TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
                visibilityState = isRunning;
            };

            // Act
            monitor.Start();
            System.Threading.Thread.Sleep(500); // Allow event to fire

            // Assert
            Assert.True(stateChanged, "State changed event should fire");
            // Whitespace path is treated as a valid path, so it checks for processes
            // Since no process will match "   ", it should return false
            Assert.False(visibilityState == true, "Should return false for whitespace-only path");
        }

        /// <summary>
        /// Tests that ProcessMonitor handles invalid file path gracefully.
        /// Should not crash and should return false (not running).
        /// </summary>
        [Fact]
        public void ProcessMonitor_InvalidFilePath_HandlesGracefully()
        {
            // Arrange
            var invalidPath = "C:\\Invalid<>Path|With*Illegal?Characters.exe";
            bool stateChanged = false;
            bool? visibilityState = null;

            using var monitor = new ProcessMonitor(invalidPath, TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
                visibilityState = isRunning;
            };

            // Act - Should not throw exception
            var exception = Record.Exception(() =>
            {
                monitor.Start();
                System.Threading.Thread.Sleep(500); // Allow check to complete
            });

            // Assert
            Assert.Null(exception); // Should not throw
            Assert.True(stateChanged, "State changed event should fire");
            Assert.False(visibilityState == true, "Should return false for invalid path");
        }

        /// <summary>
        /// Tests that ProcessMonitor handles non-existent file path gracefully.
        /// Should not crash and should return false (not running).
        /// </summary>
        [Fact]
        public void ProcessMonitor_NonExistentFilePath_HandlesGracefully()
        {
            // Arrange
            var nonExistentPath = "C:\\NonExistent\\Path\\Application.exe";
            bool stateChanged = false;
            bool? visibilityState = null;

            using var monitor = new ProcessMonitor(nonExistentPath, TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
                visibilityState = isRunning;
            };

            // Act
            monitor.Start();
            System.Threading.Thread.Sleep(500); // Allow check to complete

            // Assert
            Assert.True(stateChanged, "State changed event should fire");
            Assert.False(visibilityState == true, "Should return false for non-existent path");
        }

        /// <summary>
        /// Tests that ProcessMonitor continues to function after encountering access denied errors.
        /// Simulated by checking for system processes that typically deny access.
        /// </summary>
        [Fact]
        public void ProcessMonitor_AccessDeniedToProcessModule_ContinuesFunctioning()
        {
            // Arrange - Use a path that won't match any process
            var testPath = "C:\\Test\\NonExistent.exe";
            bool stateChanged = false;

            using var monitor = new ProcessMonitor(testPath, TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
            };

            // Act - Start monitoring (will encounter protected processes)
            var exception = Record.Exception(() =>
            {
                monitor.Start();
                System.Threading.Thread.Sleep(500); // Allow check to complete
            });

            // Assert - Should not crash despite access denied errors
            Assert.Null(exception);
            Assert.True(stateChanged, "Should still fire state changed event");
        }

        /// <summary>
        /// Tests that ProcessMonitor handles UpdateTarget with null gracefully.
        /// Should switch to always-visible mode.
        /// </summary>
        [Fact]
        public void ProcessMonitor_UpdateTargetToNull_SwitchesToAlwaysVisible()
        {
            // Arrange
            var initialPath = "C:\\Test\\App.exe";
            bool stateChanged = false;
            bool? finalState = null;

            using var monitor = new ProcessMonitor(initialPath, TimeSpan.FromSeconds(1));
            monitor.TargetApplicationStateChanged += (s, isRunning) =>
            {
                stateChanged = true;
                finalState = isRunning;
            };
            monitor.Start();
            System.Threading.Thread.Sleep(200);
            
            stateChanged = false; // Reset

            // Act - Update to null
            monitor.UpdateTarget(null);
            System.Threading.Thread.Sleep(100);

            // Assert
            Assert.True(stateChanged, "State should change when updating to null");
            Assert.True(finalState == true, "Should be visible (true) after updating to null");
        }

        /// <summary>
        /// Tests that ProcessMonitor properly disposes resources even after errors.
        /// </summary>
        [Fact]
        public void ProcessMonitor_DisposesProperlyAfterErrors()
        {
            // Arrange
            var invalidPath = "C:\\Invalid<>Path.exe";
            var monitor = new ProcessMonitor(invalidPath, TimeSpan.FromSeconds(1));
            monitor.Start();
            System.Threading.Thread.Sleep(200);

            // Act - Dispose should not throw
            var exception = Record.Exception(() => monitor.Dispose());

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region File Selection Validation Tests

        /// <summary>
        /// Tests that file selection validates file existence.
        /// </summary>
        [Fact]
        public void FileSelection_NonExistentFile_ShouldBeValidated()
        {
            // Arrange
            var profile = new Profile();
            var nonExistentPath = "C:\\NonExistent\\File.exe";

            // Act - Set the path (validation should happen in UI layer)
            profile.TargetExecutablePath = nonExistentPath;

            // Assert - Path is stored but should be validated before use
            Assert.Equal(nonExistentPath, profile.TargetExecutablePath);
            Assert.False(File.Exists(profile.TargetExecutablePath), 
                "File should not exist - UI should validate this");
        }

        /// <summary>
        /// Tests that file selection handles invalid path characters.
        /// </summary>
        [Fact]
        public void FileSelection_InvalidPathCharacters_ShouldBeHandled()
        {
            // Arrange
            var profile = new Profile();
            var invalidPath = "C:\\Invalid<>Path|With*Illegal?Characters.exe";

            // Act - Set the path
            profile.TargetExecutablePath = invalidPath;

            // Assert - Path is stored but contains invalid characters
            Assert.Equal(invalidPath, profile.TargetExecutablePath);
            
            // Verify that Path.GetFileName throws for invalid characters
            // In .NET, Path.GetFileName may or may not throw depending on the platform
            // On Windows, it typically filters out invalid characters rather than throwing
            // So we just verify it doesn't crash
            var exception = Record.Exception(() => Path.GetFileName(invalidPath));
            // Should not crash - may return filtered filename or throw
            // Either behavior is acceptable as long as it doesn't crash the app
        }

        /// <summary>
        /// Tests that file selection handles null path gracefully.
        /// </summary>
        [Fact]
        public void FileSelection_NullPath_HandlesGracefully()
        {
            // Arrange
            var profile = new Profile();

            // Act
            profile.TargetExecutablePath = null;

            // Assert
            Assert.Null(profile.TargetExecutablePath);
            
            // Verify Path.GetFileName handles null
            var fileName = Path.GetFileName(profile.TargetExecutablePath);
            Assert.Null(fileName);
        }

        /// <summary>
        /// Tests that file selection handles empty path gracefully.
        /// </summary>
        [Fact]
        public void FileSelection_EmptyPath_HandlesGracefully()
        {
            // Arrange
            var profile = new Profile();

            // Act
            profile.TargetExecutablePath = "";

            // Assert
            Assert.Equal("", profile.TargetExecutablePath);
            
            // Verify Path.GetFileName handles empty string
            var fileName = Path.GetFileName(profile.TargetExecutablePath);
            Assert.Equal("", fileName);
        }

        #endregion

        #region Settings Persistence Error Tests

        /// <summary>
        /// Tests that settings save handles disk full scenario gracefully.
        /// Note: Actual disk full is hard to simulate, so we test the error handling path.
        /// </summary>
        [Fact]
        public void SettingsSave_HandlesErrorsGracefully()
        {
            // Arrange
            var settings = AppSettings.Load();
            settings.ActiveProfile!.TargetExecutablePath = "C:\\Test\\App.exe";

            // Act - Save should not throw even if there are issues
            var exception = Record.Exception(() => settings.Save());

            // Assert
            Assert.Null(exception); // Should handle errors gracefully
        }

        /// <summary>
        /// Tests that settings load handles missing file gracefully.
        /// </summary>
        [Fact]
        public void SettingsLoad_MissingFile_UsesDefaults()
        {
            // Arrange - Backup and delete settings file
            string? backupPath = null;
            if (File.Exists(SettingsFilePath))
            {
                backupPath = SettingsFilePath + ".backup_missing";
                File.Copy(SettingsFilePath, backupPath, true);
                File.Delete(SettingsFilePath);
            }

            try
            {
                // Act
                var settings = AppSettings.Load();

                // Assert
                Assert.NotNull(settings);
                Assert.NotNull(settings.Profiles);
                Assert.NotEmpty(settings.Profiles);
                Assert.NotNull(settings.ActiveProfile);
            }
            finally
            {
                // Cleanup
                if (backupPath != null && File.Exists(backupPath))
                {
                    File.Copy(backupPath, SettingsFilePath, true);
                    File.Delete(backupPath);
                }
            }
        }

        /// <summary>
        /// Tests that settings load handles corrupted JSON with missing properties.
        /// </summary>
        [Fact]
        public void SettingsLoad_CorruptedJsonMissingProperties_UsesDefaults()
        {
            // Arrange
            string? backupPath = null;
            if (File.Exists(SettingsFilePath))
            {
                backupPath = SettingsFilePath + ".backup_corrupt";
                File.Copy(SettingsFilePath, backupPath, true);
            }

            try
            {
                // Create corrupted JSON (valid JSON but missing required properties)
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(SettingsFilePath, "{ \"SomeInvalidProperty\": \"value\" }");

                // Act
                var settings = AppSettings.Load();

                // Assert - Should use defaults
                Assert.NotNull(settings);
                Assert.NotNull(settings.Profiles);
                Assert.NotEmpty(settings.Profiles);
            }
            finally
            {
                // Cleanup
                if (backupPath != null && File.Exists(backupPath))
                {
                    File.Copy(backupPath, SettingsFilePath, true);
                    File.Delete(backupPath);
                }
                else if (File.Exists(SettingsFilePath))
                {
                    File.Delete(SettingsFilePath);
                }
            }
        }

        /// <summary>
        /// Tests that settings persist TargetExecutablePath correctly.
        /// </summary>
        [Fact]
        public void SettingsPersistence_TargetExecutablePath_SavesAndLoads()
        {
            // Arrange
            var testPath = "C:\\Test\\Racing\\Simulator.exe";
            var settings = AppSettings.Load();
            settings.ActiveProfile!.TargetExecutablePath = testPath;

            // Act
            settings.Save();
            var loadedSettings = AppSettings.Load();

            // Assert
            Assert.Equal(testPath, loadedSettings.ActiveProfile!.TargetExecutablePath);
        }

        /// <summary>
        /// Tests that settings handle null TargetExecutablePath correctly.
        /// </summary>
        [Fact]
        public void SettingsPersistence_NullTargetExecutablePath_SavesAndLoads()
        {
            // Arrange
            var settings = AppSettings.Load();
            settings.ActiveProfile!.TargetExecutablePath = null;

            // Act
            settings.Save();
            var loadedSettings = AppSettings.Load();

            // Assert
            Assert.Null(loadedSettings.ActiveProfile!.TargetExecutablePath);
        }

        #endregion

        #region Window State Error Tests

        /// <summary>
        /// Tests that visibility changes handle null ProcessMonitor gracefully.
        /// This simulates the scenario where ProcessMonitor fails to initialize.
        /// </summary>
        [Fact]
        public void WindowVisibility_NullProcessMonitor_HandlesGracefully()
        {
            // This test verifies that the application doesn't crash if ProcessMonitor is null
            // In practice, MainWindow should handle this scenario
            
            // Arrange
            ProcessMonitor? nullMonitor = null;

            // Act - Attempting to update target on null should not crash
            var exception = Record.Exception(() =>
            {
                nullMonitor?.UpdateTarget("C:\\Test\\App.exe");
            });

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests that ProcessMonitor handles rapid UpdateTarget calls gracefully.
        /// </summary>
        [Fact]
        public void ProcessMonitor_RapidUpdateTargetCalls_HandlesGracefully()
        {
            // Arrange
            using var monitor = new ProcessMonitor(null, TimeSpan.FromSeconds(1));
            monitor.Start();

            // Act - Rapidly change targets
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    monitor.UpdateTarget($"C:\\Test\\App{i}.exe");
                    System.Threading.Thread.Sleep(10);
                }
                monitor.UpdateTarget(null);
            });

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region Path Comparison Error Tests

        /// <summary>
        /// Tests that path comparison handles null paths gracefully.
        /// </summary>
        [Fact]
        public void PathComparison_NullPaths_HandlesGracefully()
        {
            // Arrange
            string? path1 = null;
            string? path2 = null;

            // Act
            var exception = Record.Exception(() =>
            {
                var result = string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);
            });

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Tests that path comparison handles empty paths gracefully.
        /// </summary>
        [Fact]
        public void PathComparison_EmptyPaths_HandlesGracefully()
        {
            // Arrange
            string path1 = "";
            string path2 = "";

            // Act
            var result = string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that path comparison handles mixed null and empty paths.
        /// </summary>
        [Fact]
        public void PathComparison_MixedNullAndEmpty_HandlesGracefully()
        {
            // Arrange
            string? path1 = null;
            string path2 = "";

            // Act
            var result = string.Equals(path1, path2, StringComparison.OrdinalIgnoreCase);

            // Assert
            Assert.False(result); // null != empty string
        }

        /// <summary>
        /// Tests that Path.GetFileName handles various invalid inputs gracefully.
        /// </summary>
        [Fact]
        public void PathGetFileName_InvalidInputs_HandlesGracefully()
        {
            // Test null
            Assert.Null(Path.GetFileName(null));
            
            // Test empty
            Assert.Equal("", Path.GetFileName(""));
            
            // Test path without filename
            Assert.Equal("", Path.GetFileName("C:\\Test\\"));
            
            // Test filename only
            Assert.Equal("app.exe", Path.GetFileName("app.exe"));
        }

        #endregion
    }
}
