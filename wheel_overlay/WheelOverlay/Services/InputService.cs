using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortice.DirectInput;

namespace WheelOverlay.Services
{
    public class InputService : IDisposable
    {
        private readonly IDirectInput8 _directInput;
        private IDirectInputDevice8? _device;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _pollingTask;

        // 1-indexed buttons 58-65 map to 0-indexed 57-64
        private const int BASE_BUTTON_INDEX = 57; 
        private int _maxButtonIndex = 64; // Default for 8 positions (57-64)
        private string _targetDeviceName = "BavarianSimTec Alpha";
        private bool _deviceNotFoundEmitted = false;

        // Test mode properties
        private bool _testMode;
        private int _testModePosition = 0;
        private int _testModeMaxPosition = 7; // 0-indexed, default 8 positions

        public event EventHandler<int>? RotaryPositionChanged;
        public event EventHandler<string>? DeviceNotFound;
        public event EventHandler? DeviceConnected;

        public bool TestMode
        {
            get => _testMode;
            set
            {
                _testMode = value;
                if (_testMode)
                {
                    EnableKeyboardInput();
                }
                else
                {
                    DisableKeyboardInput();
                }
            }
        }

        public void ReattachKeyboardHandler()
        {
            if (_testMode)
            {
                EnableKeyboardInput();
            }
        }

        public InputService()
        {
            _directInput = DInput.DirectInput8Create();
        }

        /// <summary>
        /// Sets the active profile and configures the input button range based on PositionCount.
        /// </summary>
        /// <param name="profile">The profile to activate</param>
        public void SetActiveProfile(Models.Profile profile)
        {
            if (profile == null)
            {
                LogService.Error("SetActiveProfile called with null profile");
                return;
            }

            _maxButtonIndex = BASE_BUTTON_INDEX + profile.PositionCount - 1;
            _testModeMaxPosition = profile.PositionCount - 1;
            
            LogService.Info($"InputService configured for {profile.PositionCount} positions " +
                           $"(buttons {BASE_BUTTON_INDEX}-{_maxButtonIndex})");
        }

        public void Start(string deviceName)
        {
            _targetDeviceName = deviceName;
            _deviceNotFoundEmitted = false;

            // Check for test mode flag
            var args = Environment.GetCommandLineArgs();
            if (args.Contains("--test-mode") || args.Contains("/test"))
            {
                TestMode = true;
                LogService.Info("Test mode enabled - using keyboard input");
                DeviceConnected?.Invoke(this, EventArgs.Empty);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            _pollingTask = Task.Run(() => PollLoop(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _pollingTask?.Wait();
            _device?.Unacquire();
        }

        private void PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_device == null)
                    {
                        FindDevice();
                        Thread.Sleep(1000); // Wait before retrying if not found
                        continue;
                    }

                    _device.Poll();
                    var state = _device.GetCurrentJoystickState();

                    // Check buttons in the configured range [BASE_BUTTON_INDEX, _maxButtonIndex]
                    bool[] buttons = state.Buttons;
                    
                    // Debug: Log any pressed button to verify mapping
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i])
                        {
                            Debug.WriteLine($"[InputService] Button {i} is PRESSED");
                        }
                    }

                    for (int i = BASE_BUTTON_INDEX; i <= _maxButtonIndex; i++)
                    {
                        if (i < buttons.Length && buttons[i])
                        {
                            // Found the pressed button
                            // Map index 57 -> 0, 58 -> 1, etc.
                            int position = i - BASE_BUTTON_INDEX;
                            Debug.WriteLine($"[InputService] Rotary Match! Button {i} -> Position {position}");
                            RotaryPositionChanged?.Invoke(this, position);
                            break; 
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Device lost or error, try to re-acquire
                    Debug.WriteLine($"[InputService] Error during polling: {ex.Message}");
                    _device?.Unacquire();
                    _device = null;
                }

                Thread.Sleep(16); // ~60Hz
            }
        }

        private void FindDevice()
        {
            Debug.WriteLine("[InputService] Scanning for devices...");
            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            
            bool deviceFound = false;
            foreach (var deviceInstance in devices)
            {
                Debug.WriteLine($"[InputService] Found device: '{deviceInstance.ProductName}' (GUID: {deviceInstance.InstanceGuid})");
                if (deviceInstance.ProductName.Contains(_targetDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[InputService] MATCH FOUND! Attempting to acquire '{deviceInstance.ProductName}'...");
                    _device = _directInput.CreateDevice(deviceInstance.InstanceGuid);
                    if (_device != null)
                    {
                        _device.SetDataFormat<RawJoystickState>();
                        _device.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.Background | CooperativeLevel.NonExclusive);
                        _device.Acquire();
                        Debug.WriteLine("[InputService] Device acquired successfully.");
                        deviceFound = true;
                        _deviceNotFoundEmitted = false; // Reset flag when device is found
                        DeviceConnected?.Invoke(this, EventArgs.Empty); // Notify that device connected
                        break;
                    }
                }
            }
            
            // Emit DeviceNotFound event only once
            if (!deviceFound && !_deviceNotFoundEmitted)
            {
                Debug.WriteLine($"[InputService] Device '{_targetDeviceName}' not found.");
                DeviceNotFound?.Invoke(this, _targetDeviceName);
                _deviceNotFoundEmitted = true;
            }
        }

        private void EnableKeyboardInput()
        {
            // Register keyboard event handler on the main window
            if (System.Windows.Application.Current?.MainWindow != null)
            {
                // Remove handler first to avoid duplicate registration
                System.Windows.Application.Current.MainWindow.KeyDown -= OnTestModeKeyDown;
                System.Windows.Application.Current.MainWindow.KeyDown += OnTestModeKeyDown;
                
                // Ensure the window can receive keyboard input
                System.Windows.Application.Current.MainWindow.Focusable = true;
                System.Windows.Application.Current.MainWindow.Focus();
            }
        }

        private void DisableKeyboardInput()
        {
            // Unregister keyboard event handler
            if (System.Windows.Application.Current?.MainWindow != null)
            {
                System.Windows.Application.Current.MainWindow.KeyDown -= OnTestModeKeyDown;
            }
        }

        private void OnTestModeKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_testMode) return;

            switch (e.Key)
            {
                case System.Windows.Input.Key.Left:
                    _testModePosition--;
                    if (_testModePosition < 0)
                        _testModePosition = _testModeMaxPosition;
                    RaiseRotaryPositionChanged(_testModePosition);
                    e.Handled = true;
                    break;

                case System.Windows.Input.Key.Right:
                    _testModePosition++;
                    if (_testModePosition > _testModeMaxPosition)
                        _testModePosition = 0;
                    RaiseRotaryPositionChanged(_testModePosition);
                    e.Handled = true;
                    break;
            }
        }

        private void RaiseRotaryPositionChanged(int position)
        {
            Debug.WriteLine($"[InputService] Test mode position changed to {position}");
            RotaryPositionChanged?.Invoke(this, position);
        }

        public void Dispose()
        {
            Stop();
            _device?.Dispose();
            _directInput.Dispose();
        }
    }
}
