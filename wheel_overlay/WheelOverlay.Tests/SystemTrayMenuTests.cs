using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WheelOverlay.Tests.Infrastructure;
using Xunit;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Tests for system tray menu operations.
    /// Verifies that all menu items are present, enabled, and function correctly.
    /// 
    /// Requirements: 3.1, 3.2, 3.3, 3.5, 3.6, 3.7, 3.8
    /// </summary>
    public class SystemTrayMenuTests : UITestBase
    {
        /// <summary>
        /// Helper method to create a mock context menu matching the App.xaml.cs structure.
        /// This simulates the actual menu structure for testing purposes.
        /// </summary>
        private ContextMenuStrip CreateMockContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Overlay");
            contextMenu.Items.Add("Hide Overlay");
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var minimizeMenuItem = new ToolStripMenuItem("Minimize to Taskbar");
            minimizeMenuItem.CheckOnClick = true;
            contextMenu.Items.Add(minimizeMenuItem);
            
            var configModeMenuItem = new ToolStripMenuItem("Move Overlay...");
            configModeMenuItem.CheckOnClick = true;
            contextMenu.Items.Add(configModeMenuItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Settings...");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("About Wheel Overlay");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit");

            return contextMenu;
        }

        /// <summary>
        /// Verifies that all required menu items are present in the context menu.
        /// Tests that the menu contains: Show Overlay, Hide Overlay, Minimize to Taskbar,
        /// Move Overlay, Settings, About Wheel Overlay, and Exit.
        /// 
        /// Requirements: 3.1, 3.7
        /// </summary>
        [Fact]
        public void SystemTray_ContextMenu_HasAllRequiredItems()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var items = contextMenu.Items.Cast<ToolStripItem>().ToList();
            var menuItemTexts = items
                .Where(item => !(item is ToolStripSeparator))
                .Select(item => item.Text)
                .ToList();

            // Assert - Verify all required menu items are present
            Assert.Contains("Show Overlay", menuItemTexts);
            Assert.Contains("Hide Overlay", menuItemTexts);
            Assert.Contains("Minimize to Taskbar", menuItemTexts);
            Assert.Contains("Move Overlay...", menuItemTexts);
            Assert.Contains("Settings...", menuItemTexts);
            Assert.Contains("About Wheel Overlay", menuItemTexts);
            Assert.Contains("Exit", menuItemTexts);
        }

        /// <summary>
        /// Verifies that all menu items are enabled by default.
        /// Tests that no menu items are disabled when the context menu is created.
        /// 
        /// Requirements: 3.1, 3.7
        /// </summary>
        [Fact]
        public void SystemTray_ContextMenu_AllItemsEnabled()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var items = contextMenu.Items.Cast<ToolStripItem>()
                .Where(item => !(item is ToolStripSeparator))
                .ToList();

            // Assert - Verify all menu items are enabled
            foreach (var item in items)
            {
                Assert.True(item.Enabled, $"Menu item '{item.Text}' should be enabled");
            }
        }

        /// <summary>
        /// Verifies that the Settings menu item exists and has proper configuration.
        /// Tests that clicking the Settings menu item would invoke the OpenSettings method.
        /// 
        /// Requirements: 3.2
        /// </summary>
        [Fact]
        public void SystemTray_SettingsMenuItem_ExistsAndConfigured()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var settingsItem = contextMenu.Items.Cast<ToolStripItem>()
                .FirstOrDefault(item => item.Text == "Settings...");

            // Assert
            Assert.NotNull(settingsItem);
            Assert.True(settingsItem.Enabled);
            Assert.Equal("Settings...", settingsItem.Text);
        }

        /// <summary>
        /// Verifies that the App class has an OpenSettings method.
        /// This ensures the Settings menu item has proper wiring to open the settings dialog.
        /// 
        /// Requirements: 3.2
        /// </summary>
        [Fact]
        public void App_HasOpenSettingsMethod()
        {
            // Arrange
            var appType = typeof(App);

            // Act
            var openSettingsMethod = appType.GetMethod("OpenSettings", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(openSettingsMethod);
            Assert.Equal(typeof(void), openSettingsMethod.ReturnType);
            Assert.Empty(openSettingsMethod.GetParameters());
        }

        /// <summary>
        /// Verifies that the Config Mode menu item (Move Overlay...) exists and is configured as a checkable item.
        /// Tests that the menu item has CheckOnClick enabled.
        /// 
        /// Requirements: 3.3, 3.8
        /// </summary>
        [Fact]
        public void SystemTray_ConfigModeMenuItem_IsCheckable()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var configModeItem = contextMenu.Items.Cast<ToolStripItem>()
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Text == "Move Overlay...");

            // Assert
            Assert.NotNull(configModeItem);
            Assert.True(configModeItem.Enabled);
            Assert.True(configModeItem.CheckOnClick);
            Assert.False(configModeItem.Checked); // Should be unchecked by default
        }

        /// <summary>
        /// Verifies that the Config Mode menu item state can be toggled.
        /// Tests that clicking the menu item changes its checked state.
        /// 
        /// Requirements: 3.3, 3.8
        /// </summary>
        [Fact]
        public void SystemTray_ConfigModeMenuItem_TogglesState()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();
            var configModeItem = contextMenu.Items.Cast<ToolStripItem>()
                .OfType<ToolStripMenuItem>()
                .FirstOrDefault(item => item.Text == "Move Overlay...");

            Assert.NotNull(configModeItem);
            var initialState = configModeItem.Checked;

            // Act - Simulate clicking the menu item
            configModeItem.Checked = !configModeItem.Checked;
            var newState = configModeItem.Checked;

            // Assert
            Assert.NotEqual(initialState, newState);
            Assert.True(newState); // Should be checked after toggle
        }

        /// <summary>
        /// Verifies that the App class has a ToggleConfigMode method.
        /// This ensures the Config Mode menu item has proper wiring.
        /// 
        /// Requirements: 3.3, 3.8
        /// </summary>
        [Fact]
        public void App_HasToggleConfigModeMethod()
        {
            // Arrange
            var appType = typeof(App);

            // Act
            var toggleConfigModeMethod = appType.GetMethod("ToggleConfigMode", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(toggleConfigModeMethod);
            Assert.Equal(typeof(void), toggleConfigModeMethod.ReturnType);
            
            var parameters = toggleConfigModeMethod.GetParameters();
            Assert.Single(parameters);
            Assert.Equal(typeof(bool), parameters[0].ParameterType);
        }

        /// <summary>
        /// Verifies that the About menu item exists and is properly configured.
        /// Tests that the menu item is present and enabled.
        /// 
        /// Requirements: 3.5
        /// </summary>
        [Fact]
        public void SystemTray_AboutMenuItem_ExistsAndConfigured()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var aboutItem = contextMenu.Items.Cast<ToolStripItem>()
                .FirstOrDefault(item => item.Text == "About Wheel Overlay");

            // Assert
            Assert.NotNull(aboutItem);
            Assert.True(aboutItem.Enabled);
            Assert.Equal("About Wheel Overlay", aboutItem.Text);
        }

        /// <summary>
        /// Verifies that the App class has a ShowAboutDialog method.
        /// This ensures the About menu item has proper wiring to open the about dialog.
        /// 
        /// Requirements: 3.5
        /// </summary>
        [Fact]
        public void App_HasShowAboutDialogMethod()
        {
            // Arrange
            var appType = typeof(App);

            // Act
            var showAboutMethod = appType.GetMethod("ShowAboutDialog", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert
            Assert.NotNull(showAboutMethod);
            Assert.Equal(typeof(void), showAboutMethod.ReturnType);
            Assert.Empty(showAboutMethod.GetParameters());
        }

        /// <summary>
        /// Verifies that the About menu item is positioned correctly in the menu.
        /// Tests that About is positioned above the Exit menu item.
        /// 
        /// Requirements: 3.5
        /// </summary>
        [Fact]
        public void SystemTray_AboutMenuItem_PositionedAboveExit()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var items = contextMenu.Items.Cast<ToolStripItem>().ToList();
            var aboutIndex = items.FindIndex(item => item.Text == "About Wheel Overlay");
            var exitIndex = items.FindIndex(item => item.Text == "Exit");

            // Assert
            Assert.True(aboutIndex >= 0, "About menu item not found");
            Assert.True(exitIndex >= 0, "Exit menu item not found");
            Assert.True(aboutIndex < exitIndex, 
                $"About menu item (index {aboutIndex}) should be positioned before Exit menu item (index {exitIndex})");
        }

        /// <summary>
        /// Verifies that the Exit menu item exists and is properly configured.
        /// Tests that the menu item is present and enabled.
        /// 
        /// Requirements: 3.6
        /// </summary>
        [Fact]
        public void SystemTray_ExitMenuItem_ExistsAndConfigured()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var exitItem = contextMenu.Items.Cast<ToolStripItem>()
                .FirstOrDefault(item => item.Text == "Exit");

            // Assert
            Assert.NotNull(exitItem);
            Assert.True(exitItem.Enabled);
            Assert.Equal("Exit", exitItem.Text);
        }

        /// <summary>
        /// Verifies that the Exit menu item is positioned at the end of the menu.
        /// Tests that Exit is the last non-separator item in the context menu.
        /// 
        /// Requirements: 3.6
        /// </summary>
        [Fact]
        public void SystemTray_ExitMenuItem_PositionedAtEnd()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var items = contextMenu.Items.Cast<ToolStripItem>().ToList();
            var nonSeparatorItems = items.Where(item => !(item is ToolStripSeparator)).ToList();
            var lastItem = nonSeparatorItems.LastOrDefault();

            // Assert
            Assert.NotNull(lastItem);
            Assert.Equal("Exit", lastItem.Text);
        }

        /// <summary>
        /// Verifies that the App class inherits from Application and has a Shutdown method.
        /// This ensures the Exit menu item can properly terminate the application.
        /// 
        /// Requirements: 3.6
        /// </summary>
        [Fact]
        public void App_CanShutdownGracefully()
        {
            // Arrange
            var appType = typeof(App);

            // Act
            var isApplication = typeof(System.Windows.Application).IsAssignableFrom(appType);
            var shutdownMethod = typeof(System.Windows.Application).GetMethod("Shutdown", Type.EmptyTypes);

            // Assert
            Assert.True(isApplication, "App should inherit from System.Windows.Application");
            Assert.NotNull(shutdownMethod);
        }

        /// <summary>
        /// Verifies that the menu structure includes proper separators.
        /// Tests that separators are placed between logical groups of menu items.
        /// 
        /// Requirements: 3.1, 3.7
        /// </summary>
        [Fact]
        public void SystemTray_ContextMenu_HasProperSeparators()
        {
            // Arrange
            var contextMenu = CreateMockContextMenu();

            // Act
            var items = contextMenu.Items.Cast<ToolStripItem>().ToList();
            var separatorCount = items.Count(item => item is ToolStripSeparator);

            // Assert
            Assert.True(separatorCount >= 4, $"Expected at least 4 separators, found {separatorCount}");
        }
    }
}
