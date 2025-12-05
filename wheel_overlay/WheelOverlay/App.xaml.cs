using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using WheelOverlay.Models;

namespace WheelOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;
        private ToolStripMenuItem? _configModeMenuItem;
        private ToolStripMenuItem? _minimizeMenuItem;

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
            
            _minimizeMenuItem = new ToolStripMenuItem("Minimize to Taskbar");
            _minimizeMenuItem.CheckOnClick = true;
            _minimizeMenuItem.Click += (s, args) => ToggleMinimize(_minimizeMenuItem.Checked);
            contextMenu.Items.Add(_minimizeMenuItem);
            
            _configModeMenuItem = new ToolStripMenuItem("Move Overlay...");
            _configModeMenuItem.CheckOnClick = true;
            _configModeMenuItem.Click += (s, args) => ToggleConfigMode(_configModeMenuItem.Checked);
            contextMenu.Items.Add(_configModeMenuItem);
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
            var settings = AppSettings.Load();
            var settingsWindow = new SettingsWindow(settings);
            settingsWindow.SettingsChanged += (s, e) =>
            {
                // Settings were applied, reload main window
                if (_mainWindow != null)
                {
                    _mainWindow.ApplySettings(settings);
                }
            };
            settingsWindow.Show();
        }

        private void ToggleMinimize(bool enabled)
        {
            if (_mainWindow != null)
            {
                if (enabled)
                {
                    _mainWindow.WindowState = WindowState.Minimized;
                }
                else
                {
                    _mainWindow.WindowState = WindowState.Normal;
                }
            }
        }

        private void ToggleConfigMode(bool enabled)
        {
            if (_mainWindow != null)
            {
                _mainWindow.ConfigMode = enabled;
            }
        }

        public void ClearConfigModeCheckmark()
        {
            if (_configModeMenuItem != null)
            {
                _configModeMenuItem.Checked = false;
            }
        }

        public void ClearMinimizeCheckmark()
        {
            if (_minimizeMenuItem != null)
            {
                _minimizeMenuItem.Checked = false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
