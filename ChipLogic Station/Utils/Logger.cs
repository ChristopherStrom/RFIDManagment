using System;
using System.IO;

namespace ChipLogic.Utils
{
    public static class Logger
    {
        private static readonly string logDirectoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private static readonly string logFilePath = Path.Combine(logDirectoryPath, $"error-{DateTime.Now:yyyy-MM-dd}.log");

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(logDirectoryPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create log directory: {ex.Message}");
            }
        }

        public static void Log(string message, bool isError = false, bool debug = false, string user = null, string password = null, string sqlError = null)
        {
            if (isError || debug)
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

                if (!string.IsNullOrEmpty(user))
                {
                    logMessage += $"\nUser: {user}";
                }

                if (!string.IsNullOrEmpty(password))
                {
                    logMessage += $"\nPassword: {password}";
                }

                if (!string.IsNullOrEmpty(sqlError))
                {
                    logMessage += $"\nSQL Error: {sqlError}";
                }

                try
                {
                    File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }
    }
}
