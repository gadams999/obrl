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
        private AboutWindow? _aboutWindow;
        private SettingsWindow? _settingsWindow;
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

            // Handle session ending (logout, shutdown, etc.)
            SessionEnding += App_SessionEnding;

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
                contextMenu.Items.Add("About Wheel Overlay", null, (s, args) => ShowAboutDialog());
                contextMenu.Items.Add("-");
                contextMenu.Items.Add("Exit", null, (s, args) => ExitApplication());

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, args) => ToggleOverlay();

                // Show overlay by default
                ShowOverlay();
                
                // Hook into window closing event to hide instead of close
                _mainWindow.Closing += MainWindow_Closing;
                
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
            
            // Reuse existing settings window if open
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }
            
            _settingsWindow = new SettingsWindow(settings);
            _settingsWindow.SettingsChanged += (s, e) =>
            {
                // Settings were applied, reload main window
                if (_mainWindow != null)
                {
                    _mainWindow.ApplySettings(settings);
                }
            };
            _settingsWindow.Closed += (s, e) => _settingsWindow = null;
            _settingsWindow.Show();
        }

        private void ShowAboutDialog()
        {
            var aboutWindow = new AboutWindow
            {
                Owner = _mainWindow
            };
            
            // Store reference to close it if needed during shutdown
            _aboutWindow = aboutWindow;
            aboutWindow.Closed += (s, e) => _aboutWindow = null;
            
            aboutWindow.ShowDialog();
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

        public void ExitApplication()
        {
            Services.LogService.Info("Exit requested");
            
            // Close context menu immediately to prevent it from blocking
            if (_notifyIcon?.ContextMenuStrip != null)
            {
                _notifyIcon.ContextMenuStrip.Close();
            }
            
            // Use BeginInvoke to defer shutdown until after the menu click event completes
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Services.LogService.Info("Beginning application shutdown");
                    Shutdown();
                }
                catch (Exception ex)
                {
                    Services.LogService.Error("Error during shutdown", ex);
                    // Force exit if normal shutdown fails
                    Environment.Exit(0);
                }
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services.LogService.Info("OnExit called");
            CleanupResources();
            base.OnExit(e);
        }

        private void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            Services.LogService.Info($"Session ending: {e.ReasonSessionEnding}");
            CleanupResources();
        }

        private void CleanupResources()
        {
            Services.LogService.Info("Cleaning up resources");
            
            // Close any open child windows first
            try
            {
                if (_settingsWindow != null)
                {
                    Services.LogService.Info("Closing Settings window");
                    _settingsWindow.Close();
                    _settingsWindow = null;
                }
                
                if (_aboutWindow != null)
                {
                    Services.LogService.Info("Closing About window");
                    _aboutWindow.Close();
                    _aboutWindow = null;
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error closing child windows", ex);
            }
            
            // Close main window
            try
            {
                if (_mainWindow != null)
                {
                    Services.LogService.Info("Closing main window");
                    // Remove the Closing event handler to prevent it from canceling the close
                    _mainWindow.Closing -= MainWindow_Closing;
                    _mainWindow.Close();
                    _mainWindow = null;
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error closing main window", ex);
            }
            
            // Then cleanup notify icon
            CleanupNotifyIcon();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // When user closes window from taskbar, exit the application
            Services.LogService.Info("MainWindow closing requested - initiating app shutdown");
            
            // Close any open child windows first
            try
            {
                if (_settingsWindow != null)
                {
                    Services.LogService.Info("Closing Settings window before shutdown");
                    _settingsWindow.Close();
                    _settingsWindow = null;
                }
                
                if (_aboutWindow != null)
                {
                    Services.LogService.Info("Closing About window before shutdown");
                    _aboutWindow.Close();
                    _aboutWindow = null;
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error closing child windows", ex);
            }
            
            // Cancel the close to prevent immediate window destruction
            e.Cancel = true;
            
            // Trigger app shutdown through the App class
            ((App)System.Windows.Application.Current).ExitApplication();
        }

        private void CleanupNotifyIcon()
        {
            if (_notifyIcon != null)
            {
                try
                {
                    Services.LogService.Info("Cleaning up NotifyIcon");
                    
                    // Remove event handlers to prevent callbacks during disposal
                    _notifyIcon.DoubleClick -= (s, args) => ToggleOverlay();
                    
                    // Hide icon first to ensure it's removed from system tray
                    _notifyIcon.Visible = false;
                    
                    // Dispose of context menu separately with a small delay
                    var contextMenu = _notifyIcon.ContextMenuStrip;
                    _notifyIcon.ContextMenuStrip = null;
                    
                    // Dispose of the icon itself
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                    
                    // Dispose context menu after icon is disposed
                    contextMenu?.Dispose();
                    
                    Services.LogService.Info("NotifyIcon cleanup complete");
                }
                catch (Exception ex)
                {
                    Services.LogService.Error("Error cleaning up NotifyIcon", ex);
                }
            }
        }
    }
}
