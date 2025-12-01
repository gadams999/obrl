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
        private const int ButtonStart = 57; 
        private const int ButtonEnd = 64;
        private string _targetDeviceName = "BavarianSimTec Alpha";
        private bool _deviceNotFoundEmitted = false;

        public event EventHandler<int>? RotaryPositionChanged;
        public event EventHandler<string>? DeviceNotFound;
        public event EventHandler? DeviceConnected;

        public InputService()
        {
            _directInput = DInput.DirectInput8Create();
        }

        public void Start(string deviceName)
        {
            _targetDeviceName = deviceName;
            _deviceNotFoundEmitted = false;
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

                    // Check buttons 58-65 (indices 57-64)
                    bool[] buttons = state.Buttons;
                    
                    // Debug: Log any pressed button to verify mapping
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i])
                        {
                            Debug.WriteLine($"[InputService] Button {i} is PRESSED");
                        }
                    }

                    for (int i = ButtonStart; i <= ButtonEnd; i++)
                    {
                        if (i < buttons.Length && buttons[i])
                        {
                            // Found the pressed button
                            // Map index 57 -> 0, 58 -> 1, etc.
                            int position = i - ButtonStart;
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

        public void Dispose()
        {
            Stop();
            _device?.Dispose();
            _directInput.Dispose();
        }
    }
}
