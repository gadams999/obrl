using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WheelOverlay.Services;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using WheelOverlay.Views;

namespace WheelOverlay
{
    public partial class MainWindow : Window
    {
        private readonly InputService _inputService;
        private bool _configMode = false;
        private OverlayViewModel _viewModel;
        private ProcessMonitor? _processMonitor;
        private bool _shouldBeVisible = true;

        private double _originalLeft;
        private double _originalTop;

        public bool ConfigMode
        {
            get => _configMode;
            set
            {
                if (_configMode != value)
                {
                    _configMode = value;
                    if (_configMode)
                    {
                        // Store original position when entering config mode
                        _originalLeft = Left;
                        _originalTop = Top;
                    }
                    else
                    {
                        // Save new position when exiting config mode
                        var settings = AppSettings.Load();
                        settings.WindowLeft = Left;
                        settings.WindowTop = Top;
                        settings.Save();
                    }
                    ApplyConfigMode();
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize ViewModel with settings
            var settings = AppSettings.Load();
            _viewModel = new OverlayViewModel(settings);
            _viewModel.IsDeviceNotFound = true; // Start with "not found" until device connects
            DataContext = _viewModel;

            // Restore saved position
            Left = settings.WindowLeft;
            Top = settings.WindowTop;

            _inputService = new InputService();
            _inputService.RotaryPositionChanged += OnRotaryPositionChanged;
            _inputService.DeviceNotFound += OnDeviceNotFound;
            _inputService.DeviceConnected += OnDeviceConnected;
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;
            KeyDown += MainWindow_KeyDown;
            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            // If window is restored from minimized state, uncheck the minimize menu item
            if (WindowState == WindowState.Normal)
            {
                ((App)System.Windows.Application.Current).ClearMinimizeCheckmark();
            }
        }

        private void OnDeviceConnected(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.IsDeviceNotFound = false;
            });
        }

