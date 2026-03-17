using System;
using System.IO;

namespace Services
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logPath = "logs/error.log";

        public static void LogError(string fileName, string message, Exception ex = null)
        {
            lock (_lock)
            {
                if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");

                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR | File: {fileName} | Message: {message}";
                if (ex != null) logMessage += $" | Exception: {ex.Message}";

                // Lưu vào file error.log
                File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                
                // In ra Console bình thường
                Console.WriteLine(logMessage);
            }
        }
    }
}