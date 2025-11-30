using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace WheelOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create main window but don't show it yet
            _mainWindow = new MainWindow();

            // Create system tray icon
            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Wheel Overlay"
            };

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show Overlay", null, (s, args) => ShowOverlay());
            contextMenu.Items.Add("Hide Overlay", null, (s, args) => HideOverlay());
            contextMenu.Items.Add("-");
            var configModeItem = new ToolStripMenuItem("Config Mode");
            configModeItem.CheckOnClick = true;
            configModeItem.Click += (s, args) => ToggleConfigMode(configModeItem.Checked);
            contextMenu.Items.Add(configModeItem);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Settings...", null, (s, args) => OpenSettings());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit", null, (s, args) => Shutdown());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, args) => ToggleOverlay();

            // Show overlay by default
            ShowOverlay();
        }

        private void ShowOverlay()
        {
            _mainWindow?.Show();
        }

        private void HideOverlay()
        {
            _mainWindow?.Hide();
        }

        private void ToggleOverlay()
        {
            if (_mainWindow?.IsVisible == true)
                HideOverlay();
            else
                ShowOverlay();
        }

        private void OpenSettings()
        {
            // TODO: Implement settings window
            System.Windows.MessageBox.Show("Settings window coming soon!", "Wheel Overlay");
        }

        private void ToggleConfigMode(bool enabled)
        {
            if (_mainWindow != null)
            {
                // Save current state
                var left = _mainWindow.Left;
                var top = _mainWindow.Top;
                var width = _mainWindow.Width;
                var height = _mainWindow.Height;
                var isVisible = _mainWindow.IsVisible;

                // Get the InputService before closing
                var inputService = _mainWindow.GetInputService();

                // Mark as recreating to prevent disposal
                _mainWindow.SetRecreating(true);

                // Close old window (but don't dispose InputService)
                _mainWindow.Close();

                // Create new window with shared InputService
                _mainWindow = new MainWindow(inputService);
                _mainWindow.ConfigMode = enabled;
                _mainWindow.Left = left;
                _mainWindow.Top = top;
                _mainWindow.Width = width;
                _mainWindow.Height = height;

                // Show if it was visible before
                if (isVisible)
                {
                    _mainWindow.Show();
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
