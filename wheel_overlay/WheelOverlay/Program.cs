using System;
using WheelOverlay.Services;

using System.Runtime.ExceptionServices;
using System.Security;

namespace WheelOverlay
{
    public static class Program
    {
        [STAThread]
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static void Main()
        {
            try
            {
                // Force an early log to verify we are running
                LogService.Info("Program.Main started.");

                LogService.Info("Creating App instance...");
                App app = null;
                try
                {
                    app = new App();
                }
                catch (Exception ex)
                {
                    LogService.Error("CRASH IN APP CONSTRUCTOR", ex);
                    throw;
                }
                
                LogService.Info("InitializeComponent...");
                app.InitializeComponent();
                
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
        }
    }
}
