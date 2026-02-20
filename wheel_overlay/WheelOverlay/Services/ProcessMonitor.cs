using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

namespace WheelOverlay.Services
{
    /// <summary>
    /// Monitors running processes to detect when a target executable is running.
    /// Uses WMI Event Watchers for instant, efficient process detection.
    /// </summary>
    public class ProcessMonitor : IDisposable
    {
        private ManagementEventWatcher? _processStartWatcher;
        private ManagementEventWatcher? _processStopWatcher;
        private string? _targetExecutablePath;
        private string? _targetFileName;
        private bool _targetIsRunning;
        private bool _disposed;
        private readonly object _lock = new object();

        /// <summary>
        /// Event raised when the target application state changes (started or stopped).
        /// The boolean parameter indicates whether the target is now running (true) or stopped (false).
        /// </summary>
        public event EventHandler<bool>? TargetApplicationStateChanged;

        /// <summary>
        /// Creates a new ProcessMonitor instance.
        /// </summary>
        /// <param name="targetExecutablePath">The full path to the executable to monitor, or null for always-visible behavior.</param>
        /// <param name="pollInterval">Ignored - kept for API compatibility.</param>
        public ProcessMonitor(string? targetExecutablePath, TimeSpan pollInterval)
        {
            _targetExecutablePath = targetExecutablePath;
            _targetFileName = string.IsNullOrEmpty(targetExecutablePath) 
                ? null 
                : Path.GetFileName(targetExecutablePath);
            _targetIsRunning = false;
        }

        /// <summary>
        /// Starts monitoring for the target executable using WMI event watchers.
        /// </summary>
        public void Start()
        {
            if (string.IsNullOrEmpty(_targetExecutablePath))
            {
                LogService.Info("ProcessMonitor.Start: No target configured, always visible");
                TargetApplicationStateChanged?.Invoke(this, true);
                return;
            }

            try
            {
                // Check if target is already running
                bool isRunning = IsExecutableRunning(_targetExecutablePath);
                _targetIsRunning = isRunning;
                LogService.Info($"ProcessMonitor.Start: Initial check, isRunning={isRunning}, path={_targetExecutablePath}");
                TargetApplicationStateChanged?.Invoke(this, isRunning);

                // Set up WMI event watchers for process start and stop
                SetupWmiWatchers();
                
                LogService.Info("ProcessMonitor.Start: WMI event watchers created successfully");
            }
            catch (Exception ex)
            {
                LogService.Error("ProcessMonitor.Start: Failed to setup WMI watchers", ex);
                // Fall back to always visible if WMI setup fails
                TargetApplicationStateChanged?.Invoke(this, true);
            }
        }

        /// <summary>
        /// Sets up WMI event watchers for process creation and deletion.
        /// </summary>
        private void SetupWmiWatchers()
        {
            // WQL query to watch for process creation
            var startQuery = new WqlEventQuery("__InstanceCreationEvent",
                TimeSpan.FromSeconds(1),
                "TargetInstance ISA 'Win32_Process'");

            _processStartWatcher = new ManagementEventWatcher(startQuery);
            _processStartWatcher.EventArrived += OnProcessStarted;
            _processStartWatcher.Start();

            // WQL query to watch for process deletion
            var stopQuery = new WqlEventQuery("__InstanceDeletionEvent",
                TimeSpan.FromSeconds(1),
                "TargetInstance ISA 'Win32_Process'");

            _processStopWatcher = new ManagementEventWatcher(stopQuery);
            _processStopWatcher.EventArrived += OnProcessStopped;
            _processStopWatcher.Start();

            LogService.Info("ProcessMonitor: WMI event watchers started");
        }

        /// <summary>
        /// Called when a process starts.
        /// </summary>
        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var processName = targetInstance["Name"]?.ToString();
                var executablePath = targetInstance["ExecutablePath"]?.ToString();

                if (string.IsNullOrEmpty(processName))
                    return;

                // Check if this is our target process
                bool isMatch = false;

