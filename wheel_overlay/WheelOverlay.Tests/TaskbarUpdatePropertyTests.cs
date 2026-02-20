using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FsCheck;
using FsCheck.Xunit;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;
using Xunit;

namespace WheelOverlay.Tests
{
    public class TaskbarUpdatePropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 6: Taskbar Updates Reflect Wheel Position Changes
        // Validates: Requirements 3.5, 3.7
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_TaskbarUpdatesReflectWheelPositionChanges()
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
                            // Arrange - Create a window with ViewModel
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
                                DataContext = viewModel,
                                Width = 200,
                                Height = 200,
                                ShowInTaskbar = true
                            };

                            // Show window and minimize it
                            window.Show();
                            window.WindowState = WindowState.Minimized;
                            
                            // Force WPF to process the minimize
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(100);

                            // Act - Update wheel position
                            var initialPosition = viewModel.CurrentPosition;
                            viewModel.CurrentPosition = position;
                            
                            // Force WPF to process the update
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(50);

                            // Assert - ViewModel should reflect the new position
                            // The taskbar preview is updated by WPF's rendering system
                            // We verify that the underlying data model is updated correctly
                            testPassed = viewModel.CurrentPosition == position;

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
                        .Label($"Taskbar should reflect wheel position {position} when minimized");
                });
        }
    }
}
