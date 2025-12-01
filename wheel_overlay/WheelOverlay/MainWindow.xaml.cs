using System;
using System.Windows;
using System.Windows.Interop;
using WheelOverlay.Services;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;

namespace WheelOverlay
{
    public partial class MainWindow : Window
    {
        private readonly InputService _inputService;
        private bool _configMode = false;
        private OverlayViewModel _viewModel;

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
            
            Loaded += MainWindow_Loaded;
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
            
            MakeWindowTransparent();
        }

        public void ApplySettings(AppSettings settings)
        {
            // Update ViewModel settings
            _viewModel.Settings = settings;

            // Update move overlay opacity if in config mode
            if (_configMode)
            {
                byte alpha = (byte)(settings.MoveOverlayOpacity * 255 / 100);
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(alpha, 128, 128, 128));
            }
        }



        private void MakeWindowTransparent()
        {
            if (!_configMode)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
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
        private const int WS_EX_TOOLWINDOW = 0x00000080; // Hides from Alt-Tab

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);


        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            _inputService.Stop();
            _inputService.Dispose();
        }

        private void OnRotaryPositionChanged(object? sender, int position)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.IsDeviceNotFound = false; // Device is connected
                _viewModel.CurrentPosition = position;
            });
        }
    }
}
