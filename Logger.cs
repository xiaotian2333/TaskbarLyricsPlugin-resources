using System;
using System.IO;
using System.Diagnostics;

namespace TaskbarLyrics
{
    public enum LogLevel
    {
        Off = 0,
        Error = 1,
        Info = 2,
        Debug = 3
    }

    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TaskbarLyrics", "debug.log");

        private static readonly object _lock = new object();
        private static string _lastMessage = string.Empty;
        private static DateTime _lastMessageTime = DateTime.MinValue;
        private static readonly TimeSpan _duplicateThreshold = TimeSpan.FromSeconds(5); // 5秒内的重复消息会被过滤
        private static LogLevel _currentLogLevel = LogLevel.Off; // 默认关闭日志

        static Logger()
        {
            // 根据运行环境设置默认日志级别
            SetDefaultLogLevel();

            // 确保日志目录存在
            var directory = Path.GetDirectoryName(LogPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 只有在日志级别不为Off时才创建日志文件
            if (_currentLogLevel != LogLevel.Off)
            {
                try
                {
                    File.WriteAllText(LogPath, $"=== TaskbarLyrics 启动 - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                    File.AppendAllText(LogPath, $"=== 日志级别: {_currentLogLevel} ==={Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法创建日志文件: {ex.Message}");
                }
            }
        }

        private static void SetDefaultLogLevel()
        {
#if DEBUG
            // 开发环境默认使用Debug级别
            _currentLogLevel = LogLevel.Debug;
#else
            // 生产环境检查是否附加了调试器
            if (Debugger.IsAttached)
            {
                // 如果附加了调试器，使用Debug级别
                _currentLogLevel = LogLevel.Debug;
            }
            else
            {
                // 否则默认关闭日志
                _currentLogLevel = LogLevel.Off;
            }
#endif

            // 也可以通过环境变量覆盖日志级别
            string envLogLevel = Environment.GetEnvironmentVariable("TASKBARDLYRICS_LOG_LEVEL");
            if (!string.IsNullOrEmpty(envLogLevel) && Enum.TryParse<LogLevel>(envLogLevel, true, out LogLevel logLevel))
            {
                _currentLogLevel = logLevel;
            }
        }

        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;

            if (level != LogLevel.Off)
            {
                try
                {
                    File.AppendAllText(LogPath, $"=== 日志级别已更改为: {_currentLogLevel} - {DateTime.Now:HH:mm:ss} ==={Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法写入日志级别变更: {ex.Message}");
                }
            }
        }

        public static LogLevel GetLogLevel()
        {
            return _currentLogLevel;
        }

        public static void Info(string message)
        {
            if (_currentLogLevel >= LogLevel.Info)
                WriteLog("INFO", message);
        }

        public static void Error(string message)
        {
            if (_currentLogLevel >= LogLevel.Error)
                WriteLog("ERROR", message);
        }

        public static void Debug(string message)
        {
            if (_currentLogLevel >= LogLevel.Debug)
                WriteLog("DEBUG", message);
        }

        private static void WriteLog(string level, string message)
        {
            // 如果日志级别为Off，不写入任何日志
            if (_currentLogLevel == LogLevel.Off)
                return;

            try
            {
                lock (_lock)
                {
                    // 过滤重复的消息
                    if (message == _lastMessage && DateTime.Now - _lastMessageTime < _duplicateThreshold)
                    {
                        return; // 跳过重复的消息
                    }

                    _lastMessage = message;
                    _lastMessageTime = DateTime.Now;

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