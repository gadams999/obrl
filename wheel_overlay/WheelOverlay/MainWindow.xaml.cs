using System;
using System.Windows;
using System.Windows.Interop;
using WheelOverlay.Services;

namespace WheelOverlay
{
    public partial class MainWindow : Window
    {
        private readonly InputService _inputService;
        private readonly string[] _displayItems = { "DASH", "TC2", "MAP", "FUEL", "BRGT", "VOL", "BOX", "DIFF" };
        private bool _configMode = false;

        public bool ConfigMode
        {
            get => _configMode;
            set
            {
                _configMode = value;
                ApplyConfigMode();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _inputService = new InputService();
            _inputService.RotaryPositionChanged += OnRotaryPositionChanged;
            
            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _inputService.Start();
            
            MakeWindowTransparent();
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
                // Config mode: visible, draggable window
                AllowsTransparency = false;
                Background = System.Windows.Media.Brushes.White;
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResizeWithGrip;
                ShowInTaskbar = true;
            }
            else
            {
                // Overlay mode: transparent, click-through
                AllowsTransparency = true;
                Background = System.Windows.Media.Brushes.Transparent;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = true;
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
            if (position >= 0 && position < _displayItems.Length)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = _displayItems[position];
                });
            }
        }
    }
}