        private void OnDeviceNotFound(object? sender, string deviceName)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.IsDeviceNotFound = true;
            });
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_configMode)
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    // Accept new position
                    ConfigMode = false;
                    ((App)System.Windows.Application.Current).ClearConfigModeCheckmark();
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    // Cancel move, restore position
                    Left = _originalLeft;
                    Top = _originalTop;
                    ConfigMode = false;
                    ((App)System.Windows.Application.Current).ClearConfigModeCheckmark();
                }
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = AppSettings.Load();
            _inputService.Start(settings.SelectedDeviceName);
            
            // Set test mode indicator on ViewModel
            _viewModel.IsTestMode = _inputService.TestMode;
            
            MakeWindowTransparent();
            
            // Skip process monitoring in test mode - overlay should always be visible
            if (_inputService.TestMode)
            {
                _shouldBeVisible = true;
                Services.LogService.Info("Test mode: skipping process monitoring, overlay always visible");
                return;
            }
            
            // Initialize process monitoring for conditional visibility
            InitializeProcessMonitoring();
        }

        public void ApplySettings(AppSettings settings)
        {
            // Reload settings from disk to get latest changes
            var latestSettings = AppSettings.Load();
            
            // Update ViewModel settings
            _viewModel.Settings = latestSettings;

            // Update move overlay opacity if in config mode
            if (_configMode)
            {
                byte alpha = (byte)(latestSettings.MoveOverlayOpacity * 255 / 100);
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(alpha, 128, 128, 128));
            }

            // Re-attach keyboard handler in case it was lost (for test mode)
            _inputService.ReattachKeyboardHandler();
            
            // Update process monitoring when profile changes
            OnProfileChanged();
        }

        /// <summary>
        /// Initializes process monitoring for conditional visibility based on target executable.
        /// </summary>
        private void InitializeProcessMonitoring()
        {
            try
            {
                var targetExe = _viewModel.Settings?.ActiveProfile?.TargetExecutablePath;
                _processMonitor = new ProcessMonitor(targetExe, TimeSpan.FromSeconds(1));
                _processMonitor.TargetApplicationStateChanged += OnTargetApplicationStateChanged;
                _processMonitor.Start();
                
                Services.LogService.Info($"Process monitoring initialized with target: {targetExe ?? "(none)"}");
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Failed to initialize process monitoring", ex);
                // Fail safe - default to always visible
                _shouldBeVisible = true;
                _processMonitor = null;
            }
        }

        /// <summary>
        /// Called when the target application state changes (started or stopped).
        /// </summary>
        private void OnTargetApplicationStateChanged(object? sender, bool isRunning)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    _shouldBeVisible = isRunning;
                    UpdateWindowVisibility();
                });
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error handling target application state change", ex);
                // Fail safe - keep current visibility state
            }
        }

        /// <summary>
        /// Updates window visibility based on the current visibility state.
        /// </summary>
        private void UpdateWindowVisibility()
        {
            try
            {
                if (_shouldBeVisible)
                {
                    Show();
                    if (WindowState == WindowState.Minimized)
                        WindowState = WindowState.Normal;
                }
                else
                {
                    Hide();
                }
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error updating window visibility", ex);
                // Don't throw - window state errors shouldn't crash the app
            }
        }

        /// <summary>
        /// Called when the active profile changes. Updates process monitoring with new target executable.
        /// </summary>
        public void OnProfileChanged()
        {
            try
            {
                var targetExe = _viewModel.Settings?.ActiveProfile?.TargetExecutablePath;
                _processMonitor?.UpdateTarget(targetExe);
                Services.LogService.Info($"Profile changed, target executable updated to: {targetExe ?? "(none)"}");
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error updating process monitor target", ex);
                // Don't throw - continue with current monitoring state
            }
        }



        private void MakeWindowTransparent()
        {
            if (!_configMode)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                // Only use WS_EX_TRANSPARENT for click-through, not WS_EX_TOOLWINDOW
                // WS_EX_TOOLWINDOW prevents window capture tools like OpenKneeboard from detecting the window
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            }
        }

        private void ApplyConfigMode()
        {
            if (_configMode)
            {
                // Config mode: Make window visible and interactive
                // Semi-transparent gray background (80% opacity)
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(204, 128, 128, 128));
                
                // Show border
                ConfigBorder.BorderThickness = new Thickness(2);
                ConfigBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                
                // Remove click-through
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                }
                
                // Make window draggable by handling MouseDown
                this.MouseDown += Window_MouseDown;
            }
            else
            {
                // Overlay mode: transparent, click-through
                Background = System.Windows.Media.Brushes.Transparent;
                
                // Hide border
                ConfigBorder.BorderThickness = new Thickness(0);
                
                // Re-apply click-through
                MakeWindowTransparent();
                
                // Remove drag handler
                this.MouseDown -= Window_MouseDown;
            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && _configMode)
            {
                this.DragMove();
            }
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);


        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // When user closes window from taskbar, hide it instead of closing
            // This prevents the window from being destroyed while app is still running
            Services.LogService.Info("MainWindow closing requested - hiding window");
            e.Cancel = true;
            Hide();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            try
            {
                Services.LogService.Info("MainWindow closed");
                
                // Dispose process monitor
                _processMonitor?.Dispose();
                
                // Stop and dispose input service
                _inputService.Stop();
                _inputService.Dispose();
            }
            catch (Exception ex)
            {
                Services.LogService.Error("Error disposing services", ex);
            }
        }

        private void OnRotaryPositionChanged(object? sender, int position)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.IsDeviceNotFound = false; // Device is connected
                
                // Detect layout type and trigger animation for Single layout
                if (_viewModel.Settings?.ActiveProfile?.Layout == DisplayLayout.Single)
                {
                    // Find the SingleTextLayout control
                    var singleTextLayout = FindVisualChild<SingleTextLayout>(this);
                    if (singleTextLayout != null)
                    {
                        singleTextLayout.OnPositionChanged(position, _viewModel);
                    }
                }
                
                _viewModel.CurrentPosition = position;
            });
        }
        
        /// <summary>
        /// Finds a child control of a specific type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of child to find.</typeparam>
        /// <param name="parent">The parent element to search from.</param>
        /// <returns>The first child of the specified type, or null if not found.</returns>
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            
            return null;
        }
    }
}
