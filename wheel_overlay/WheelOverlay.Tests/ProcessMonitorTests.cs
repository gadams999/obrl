using System;
using System.Threading;
using WheelOverlay.Services;
using Xunit;

namespace WheelOverlay.Tests
{
    public class ProcessMonitorTests
    {
        [Fact]
        public void ProcessMonitor_WithNullPath_ReturnsAlwaysVisible()
        {
            // Arrange
            bool? visibilityState = null;
            var monitor = new ProcessMonitor(null, TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
            
            // Act
            monitor.Start();
            Thread.Sleep(200);
            
            // Assert
            Assert.True(visibilityState, "Null path should result in always-visible behavior (true)");
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_WithEmptyPath_ReturnsAlwaysVisible()
        {
            // Arrange
            bool? visibilityState = null;
            var monitor = new ProcessMonitor(string.Empty, TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
            
            // Act
            monitor.Start();
            Thread.Sleep(200);
            
            // Assert
            Assert.True(visibilityState, "Empty path should result in always-visible behavior (true)");
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_WithNonExistentPath_ReturnsFalse()
        {
            // Arrange
            bool? visibilityState = null;
            var nonExistentPath = @"C:\NonExistent\FakeApplication.exe";
            var monitor = new ProcessMonitor(nonExistentPath, TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
            
            // Act
            monitor.Start();
            Thread.Sleep(200);
            
            // Assert
            Assert.False(visibilityState, "Non-existent executable path should return false");
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_UpdateTarget_TriggersStateChange()
        {
            // Arrange
            int eventCount = 0;
            bool? lastState = null;
            var monitor = new ProcessMonitor(@"C:\Initial\Path.exe", TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) =>
            {
                eventCount++;
                lastState = running;
            };
            
            monitor.Start();
            Thread.Sleep(200);
            
            // Act - Update to null (always visible)
            monitor.UpdateTarget(null);
            Thread.Sleep(100);
            
            // Assert
            Assert.True(eventCount >= 2, "Should have received at least 2 state change events");
            Assert.True(lastState, "After updating to null, should be always visible (true)");
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_Stop_StopsMonitoring()
        {
            // Arrange
            int eventCount = 0;
            var monitor = new ProcessMonitor(@"C:\Test\Path.exe", TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => eventCount++;
            
            monitor.Start();
            Thread.Sleep(200);
            var countAfterStart = eventCount;
            
            // Act
            monitor.Stop();
            Thread.Sleep(300); // Wait longer than poll interval
            
            // Assert
            Assert.Equal(countAfterStart, eventCount);
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_Dispose_StopsTimer()
        {
            // Arrange
            int eventCount = 0;
            var monitor = new ProcessMonitor(@"C:\Test\Path.exe", TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => eventCount++;
            
            monitor.Start();
            Thread.Sleep(200);
            var countBeforeDispose = eventCount;
            
            // Act
            monitor.Dispose();
            Thread.Sleep(300);
            
            // Assert
            Assert.Equal(countBeforeDispose, eventCount);
        }

        [Fact]
        public void ProcessMonitor_WithInvalidPathCharacters_HandlesGracefully()
        {
            // Arrange
            bool? visibilityState = null;
            var invalidPath = @"C:\Invalid<>Path|?.exe";
            var monitor = new ProcessMonitor(invalidPath, TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
            
            // Act - Should not throw exception
            monitor.Start();
            Thread.Sleep(200);
            
            // Assert
            Assert.False(visibilityState, "Invalid path should return false without throwing");
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_MultipleDispose_DoesNotThrow()
        {
            // Arrange
            var monitor = new ProcessMonitor(@"C:\Test\Path.exe", TimeSpan.FromMilliseconds(100));
            
            // Act & Assert - Should not throw
            monitor.Dispose();
            monitor.Dispose();
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_StartWithoutTarget_TriggersAlwaysVisible()
        {
            // Arrange
            bool? visibilityState = null;
            var monitor = new ProcessMonitor(null, TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) => visibilityState = running;
            
            // Act
            monitor.Start();
            Thread.Sleep(100);
            
            // Assert - Should receive one event indicating always visible (true)
            Assert.True(visibilityState, "Null target should trigger always-visible state (true)");
            
            // Cleanup
            monitor.Dispose();
        }

        [Fact]
        public void ProcessMonitor_DetectsAlreadyRunningProcess()
        {
            // Arrange - Use a process that's always running (explorer.exe on Windows)
            var explorerPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "explorer.exe");
            
            bool? initialState = null;
            var monitor = new ProcessMonitor(explorerPath, TimeSpan.FromMilliseconds(100));
            monitor.TargetApplicationStateChanged += (s, running) =>
            {
                if (!initialState.HasValue)
                {
                    initialState = running;
                }
            };
            
            // Act - Start monitoring
            monitor.Start();
            Thread.Sleep(200); // Give time for initial check
            
            // Assert - Should immediately detect that explorer.exe is running
            Assert.True(initialState.HasValue, "Should receive initial state event");
            Assert.True(initialState.Value, "Should detect that explorer.exe is already running");
            
            // Cleanup
            monitor.Dispose();
        }
    }
}