                // First try exact path match
                if (!string.IsNullOrEmpty(executablePath) && 
                    string.Equals(executablePath, _targetExecutablePath, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    LogService.Info($"ProcessMonitor: Process started - EXACT MATCH! Name: {processName}, Path: {executablePath}");
                }
                // Fallback to filename match
                else if (!string.IsNullOrEmpty(_targetFileName) &&
                         string.Equals(processName, _targetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    LogService.Info($"ProcessMonitor: Process started - FILENAME MATCH! Name: {processName}, Path: {executablePath ?? "N/A"}");
                }

                if (isMatch)
                {
                    lock (_lock)
                    {
                        if (!_targetIsRunning)
                        {
                            _targetIsRunning = true;
                            LogService.Info("ProcessMonitor: Target application started, firing event with True");
                            TargetApplicationStateChanged?.Invoke(this, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("ProcessMonitor: Error in OnProcessStarted", ex);
            }
        }

        /// <summary>
        /// Called when a process stops.
        /// </summary>
        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            try
            {
                var targetInstance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                var processName = targetInstance["Name"]?.ToString();
                var executablePath = targetInstance["ExecutablePath"]?.ToString();

                if (string.IsNullOrEmpty(processName))
                    return;

                // Check if this is our target process
                bool isMatch = false;

                // First try exact path match
                if (!string.IsNullOrEmpty(executablePath) && 
                    string.Equals(executablePath, _targetExecutablePath, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    LogService.Info($"ProcessMonitor: Process stopped - EXACT MATCH! Name: {processName}, Path: {executablePath}");
                }
                // Fallback to filename match
                else if (!string.IsNullOrEmpty(_targetFileName) &&
                         string.Equals(processName, _targetFileName, StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = true;
                    LogService.Info($"ProcessMonitor: Process stopped - FILENAME MATCH! Name: {processName}, Path: {executablePath ?? "N/A"}");
                }

                if (isMatch)
                {
                    lock (_lock)
                    {
                        // Check if any other instances are still running
                        bool stillRunning = IsExecutableRunning(_targetExecutablePath);
                        
                        if (_targetIsRunning && !stillRunning)
                        {
                            _targetIsRunning = false;
                            LogService.Info("ProcessMonitor: Target application stopped (no more instances), firing event with False");
                            TargetApplicationStateChanged?.Invoke(this, false);
                        }
                        else if (stillRunning)
                        {
                            LogService.Info("ProcessMonitor: Target application instance stopped, but other instances still running");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Error("ProcessMonitor: Error in OnProcessStopped", ex);
            }
        }

        /// <summary>
        /// Stops monitoring for the target executable.
        /// </summary>
        public void Stop()
        {
            try
            {
                _processStartWatcher?.Stop();
                _processStopWatcher?.Stop();
                LogService.Info("ProcessMonitor: WMI event watchers stopped");
            }
            catch (Exception ex)
            {
                LogService.Error("ProcessMonitor: Error stopping watchers", ex);
            }
        }

        /// <summary>
        /// Updates the target executable path and restarts monitoring.
        /// </summary>
        /// <param name="targetExecutablePath">The new target executable path, or null to disable monitoring.</param>
        public void UpdateTarget(string? targetExecutablePath)
        {
            LogService.Info($"ProcessMonitor.UpdateTarget: Updating target from '{_targetExecutablePath}' to '{targetExecutablePath}'");
            
            // Stop and dispose existing watchers
            Stop();
            _processStartWatcher?.Dispose();
            _processStopWatcher?.Dispose();
            _processStartWatcher = null;
            _processStopWatcher = null;
            
            _targetExecutablePath = targetExecutablePath;
            _targetFileName = string.IsNullOrEmpty(targetExecutablePath) 
                ? null 
                : Path.GetFileName(targetExecutablePath);
            _targetIsRunning = false;
            
            if (string.IsNullOrEmpty(targetExecutablePath))
            {
                LogService.Info("ProcessMonitor.UpdateTarget: No target, always visible");
                TargetApplicationStateChanged?.Invoke(this, true);
            }
            else
            {
                Start();
            }
        }

        /// <summary>
        /// Checks if a process is currently running from the specified executable path.
        /// Used for initial state check and to verify no other instances are running.
        /// </summary>
        private bool IsExecutableRunning(string? executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                return true;
                
            try
            {
                var targetFileName = Path.GetFileName(executablePath);
                var processes = Process.GetProcesses();
                
                foreach (var process in processes)
                {
                    try
                    {
                        var processPath = process.MainModule?.FileName;
                        
                        if (processPath != null)
                        {
                            // Exact path match
                            if (string.Equals(processPath, executablePath, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                            
                            // Filename match (handles Windows redirects)
                            var processFileName = Path.GetFileName(processPath);
                            if (string.Equals(processFileName, targetFileName, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Disposes the ProcessMonitor and stops the watchers.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _processStartWatcher?.Dispose();
                _processStopWatcher?.Dispose();
                _processStartWatcher = null;
                _processStopWatcher = null;
                _disposed = true;
                LogService.Info("ProcessMonitor: Disposed");
            }
        }
    }
}
