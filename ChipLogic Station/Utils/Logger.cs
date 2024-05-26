using System;
using System.IO;

namespace ChipLogic.Utils
{
    public static class Logger
    {
        private static readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", $"error-{DateTime.Now:yyyy-MM-dd}.log");

        public static void Log(string message, bool isError = false, bool debug = false)
        {
            if (isError || debug)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Handle any exceptions related to logging (e.g., file access issues)
                }
            }
        }
    }
}
