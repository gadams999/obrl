using System.Windows;
using System.Windows.Interop;
using WheelOverlay.Services;
using WheelOverlay.Models;

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

        public void ApplySettings(AppSettings settings)
        {
            // Update font size
            StatusText.FontSize = settings.FontSize;
            
            // Update text color
            StatusText.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.SelectedTextColor));
            
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
