using System;
using System.IO;
using System.Text;

namespace RHttpServer.Logging
{
    /// <summary>
    ///     Class used to control the logging of the server.
    /// </summary>
    public static class Logger
    {
        private static LoggingOptions _logOpt = LoggingOptions.None;
        private static string _logFilePath;

        /// <summary>
        ///     Call this method to change to logging option
        /// </summary>
        /// <param name="logOpt"></param>
        /// <param name="logFilePath"></param>
        public static void Configure(LoggingOptions logOpt, string logFilePath = "")
        {
            _logOpt = logOpt;
            _logFilePath = logFilePath;
            if (logOpt == LoggingOptions.File && string.IsNullOrWhiteSpace(logFilePath))
            {
                Console.WriteLine("\nYou must give a filepath if choosing File" +
                                  "\nLogging now turned off");
                _logOpt = LoggingOptions.None;
            }
        }

        /// <summary>
        ///     Logs an exception, based of logging option
        /// </summary>
        /// <param name="ex"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static void Log(Exception ex)
        {
            switch (_logOpt)
            {
                case LoggingOptions.None:
                    break;
                case LoggingOptions.Terminal:
                    Console.WriteLine($"{DateTime.Now.ToString("g")}: {ex.GetType().Name} - {ex.Message}\n Stack trace:\n{ex.StackTrace}");
                    break;
                case LoggingOptions.File:
                    File.AppendAllText(_logFilePath,
                        $"{DateTime.Now.ToString("g")}: {ex.GetType().Name} - {ex.Message}\n Stack trace:\n{ex.StackTrace}\n", Encoding.Default);
                    break;
            }
        }

        /// <summary>
        ///     Log an item using title and message
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        public static void Log(string title, string message)
        {
            switch (_logOpt)
            {
                case LoggingOptions.None:
                    break;
                case LoggingOptions.Terminal:
                    Console.WriteLine($"{DateTime.Now.ToString("g")}: {title} - {message}");
                    break;
                case LoggingOptions.File:
                    File.AppendAllText(_logFilePath, $"{DateTime.Now.ToString("g")}: {title} - {message}\n",
                        Encoding.Default);
                    break;
            }
        }
    }
}