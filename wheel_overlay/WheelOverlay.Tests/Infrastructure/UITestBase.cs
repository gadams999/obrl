using System;
using System.Threading;
using System.Windows;
using WheelOverlay.Models;
using WheelOverlay.ViewModels;

namespace WheelOverlay.Tests.Infrastructure
{
    /// <summary>
    /// Base class for UI automation tests that provides common setup and teardown functionality.
    /// Implements IDisposable to ensure proper cleanup of test resources.
    /// 
    /// NOTE: WPF UI tests require STA (Single-Threaded Apartment) threading model.
    /// Test methods that use this base class should be marked with [STAFact] or [STATheory]
    /// instead of [Fact] or [Theory]. You may need to install the Xunit.StaFact NuGet package.
    /// 
    /// For now, this base class is designed to work with non-UI components (ViewModels, Settings)
    /// without requiring actual window creation. Full UI automation will be implemented in future tasks.
    /// 
    /// Requirements: 3.7, 4.7
    /// </summary>
    public abstract class UITestBase : IDisposable
    {
        protected Application? TestApp;
        protected MainWindow? TestWindow;
        protected OverlayViewModel? TestViewModel;
        protected AppSettings? TestSettings;
        private bool _disposed = false;

        /// <summary>
        /// Sets up the test application and main window.
        /// Creates a WPF Application instance and initializes the MainWindow.
        /// This method should be called in test setup (e.g., constructor or [Fact] method).
        /// 
        /// NOTE: This method creates a MainWindow which requires STA threading.
        /// For tests that only need ViewModels and Settings, use SetupTestViewModel() instead.
        /// </summary>
        protected virtual void SetupTestApp()
        {
            // Create test settings and view model first
            SetupTestViewModel();

            // Note: Creating MainWindow requires STA thread
            // This will be fully implemented when UI automation tests are added
            // For now, we focus on ViewModel and Settings testing
            
            // Ensure we're on the UI thread
            if (Application.Current == null)
            {
                TestApp = new Application();
            }
            else
            {
                TestApp = Application.Current;
            }

            // Create and configure the main window (requires STA)
            TestWindow = new MainWindow();
            
            // Set the window as the main window if we created a new app
            if (TestApp != null && TestApp.MainWindow == null)
            {
                TestApp.MainWindow = TestWindow;
            }
        }

        /// <summary>
        /// Sets up test ViewModel and Settings without creating UI windows.
        /// This method can be used in regular [Fact] tests without requiring STA threading.
        /// Use this for testing ViewModels, Settings, and business logic.
        /// </summary>
        protected virtual void SetupTestViewModel()
        {
            // Create test settings with a default profile
            TestSettings = CreateTestSettings();

            // Create test view model
            TestViewModel = new OverlayViewModel(TestSettings);
        }

        /// <summary>
        /// Creates default test settings with a basic profile configuration.
        /// This ensures tests have valid settings to work with.
        /// </summary>
        protected virtual AppSettings CreateTestSettings()
        {
            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                Name = "Test Profile",
                DeviceName = "Test Device",
                Layout = DisplayLayout.Vertical,
                PositionCount = 8,
                TextLabels = new List<string> 
                { 
                    "POS1", "POS2", "POS3", "POS4", 
                    "POS5", "POS6", "POS7", "POS8" 
                }
            };

            var settings = new AppSettings
            {
                Profiles = new List<Profile> { profile },
                SelectedProfileId = profile.Id
            };

            return settings;
        }

        /// <summary>
        /// Tears down the test application and cleans up resources.
        /// Closes the test window and shuts down the application if it was created by this test.
        /// </summary>
        protected virtual void TeardownTestApp()
        {
            try
            {
                // Close the window if it exists
                if (TestWindow != null)
                {
                    if (TestWindow.IsLoaded)
                    {
                        TestWindow.Close();
                    }
                    TestWindow = null;
                }

                // Shutdown the application if we created it
                if (TestApp != null && TestApp != Application.Current)
                {
                    TestApp.Shutdown();
                    TestApp = null;
                }
            }
            catch (Exception)
            {
                // Suppress exceptions during cleanup to avoid masking test failures
            }
        }

        /// <summary>
        /// Executes an action on the UI thread.
        /// This is necessary for WPF operations that must run on the dispatcher thread.
        /// </summary>
        /// <param name="action">The action to execute on the UI thread</param>
        protected void RunOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Waits for the UI thread to process pending operations.
        /// Useful for ensuring UI updates have completed before making assertions.
        /// </summary>
        protected void WaitForUIThread()
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Disposes of test resources.
        /// Implements the IDisposable pattern to ensure proper cleanup.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    TeardownTestApp();
                }
                _disposed = true;
            }
        }
    }
}
