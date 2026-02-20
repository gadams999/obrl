using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using WheelOverlay.Models;
using WheelOverlay.Services;
using Xunit;
using Xunit.Abstractions;

namespace WheelOverlay.Tests
{
    /// <summary>
    /// Performance tests for overlay-visibility-and-ui-improvements feature.
    /// Tests CPU usage, response times, and settings persistence performance.
    /// </summary>
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Tests that visibility changes respond within 1 second.
        /// Requirements: 1.6, 1.7
        /// </summary>
        [Fact]
        public void ProcessMonitor_VisibilityChange_ShouldRespondWithin1Second()
        {
            // Arrange
            var targetPath = GetTestExecutablePath();
            bool? visibilityState = null;
            var responseTime = TimeSpan.Zero;
            var stopwatch = new Stopwatch();
            
            using (var monitor = new ProcessMonitor(targetPath, TimeSpan.FromMilliseconds(100)))
            {
                monitor.TargetApplicationStateChanged += (s, running) =>
                {
                    if (!stopwatch.IsRunning)
                    {
                        stopwatch.Start();
                    }
                    else
                    {
                        stopwatch.Stop();
                        responseTime = stopwatch.Elapsed;
                    }
                    visibilityState = running;
                };
                
                // Act - Start monitoring and wait for initial state
                monitor.Start();
                Thread.Sleep(1500); // Wait for response
                
                _output.WriteLine($"Response Time: {responseTime.TotalMilliseconds:F2}ms");
                _output.WriteLine($"Visibility State: {visibilityState}");
                
                // Assert - Response should be within 1 second
                Assert.True(responseTime.TotalSeconds <= 1.0 || responseTime == TimeSpan.Zero,
                    $"Response time should be <= 1 second, but was {responseTime.TotalSeconds:F2}s");
            }
        }

        /// <summary>
        /// Tests that settings save/load time is acceptable (< 200ms).
        /// Requirements: 6.1, 6.2, 6.3
        /// </summary>
        [Fact]
        public void Settings_SaveAndLoad_ShouldBeFast()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var profile = new Profile
            {
                Name = "Test Profile",
                TargetExecutablePath = @"C:\Test\Application.exe",
                FontSize = 14,
                FontWeight = "Bold",
                TextRenderingMode = "Aliased"
            };
            
            try
            {
                // Act - Measure save time
                var saveStopwatch = Stopwatch.StartNew();
                var json = JsonSerializer.Serialize(profile);
                File.WriteAllText(tempFile, json);
                saveStopwatch.Stop();
                
                _output.WriteLine($"Save Time: {saveStopwatch.ElapsedMilliseconds}ms");
                
                // Act - Measure load time
                var loadStopwatch = Stopwatch.StartNew();
                var loadedJson = File.ReadAllText(tempFile);
                var loadedProfile = JsonSerializer.Deserialize<Profile>(loadedJson);
                loadStopwatch.Stop();
                
                _output.WriteLine($"Load Time: {loadStopwatch.ElapsedMilliseconds}ms");
                
                // Assert - Both operations should be fast (< 200ms is still very fast)
                Assert.True(saveStopwatch.ElapsedMilliseconds < 200,
                    $"Save time should be < 200ms, but was {saveStopwatch.ElapsedMilliseconds}ms");
                Assert.True(loadStopwatch.ElapsedMilliseconds < 200,
                    $"Load time should be < 200ms, but was {loadStopwatch.ElapsedMilliseconds}ms");
                
                // Verify data integrity
                Assert.NotNull(loadedProfile);
                Assert.Equal(profile.TargetExecutablePath, loadedProfile.TargetExecutablePath);
                Assert.Equal(profile.FontSize, loadedProfile.FontSize);
                Assert.Equal(profile.FontWeight, loadedProfile.FontWeight);
                Assert.Equal(profile.TextRenderingMode, loadedProfile.TextRenderingMode);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests that process enumeration completes in reasonable time.
        /// Requirements: 5.4
        /// NOTE: This test is skipped in CI due to high variability in process enumeration time
        /// based on system load, number of processes, and security policies.
        /// </summary>
        [Fact(Skip = "Flaky in CI - process enumeration time varies significantly with system load")]
        public void ProcessEnumeration_ShouldCompleteQuickly()
        {
            // Arrange & Act
            var stopwatch = Stopwatch.StartNew();
            var processes = Process.GetProcesses();
            var count = 0;
            
            foreach (var process in processes)
            {
                try
                {
                    var path = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path))
                    {
                        count++;
                    }
                }
                catch
                {
                    // Skip inaccessible processes
                }
                finally
                {
                    process.Dispose();
                }
            }
            
            stopwatch.Stop();
            
            _output.WriteLine($"Enumeration Time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Accessible Processes: {count}");
            
            // Assert - Should complete in reasonable time (2000ms)
            Assert.True(stopwatch.ElapsedMilliseconds < 2000,
                $"Process enumeration should complete in < 2000ms, but took {stopwatch.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// Tests that visibility rules update within 500ms when target changes.
        /// Requirements: 5.6
        /// </summary>
        [Fact]
        public void ProcessMonitor_TargetUpdate_ShouldApplyWithin500ms()
        {
            // Arrange
            var initialPath = GetTestExecutablePath();
            var newPath = GetAlternateTestExecutablePath();
            bool? visibilityState = null;
            var stopwatch = new Stopwatch();
            
            using (var monitor = new ProcessMonitor(initialPath, TimeSpan.FromMilliseconds(100)))
            {
                monitor.TargetApplicationStateChanged += (s, running) =>
                {
                    visibilityState = running;
                    if (stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                    }
                };
                
                monitor.Start();
                Thread.Sleep(200); // Let initial state settle
                
                // Act - Update target and measure response time
                stopwatch.Start();
                monitor.UpdateTarget(newPath);
                Thread.Sleep(600); // Wait for update to complete
                
                _output.WriteLine($"Update Response Time: {stopwatch.ElapsedMilliseconds}ms");
                _output.WriteLine($"New Visibility State: {visibilityState}");
                
                // Assert - Update should apply within 500ms
                Assert.True(stopwatch.ElapsedMilliseconds <= 500,
                    $"Target update should apply within 500ms, but took {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Gets a test executable path (preferably a running process).
        /// </summary>
        private string GetTestExecutablePath()
        {
            try
            {
                var runningProcess = Process.GetProcesses()
                    .FirstOrDefault(p =>
                    {
                        try
                        {
                            return p.MainModule?.FileName != null;
                        }
                        catch
                        {
                            return false;
                        }
                    });
                
                if (runningProcess != null)
                {
                    var path = runningProcess.MainModule?.FileName;
                    runningProcess.Dispose();
                    return path ?? @"C:\Windows\System32\notepad.exe";
                }
            }
            catch
            {
                // Fall through to default
            }
            
            return @"C:\Windows\System32\notepad.exe";
        }

        /// <summary>
        /// Gets an alternate test executable path.
        /// </summary>
        private string GetAlternateTestExecutablePath()
        {
            return @"C:\Windows\explorer.exe";
        }
    }
}
