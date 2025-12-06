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

        public App()
        {
            // Explicit constructor prevents hard crash in SingleFile/Release mode
            Services.LogService.Info("App constructor called.");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services.LogService.Info("Application identifying startup sequence...");

            // Global Exception Handling
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                Services.LogService.Error($"AppDomain Unhandled Exception", args.ExceptionObject as Exception ?? new Exception("Unknown"));
            };

            DispatcherUnhandledException += (s, args) =>
            {
                Services.LogService.Error($"Dispatcher Unhandled Exception", args.Exception);
                // args.Handled = true; // Optional: prevents crash, but maybe we want crash?
            };

            TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                Services.LogService.Error($"TaskScheduler Unobserved Exception", args.Exception);
            };

            try 
            {
                Services.LogService.Info("Initializing MainWindow...");
                // Create main window but don't show it yet
                _mainWindow = new MainWindow();
                Services.LogService.Info("MainWindow initialized.");

                // Create system tray icon
                _notifyIcon = new NotifyIcon
                {
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
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
                Services.LogService.Info("Startup sequence completed successfully.");
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Startup crashed!", ex);
                throw; // Rethrow to let the app die properly after logging
            }
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
