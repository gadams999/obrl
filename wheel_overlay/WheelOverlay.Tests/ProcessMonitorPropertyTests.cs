using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Services;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ProcessMonitorPropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 2: Overlay Visibility Matches Executable State
        // Validates: Requirements 1.4
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_OverlayVisibilityMatchesExecutableState()
        {
            return Prop.ForAll(
                Arb.From(Gen.Elements(GetTestExecutablePaths())),
                exePath =>
                {
                    // Arrange - Check if any process is actually running from this path
                    var isActuallyRunning = IsProcessRunning(exePath);
                    
                    bool? visibilityState = null;
                    var monitor = new ProcessMonitor(exePath, TimeSpan.FromMilliseconds(100));
                    monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
                    
                    // Act - Start monitoring and wait for initial check
                    monitor.Start();
                    Thread.Sleep(200); // Wait for at least one check cycle
                    
                    // Cleanup
                    monitor.Dispose();
                    
                    // Assert - Visibility state should match actual running state
                    return (visibilityState == isActuallyRunning)
                        .Label($"Expected visibility state {isActuallyRunning} for path '{exePath}', but got {visibilityState}");
                });
        }

        // Feature: overlay-visibility-and-ui-improvements, Property 12: Case-Insensitive Path Comparison
        // Validates: Requirements 5.7
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_CaseInsensitivePathComparison()
        {
            return Prop.ForAll(
                Arb.From(Gen.Elements(GetRunningExecutablePaths())),
                exePath =>
                {
                    if (string.IsNullOrEmpty(exePath))
                        return true.ToProperty();
                    
                    // Arrange - Create variations of the path with different casing
                    var lowerPath = exePath.ToLower();
                    var upperPath = exePath.ToUpper();
                    
                    bool? lowerResult = null;
                    bool? upperResult = null;
                    
                    var lowerMonitor = new ProcessMonitor(lowerPath, TimeSpan.FromMilliseconds(100));
                    lowerMonitor.TargetApplicationStateChanged += (s, running) => lowerResult = running;
                    
                    var upperMonitor = new ProcessMonitor(upperPath, TimeSpan.FromMilliseconds(100));
                    upperMonitor.TargetApplicationStateChanged += (s, running) => upperResult = running;
                    
                    // Act - Start both monitors and wait for checks
                    lowerMonitor.Start();
                    upperMonitor.Start();
                    Thread.Sleep(200);
                    
                    // Cleanup
                    lowerMonitor.Dispose();
                    upperMonitor.Dispose();
                    
                    // Assert - Both should return the same result regardless of casing
                    return (lowerResult == upperResult)
                        .Label($"Path comparison should be case-insensitive. Lower case result: {lowerResult}, Upper case result: {upperResult} for path '{exePath}'");
                });
        }

        /// <summary>
        /// Gets a list of test executable paths including currently running processes
        /// and some non-existent paths for testing.
        /// </summary>
        private static string[] GetTestExecutablePaths()
        {
            var runningPaths = GetRunningExecutablePaths().Take(5).ToList();
            
            // Add some non-existent paths for testing
            runningPaths.Add(@"C:\NonExistent\Application.exe");
            runningPaths.Add(@"C:\Fake\Program.exe");
            runningPaths.Add(@"D:\NotReal\Test.exe");
            
            return runningPaths.ToArray();
        }

        /// <summary>
        /// Gets paths of currently running executables.
        /// </summary>
        private static string[] GetRunningExecutablePaths()
        {
            try
            {
                return Process.GetProcesses()
                    .Select(p =>
                    {
                        try
                        {
                            return p.MainModule?.FileName;
                        }
                        catch
                        {
                            return null;
                        }
                        finally
                        {
                            p.Dispose();
                        }
                    })
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct()
                    .Take(10)
                    .ToArray()!;
            }
            catch
            {
                // Fallback to some common Windows executables
                return new[]
                {
                    @"C:\Windows\System32\notepad.exe",
                    @"C:\Windows\explorer.exe"
                };
            }
        }

        /// <summary>
        /// Checks if a process is currently running from the specified path.
        /// </summary>
        private static bool IsProcessRunning(string? executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                return true;
                
            try
            {
                return Process.GetProcesses()
                    .Any(p =>
                    {
                        try
                        {
                            var processPath = p.MainModule?.FileName;
                            return processPath != null &&
                                   string.Equals(processPath, executablePath,
                                       StringComparison.OrdinalIgnoreCase);
                        }
                        catch
                        {
                            return false;
                        }
                        finally
                        {
                            p.Dispose();
                        }
                    });
            }
            catch
            {
                return false;
            }
        }
    }
}
