using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class WindowDiscoverabilityPropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 8: Window Discoverability Is State-Independent
        // Validates: Requirements 4.3
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_WindowDiscoverabilityIsStateIndependent()
        {
            return Prop.ForAll(
                Arb.From(Gen.Elements(new[] { WindowState.Normal, WindowState.Minimized })),
                windowState =>
                {
                    bool testPassed = false;
                    Exception? testException = null;

                    // WPF tests must run on STA thread
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            // Arrange - Create a window with a unique title
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
                            var uniqueTitle = $"WheelOverlay_Test_{Guid.NewGuid()}";
                            
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
                            Thread.Sleep(50);

                            // Get window handle when visible
                            var hwnd = new WindowInteropHelper(window).Handle;
                            var visibleHandleValid = hwnd != IntPtr.Zero;

                            // Act - Change window state
                            window.WindowState = windowState;
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(100);

                            // Assert - Window should still be discoverable by handle
                            var stateHandleValid = hwnd != IntPtr.Zero && IsWindow(hwnd);
                            
                            // Also verify we can find it by title
                            var foundByTitle = FindWindow(null, uniqueTitle);
                            var foundByTitleValid = foundByTitle != IntPtr.Zero;

                            testPassed = visibleHandleValid && stateHandleValid && foundByTitleValid;

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

                    return testPassed
                        .Label($"Window should be discoverable in {windowState} state");
                });
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
    }
}
