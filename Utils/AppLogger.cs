using System;
using System.IO;

namespace ThGameMode.Utils
{
    /// <summary>
    /// Logger simples baseado em arquivo para diagnóstico local da aplicação.
    /// </summary>
    public static partial class AppLogger
    {
        // O lock evita que múltiplas threads escrevam no mesmo arquivo ao mesmo tempo.
        private static readonly object _lock = new object();

        // Cada dia gera um arquivo próprio dentro da pasta de logs ao lado do executável.
        private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string LogFile = Path.Combine(LogDir, $"log_{DateTime.Now:yyyyMMdd}.txt");

        /// <summary>
        /// Severidade da mensagem gravada no log.
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        static AppLogger()
        {
            // Garante a infraestrutura mínima de log antes de qualquer escrita.
            if (!Directory.Exists(LogDir))
                Directory.CreateDirectory(LogDir);

            RotateLogs();
        }

        /// <summary>
        /// Escreve uma linha no log atual sem permitir que falhas de I/O derrubem a aplicação.
        /// </summary>
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

            // Mantém a pasta enxuta para que os logs não cresçam indefinidamente.
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
