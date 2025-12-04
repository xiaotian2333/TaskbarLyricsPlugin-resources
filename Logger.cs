using System;
using System.IO;

namespace TaskbarLyrics
{
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskbarLyrics", "debug.log");

        private static readonly object _lock = new object();

        static Logger()
        {
            // 确保日志目录存在
            var directory = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 启动时清空日志文件
            try
            {
                File.WriteAllText(LogPath, $"=== TaskbarLyrics 启动 - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法创建日志文件: {ex.Message}");
            }
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        public static void Debug(string message)
        {
            WriteLog("DEBUG", message);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    var logEntry = $"[{DateTime.Now:HH:mm:ss}] {level}: {message}{Environment.NewLine}";
                    File.AppendAllText(LogPath, logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志失败: {ex.Message}");
            }
        }

        public static string GetLogPath()
        {
            return LogPath;
        }
    }
}