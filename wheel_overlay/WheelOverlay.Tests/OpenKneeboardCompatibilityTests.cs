using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class OpenKneeboardCompatibilityTests
    {
        // Validates: Requirements 3.6, 4.1
        [Fact]
        public void WindowHandle_IsAccessible_WhenMinimized()
        {
            // Arrange
            bool testPassed = false;
            Exception? testException = null;

            // WPF tests must run on STA thread
            var thread = new Thread(() =>
            {
                try
                {
                    // Create a window with ViewModel
                    var profile = new Profile
                    {
                        Name = "Test Profile",
                        PositionCount = 100,
                        Layout = DisplayLayout.Grid
                    };
                    
                    var settings = new AppSettings();
                    settings.Profiles.Add(profile);
                    settings.SelectedProfileId = profile.Id;
                    
                    var viewModel = new OverlayViewModel(settings);
                    var window = new Window
                    {
                        Title = "WheelOverlay Test",
                        DataContext = viewModel,
                        Width = 200,
                        Height = 200,
                        ShowInTaskbar = true
                    };

                    // Show window
                    window.Show();
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    Thread.Sleep(100);

                    // Get window handle when visible
                    var hwnd = new WindowInteropHelper(window).Handle;
                    var visibleHandleValid = hwnd != IntPtr.Zero && IsWindow(hwnd);

                    // Act - Minimize window
                    window.WindowState = WindowState.Minimized;
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    Thread.Sleep(100);

                    // Assert - Window handle should still be valid and accessible
                    var minimizedHandleValid = hwnd != IntPtr.Zero && IsWindow(hwnd);
                    
                    // Verify window is still visible to external applications (not WS_EX_TOOLWINDOW)
                    var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                    var hasToolWindowStyle = (extendedStyle & WS_EX_TOOLWINDOW) != 0;

                    testPassed = visibleHandleValid && minimizedHandleValid && !hasToolWindowStyle;

                    // Cleanup
                    window.Close();
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(5));

            if (testException != null)
                throw testException;

            Assert.True(testPassed, "Window handle should be accessible when minimized and not have WS_EX_TOOLWINDOW style");
        }

        [Fact]
        public void Window_IsDiscoverableByTitle_WhenMinimized()
        {
            // FindWindow Win32 API is unreliable in CI (headless/non-interactive session)
            if (Infrastructure.TestConfiguration.IsRunningInCI())
            {
                return;
            }

            // Arrange
            bool testPassed = false;
            Exception? testException = null;

            // WPF tests must run on STA thread
            var thread = new Thread(() =>
            {
                try
                {
                    // Create a window with a unique title
                    var profile = new Profile
                    {
                        Name = "Test Profile",
                        PositionCount = 100,
                        Layout = DisplayLayout.Grid
                    };
                    
                    var settings = new AppSettings();
                    settings.Profiles.Add(profile);
                    settings.SelectedProfileId = profile.Id;
                    
                    var viewModel = new OverlayViewModel(settings);
                    var uniqueTitle = $"WheelOverlay_OpenKneeboard_Test_{Guid.NewGuid()}";
                    
                    var window = new Window
                    {
                        Title = uniqueTitle,
                        DataContext = viewModel,
                        Width = 200,
                        Height = 200,
                        ShowInTaskbar = true
                    };

                    // Show window
                    window.Show();
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    Thread.Sleep(100);

                    // Verify window is discoverable when visible
                    var visibleHandle = FindWindow(null, uniqueTitle);
                    var visibleDiscoverable = visibleHandle != IntPtr.Zero;

                    // Act - Minimize window
                    window.WindowState = WindowState.Minimized;
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    Thread.Sleep(100);

                    // Assert - Window should still be discoverable by title
                    var minimizedHandle = FindWindow(null, uniqueTitle);
                    var minimizedDiscoverable = minimizedHandle != IntPtr.Zero;

                    testPassed = visibleDiscoverable && minimizedDiscoverable;

                    // Cleanup
                    window.Close();
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
                catch (Exception ex)
                {
                    testException = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(5));

            if (testException != null)
                throw testException;

            Assert.True(testPassed, "Window should be discoverable by title when minimized");
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
    }
}
