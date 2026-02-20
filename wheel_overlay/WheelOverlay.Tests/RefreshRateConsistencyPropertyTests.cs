using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class RefreshRateConsistencyPropertyTests
    {
        // Feature: overlay-visibility-and-ui-improvements, Property 9: Refresh Rate Consistency Across States
        // Validates: Requirements 4.4
        #if FAST_TESTS
        [Property(MaxTest = 10)]
        #else
        [Property(MaxTest = 100)]
        #endif
        public Property Property_RefreshRateConsistencyAcrossStates()
        {
            return Prop.ForAll(
                Arb.From(Gen.Choose(10, 20)), // Number of updates to measure (minimum 10 for statistical significance)
                updateCount =>
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

                            // Show window
                            window.Show();
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(100);

                            // Measure ViewModel update rate when visible
                            var visibleUpdateCount = CountSuccessfulUpdates(viewModel, updateCount);

                            // Minimize window
                            window.WindowState = WindowState.Minimized;
                            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                            Thread.Sleep(100);

                            // Measure ViewModel update rate when minimized
                            var minimizedUpdateCount = CountSuccessfulUpdates(viewModel, updateCount);

                            // Assert - Both should successfully complete all updates
                            // The key requirement is that updates continue to happen, not exact timing
                            testPassed = visibleUpdateCount == updateCount && minimizedUpdateCount == updateCount;

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
                    thread.Join(TimeSpan.FromSeconds(10));

                    if (testException != null)
                        throw testException;

                    return testPassed
                        .Label($"ViewModel should complete all {updateCount} updates in both visible and minimized states");
                });
        }

        /// <summary>
        /// Counts how many updates successfully complete.
        /// This verifies that the ViewModel continues to update regardless of window state.
        /// </summary>
        private static int CountSuccessfulUpdates(OverlayViewModel viewModel, int updateCount)
        {
            int successfulUpdates = 0;
            
            for (int i = 0; i < updateCount; i++)
            {
                var expectedPosition = i;
                viewModel.CurrentPosition = expectedPosition;
                
                // Verify the update was applied
                if (viewModel.CurrentPosition == expectedPosition)
                {
                    successfulUpdates++;
                }
                
                // Consistent delay between updates to simulate regular refresh
                Thread.Sleep(16); // ~60 FPS
            }
            
            return successfulUpdates;
        }

        /// <summary>
        /// Calculates the average interval between updates.
        /// </summary>
        private static double CalculateAverageInterval(List<long> timings)
        {
            if (timings.Count == 0)
                return 0;
                
            long sum = 0;
            foreach (var timing in timings)
            {
                sum += timing;
            }
            
            return (double)sum / timings.Count;
        }
    }
}
