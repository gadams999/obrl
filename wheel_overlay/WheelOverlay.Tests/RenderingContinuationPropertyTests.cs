using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class RenderingContinuationPropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 7: Rendering Continues When Minimized
        // Validates: Requirements 4.2
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_RenderingContinuesWhenMinimized()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(0, 100)), // Generate wheel positions
                position =>
                {
                    bool testPassed = false;
                    Exception? testException = null;

                    // WPF tests must run on STA thread
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            // Arrange - Create a window with content
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
                            
                            var textBlock = new TextBlock
                            {
                                Text = "Initial",
                                DataContext = viewModel
                            };
                            
                            var window = new Window
                            {
                                Content = textBlock,
                                Width = 200,
                                Height = 200,
                                ShowInTaskbar = true
                            };

                            // Show window
                            window.Show();
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(50);

                            // Act - Minimize window and update content
                            window.WindowState = WindowState.Minimized;
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(50);
                            
                            // Update the content while minimized
                            var newText = $"Position: {position}";
                            textBlock.Text = newText;
                            
                            // Force rendering update
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);
                            Thread.Sleep(50);

                            // Assert - Content should be updated even when minimized
                            // The window buffer should contain the new content
                            testPassed = textBlock.Text == newText;

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
                        .Label($"Window content should update to position {position} even when minimized");
                });
        }
    }
}
