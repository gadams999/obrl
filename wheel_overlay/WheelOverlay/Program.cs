using System;
using System.Threading;
using WheelOverlay.Services;

namespace WheelOverlay
{
    public static class Program
    {
        private const string MutexName = "WheelOverlay_SingleInstance_Mutex";

        [STAThread]
        public static void Main()
        {
            // Create a named mutex for single-instance enforcement
            bool createdNew;
            using (var mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Another instance is already running - exit silently
                    return;
                }

                try
                {
                    // Force an early log to verify we are running
                    LogService.Info("Program.Main started.");

                    LogService.Info("Creating App instance...");
                    App? app = null;
                    try
                    {
                        app = new App();
                    }
                    catch (Exception ex)
                    {
                        LogService.Error("CRASH IN APP CONSTRUCTOR", ex);
                        throw;
                    }
                    
                    LogService.Info("Starting app without explicit InitializeComponent...");
                    // InitializeComponent will be called automatically by WPF
                    
                    LogService.Info("Calling app.Run()...");
                    app.Run();
                }
                catch (Exception ex)
                {
                    // Catch absolutely everything including assembly load failures if possible
                    try
                    {
                        LogService.Error("CRITICAL FAILURE IN MAIN", ex);
                        
                        // Attempt to show a MessageBox as a last resort
                        System.Windows.MessageBox.Show(
                            $"Critical Startup Error:\n{ex.Message}\n\nSee logs at:\n{LogService.GetLogPath()}", 
                            "Wheel Overlay Fatal Error", 
                            System.Windows.MessageBoxButton.OK, 
                            System.Windows.MessageBoxImage.Error);
                    }
                    catch
                    {
                        // If logging fails, we are truly lost.
                    }
                    finally
                    {
                        Environment.Exit(1);
                    }
                }
                finally
                {
                    // Keep the mutex alive until the app exits
                    GC.KeepAlive(mutex);
                }
            }
        }
    }
}
