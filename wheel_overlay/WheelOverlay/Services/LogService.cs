using System;
using System.IO;
using System.Text;

namespace WheelOverlay.Services
{
    public static class LogService
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WheelOverlay",
            "logs.txt");

        private static readonly object _lock = new object();

        static LogService()
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Append session start separator
                Log($"=== Application Started: {DateTime.Now} ===");
            }
            catch (Exception) 
            {
                // If we can't create log dir, we are in trouble, but try not to crash static ctor
            }
        }

        public static void Info(string message) => Log($"[INFO] {message}");
        public static void Error(string message) => Log($"[ERROR] {message}");
        public static void Error(string message, Exception ex)
        {
            Log($"[ERROR] {message}");
            Log($"[EXCEPTION] {ex}");
        }

        private static void Log(string line)
        {
            try
            {
                lock (_lock)
                {
                    // Truncate if > 1MB
                    var fileInfo = new FileInfo(LogPath);
                    if (fileInfo.Exists && fileInfo.Length > 1 * 1024 * 1024)
                    {
                        File.WriteAllText(LogPath, $"[LOG TRUNCATED - New Session {DateTime.Now}]\n");
                    }

                    File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {line}{Environment.NewLine}");
                }
            }
            catch
            {
                // Swallowing log exceptions to prevent app crash due to logging
            }
        }

        public static string GetLogPath() => LogPath;
    }
}
