using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace TaskbarLyrics
{
    public static class FullScreenDetector
    {
        private static System.Windows.Forms.Timer _checkTimer;
        private static bool _isFullScreenActive = false;

        public static event EventHandler<bool> FullScreenStatusChanged;

        public static bool IsFullScreenActive => _isFullScreenActive;

        static FullScreenDetector()
        {
            _checkTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 每1秒检查一次，避免过于频繁的检查
            };
            _checkTimer.Tick += CheckFullScreenStatus;
        }

        public static void Start()
        {
            if (!_checkTimer.Enabled)
            {
                _checkTimer.Start();
                // 立即检查一次
                CheckFullScreenStatus(null, null);
            }
        }

        /// <summary>
        /// 停止全屏检测功能
        /// </summary>
        public static void Stop()
        {
            _checkTimer.Stop(); // 停止计时器
            _isFullScreenActive = false; // 重置全屏状态标志
            Logger.Info("全屏检测已停止"); // 记录停止信息到日志
        }

        private static void CheckFullScreenStatus(object sender, EventArgs e)
        {
            try
            {
                bool currentFullScreenStatus = IsFullScreenWindow();

                if (currentFullScreenStatus != _isFullScreenActive)
                {
                    _isFullScreenActive = currentFullScreenStatus;

                    if (currentFullScreenStatus)
                    {
                        Logger.Info($"检测到全屏应用: {GetActiveWindowTitle()}");
                    }
                    else
                    {
                        Logger.Info("退出全屏模式");
                    }

                    FullScreenStatusChanged?.Invoke(null, _isFullScreenActive);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"全屏检测错误: {ex.Message}");
            }
        }

        private static bool IsFullScreenWindow()
        {
            IntPtr hWnd = GetForegroundWindow();

            if (hWnd == IntPtr.Zero)
            {
                return false;
            }

            // 获取窗口标题用于调试
            var title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            string windowTitle = title.ToString();

            // 检查窗口是否可见
            if (!IsWindowVisible(hWnd))
            {
                return false;
            }

            // 获取窗口信息
            if (!GetWindowRect(hWnd, out RECT windowRect))
            {
                return false;
            }

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            // 获取屏幕信息
            RECT screenRect = GetScreenRect();
            int screenWidth = screenRect.Right - screenRect.Left;
            int screenHeight = screenRect.Bottom - screenRect.Top;

            // 检查窗口是否覆盖整个屏幕（允许小的误差）
            const int tolerance = 10; // 允许10像素的误差
            bool isFullScreenSize = (windowWidth >= screenWidth - tolerance &&
                                    windowHeight >= screenHeight - tolerance);

            if (!isFullScreenSize)
            {
                return false;
            }

            // 检查窗口样式，确保不是系统窗口
            if (!ShouldConsiderAsFullScreen(hWnd))
            {
                return false;
            }

            return true;
        }

        private static bool ShouldConsiderAsFullScreen(IntPtr hWnd)
        {
            // 获取窗口类名
            var className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            string windowClassName = className.ToString();

            // 排除系统窗口
            if (windowClassName.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||  // 任务栏
                windowClassName.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||        // 桌面
                windowClassName.Equals("WorkerW", StringComparison.OrdinalIgnoreCase) ||         // 桌面
                windowClassName.Equals("DV2ControlHost", StringComparison.OrdinalIgnoreCase) ||  // 任务视图
                windowClassName.Contains("Windows.UI.Core.CoreWindow"))                         // UWP 系统窗口
            {
                return false;
            }

            // 获取窗口标题
            var title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            string windowTitle = title.ToString();

            // 排除空标题的窗口
            if (string.IsNullOrWhiteSpace(windowTitle))
                return false;

            // 检查窗口是否具有典型应用程序特征
            long exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);

            // 排除工具窗口和纯顶层窗口
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return false;

            return true;
        }

        private static RECT GetScreenRect()
        {
            // 获取窗口所在屏幕的信息
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                HMONITOR hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);

                if (hMonitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

                    if (GetMonitorInfo(hMonitor, ref monitorInfo))
                    {
                        return monitorInfo.rcMonitor;
                    }
                }
            }

            // 如果无法获取特定屏幕，返回主屏幕
            return new RECT
            {
                Left = 0,
                Top = 0,
                Right = Screen.PrimaryScreen.Bounds.Width,
                Bottom = Screen.PrimaryScreen.Bounds.Height
            };
        }

        public static string GetActiveWindowTitle()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return "未知窗口";

            var title = new StringBuilder(256);
            GetWindowText(hWnd, title, title.Capacity);
            return title.ToString();
        }

        #region Win32 API Declarations

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern HMONITOR MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        private readonly struct HMONITOR
        {
            private readonly IntPtr handle;
            private HMONITOR(IntPtr handle) => this.handle = handle;
            public static implicit operator HMONITOR(IntPtr handle) => new HMONITOR(handle);
            public static implicit operator IntPtr(HMONITOR hMonitor) => hMonitor.handle;
        }

        // Constants
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        #endregion
    }
}