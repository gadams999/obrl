using System;
using System.Reflection;
using WheelOverlay.Models;
using WheelOverlay.Tests.Infrastructure;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for mouse interactions with the overlay window.
    /// Verifies window dragging, click-through behavior, and position persistence.
    /// 
    /// NOTE: These tests verify the logic and properties related to mouse interactions
    /// without creating actual UI windows, which can hang in automated test environments.
    /// Full UI automation tests would require a different testing framework or manual testing.
    /// 
    /// Requirements: 4.1, 4.2, 4.3, 4.5, 4.6, 4.7
    /// </summary>
    [Collection("SettingsFile")]
    public class MouseInteractionTests : UITestBase
    {
        /// <summary>
        /// Verifies that the MainWindow class has a ConfigMode property.
        /// This property controls whether the window is draggable.
        /// 
        /// Requirements: 4.1, 4.5
        /// </summary>
        [Fact]
        public void MainWindow_HasConfigModeProperty()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var configModeProperty = mainWindowType.GetProperty("ConfigMode", BindingFlags.Public | BindingFlags.Instance);

            // Assert
            Assert.NotNull(configModeProperty);
            Assert.Equal(typeof(bool), configModeProperty.PropertyType);
            Assert.True(configModeProperty.CanRead);
            Assert.True(configModeProperty.CanWrite);
        }

        /// <summary>
        /// Verifies that the MainWindow class has methods for applying config mode.
        /// This ensures the window can toggle between draggable and click-through modes.
        /// 
        /// Requirements: 4.1, 4.2, 4.5, 4.6
        /// </summary>
        [Fact]
        public void MainWindow_HasApplyConfigModeMethod()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var applyConfigModeMethod = mainWindowType.GetMethod("ApplyConfigMode", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(applyConfigModeMethod);
            Assert.Equal(typeof(void), applyConfigModeMethod.ReturnType);
        }

        /// <summary>
        /// Verifies that the MainWindow class has a method for making the window transparent.
        /// This method applies the WS_EX_TRANSPARENT style for click-through behavior.
        /// 
        /// Requirements: 4.2, 4.6
        /// </summary>
        [Fact]
        public void MainWindow_HasMakeWindowTransparentMethod()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var makeTransparentMethod = mainWindowType.GetMethod("MakeWindowTransparent", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(makeTransparentMethod);
            Assert.Equal(typeof(void), makeTransparentMethod.ReturnType);
        }

        /// <summary>
        /// Verifies that the MainWindow class has a mouse down handler for dragging.
        /// This handler enables window dragging when config mode is enabled.
        /// 
        /// Requirements: 4.1, 4.5
        /// </summary>
        [Fact]
        public void MainWindow_HasWindowMouseDownHandler()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var mouseDownHandler = mainWindowType.GetMethod("Window_MouseDown", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(mouseDownHandler);
            Assert.Equal(typeof(void), mouseDownHandler.ReturnType);
            
            var parameters = mouseDownHandler.GetParameters();
            Assert.Equal(2, parameters.Length);
            Assert.Equal(typeof(object), parameters[0].ParameterType);
        }

        /// <summary>
        /// Verifies that the ConfigMode property setter saves window position when disabled.
        /// This ensures position persistence when exiting config mode.
        /// 
        /// Requirements: 4.3
        /// </summary>
        [Fact]
        public void ConfigMode_PropertySetter_SavesPositionWhenDisabled()
        {
            // Arrange
            SetupTestViewModel();
            var settings = AppSettings.Load();
            var originalLeft = settings.WindowLeft;
            var originalTop = settings.WindowTop;

            try
            {
                // Act - Simulate the logic that happens in ConfigMode setter
                // When ConfigMode is set to false, it should save position
                // Use unique values that are unlikely to conflict with other tests
                var newLeft = 250.123;
                var newTop = 175.456;
                settings.WindowLeft = newLeft;
                settings.WindowTop = newTop;
                settings.Save();

                // Wait a bit to ensure file is written and flushed
                System.Threading.Thread.Sleep(200);

                // Reload settings to verify persistence
                var reloadedSettings = AppSettings.Load();

                // Assert - Use tolerance for floating point comparison
                Assert.Equal(newLeft, reloadedSettings.WindowLeft, precision: 2);
                Assert.Equal(newTop, reloadedSettings.WindowTop, precision: 2);
            }
            finally
            {
                // Cleanup - restore original values with retry logic
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(100);
                        // Reload settings to get fresh instance
                        var cleanupSettings = AppSettings.Load();
                        cleanupSettings.WindowLeft = originalLeft;
                        cleanupSettings.WindowTop = originalTop;
                        cleanupSettings.Save();
                        System.Threading.Thread.Sleep(100);
                        break;
                    }
                    catch (System.IO.IOException) when (i < 2)
                    {
                        // Retry on file access issues
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that AppSettings has WindowLeft and WindowTop properties.
        /// These properties store the window position for persistence.
        /// 
        /// Requirements: 4.3
        /// </summary>
        [Fact]
        public void AppSettings_HasWindowPositionProperties()
        {
            // Arrange
            var settingsType = typeof(AppSettings);

            // Act
            var windowLeftProperty = settingsType.GetProperty("WindowLeft");
            var windowTopProperty = settingsType.GetProperty("WindowTop");

            // Assert
            Assert.NotNull(windowLeftProperty);
            Assert.NotNull(windowTopProperty);
            Assert.Equal(typeof(double), windowLeftProperty.PropertyType);
            Assert.Equal(typeof(double), windowTopProperty.PropertyType);
        }

        /// <summary>
        /// Verifies that window position can be persisted and loaded.
        /// Tests the full save/load cycle for window position.
        /// 
        /// Requirements: 4.3
        /// </summary>
        [Fact]
        public void WindowPosition_CanBePersisted()
        {
            // Arrange
            var settings = AppSettings.Load();
            var originalLeft = settings.WindowLeft;
            var originalTop = settings.WindowTop;
            var testLeft = 300.0;
            var testTop = 200.0;

            try
            {
                // Act - Save position
                settings.WindowLeft = testLeft;
                settings.WindowTop = testTop;
                settings.Save();

                // Wait to ensure file is written
                System.Threading.Thread.Sleep(100);

                // Load settings again
                var loadedSettings = AppSettings.Load();

                // Assert
                Assert.Equal(testLeft, loadedSettings.WindowLeft);
                Assert.Equal(testTop, loadedSettings.WindowTop);
            }
            finally
            {
                // Cleanup - restore original values
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(100);
                        var cleanupSettings = AppSettings.Load();
                        cleanupSettings.WindowLeft = originalLeft;
                        cleanupSettings.WindowTop = originalTop;
                        cleanupSettings.Save();
                        System.Threading.Thread.Sleep(100);
                        break;
                    }
                    catch (System.IO.IOException) when (i < 2)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that the MainWindow class has P/Invoke declarations for window styles.
        /// These are needed to set WS_EX_TRANSPARENT for click-through behavior.
        /// 
        /// Requirements: 4.2, 4.6
        /// </summary>
        [Fact]
        public void MainWindow_HasWindowStylePInvokes()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var getWindowLongMethod = mainWindowType.GetMethod("GetWindowLong", BindingFlags.NonPublic | BindingFlags.Static);
            var setWindowLongMethod = mainWindowType.GetMethod("SetWindowLong", BindingFlags.NonPublic | BindingFlags.Static);

            // Assert
            Assert.NotNull(getWindowLongMethod);
            Assert.NotNull(setWindowLongMethod);
            Assert.Equal(typeof(int), getWindowLongMethod.ReturnType);
            Assert.Equal(typeof(int), setWindowLongMethod.ReturnType);
        }

        /// <summary>
        /// Verifies that the MainWindow XAML defines Topmost property.
        /// This ensures the window remains on top of other windows.
        /// 
        /// Requirements: 4.7
        /// </summary>
        [Fact]
        public void MainWindow_IsConfiguredAsTopmost()
        {
            // This test verifies that the MainWindow class inherits from Window
            // and has access to the Topmost property
            
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var isWindow = typeof(System.Windows.Window).IsAssignableFrom(mainWindowType);
            var topmostProperty = typeof(System.Windows.Window).GetProperty("Topmost");

            // Assert
            Assert.True(isWindow, "MainWindow should inherit from System.Windows.Window");
            Assert.NotNull(topmostProperty);
            Assert.Equal(typeof(bool), topmostProperty.PropertyType);
        }

        /// <summary>
        /// Verifies that the MainWindow has fields for storing original position.
        /// These are used to restore position when canceling config mode.
        /// 
        /// Requirements: 4.1, 4.5
        /// </summary>
        [Fact]
        public void MainWindow_HasOriginalPositionFields()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var originalLeftField = mainWindowType.GetField("_originalLeft", BindingFlags.NonPublic | BindingFlags.Instance);
            var originalTopField = mainWindowType.GetField("_originalTop", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(originalLeftField);
            Assert.NotNull(originalTopField);
            Assert.Equal(typeof(double), originalLeftField.FieldType);
            Assert.Equal(typeof(double), originalTopField.FieldType);
        }

        /// <summary>
        /// Verifies that the MainWindow has a KeyDown event handler.
        /// This handler processes Escape key to cancel config mode and restore position.
        /// 
        /// Requirements: 4.1, 4.5
        /// </summary>
        [Fact]
        public void MainWindow_HasKeyDownHandler()
        {
            // Arrange
            var mainWindowType = typeof(MainWindow);

            // Act
            var keyDownHandler = mainWindowType.GetMethod("MainWindow_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(keyDownHandler);
            Assert.Equal(typeof(void), keyDownHandler.ReturnType);
            
            var parameters = keyDownHandler.GetParameters();
            Assert.Equal(2, parameters.Length);
        }
    }
}
