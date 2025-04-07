using System;
using System.IO;
using System.Text;

namespace DSRL.Core.Utilities
{
    /// <summary>
    /// Simple logging utility class
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DSRL", "dsrl_log.txt");
            
        private static readonly object LogLock = new object();
        
        /// <summary>
        /// Log a message with the current timestamp
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Log(string message)
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Write log message with timestamp
                lock (LogLock)
                {
                    File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Silently fail if logging fails
            }
        }
        
        /// <summary>
        /// Log an error with exception details
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="ex">Exception object</param>
        public static void LogError(string message, Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ERROR: {message}");
            sb.AppendLine($"Exception: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");
            
            Log(sb.ToString());
        }
        
        /// <summary>
        /// Clear the log file
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                if (File.Exists(LogFilePath))
                {
                    File.Delete(LogFilePath);
                }
            }
            catch
            {
                // Silently fail if clearing fails
            }
        }
    }
}