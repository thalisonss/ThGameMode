using System;
using System.IO;

namespace ThGameMode.Utils
{
    public static partial class AppLogger
    {
        private static readonly object _lock = new object();
        private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFile = Path.Combine(LogDir, $"log_{DateTime.Now:yyyyMMdd}.txt");

        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        static AppLogger()
        {
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);

            RotateLogs();
        }

        public static void Write(LogLevel level, string message)
        {
            lock (_lock)
            {
                try
                {
                    string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                    File.AppendAllLines(LogFile, new[] { line });
                }
                catch
                {
                    // Nunca deixar log travar a app
                }
            }
        }

        private static void RotateLogs()
        {
            var files = Directory.GetFiles(LogDir, "*.txt");

            // Manter somente os últimos 7 dias
            foreach (var file in files)
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddDays(-7))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
    }
}
