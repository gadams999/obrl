using System;
using WheelOverlay.Services;

namespace WheelOverlay
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                // Force an early log to verify we are running
                LogService.Info("Program.Main started. Initializing Windows Forms...");
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                LogService.Info("Creating App instance...");
                var app = new App();
                
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
