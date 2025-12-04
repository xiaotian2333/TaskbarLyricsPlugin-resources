using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TaskbarLyrics.Models;

namespace TaskbarLyrics
{
    public partial class MainWindow : Window
    {
        private LyricsApiService _apiService;
        private DispatcherTimer _updateTimer;
        private DispatcherTimer _positionTimer;
        private DispatcherTimer _restoreTimer;
        private DispatcherTimer _nowPlayingTimer;
        private List<LyricsLine> _lyricsLines = new List<LyricsLine>();
        private string _lastLyricsText = "";
        private bool _forceRefresh = false;
        private int _currentPosition = 0;
        private int _lastLyricsLineCount = 0; // 用于避免重复的歌词解析日志
        private bool _isPlaying = false;
        private bool _isMouseOver = false;
        private DispatcherTimer _mouseLeaveTimer;
        private DispatcherTimer _smoothUpdateTimer;
        private bool _isClosing = false;
        private string _lastSongTitle = ""; // 用于检测歌曲变化

        public MainWindow()
        {
            InitializeComponent();

            _apiService = new LyricsApiService();

            this.IsVisibleChanged += MainWindow_IsVisibleChanged;
            this.Closing += MainWindow_Closing;

            InitializeWindow();
            SetupTimers();
            SetupFullScreenDetection();

            this.Focusable = true;
            this.IsHitTestVisible = true;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _isClosing = true;

            _updateTimer?.Stop();
            _positionTimer?.Stop();
            _restoreTimer?.Stop();
            _nowPlayingTimer?.Stop();
            _smoothUpdateTimer?.Stop();
            _mouseLeaveTimer?.Stop();

            FullScreenDetector.Stop();
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_isClosing)
                return;

            // 如果全屏检测正在运行且已检测到全屏，不要自动恢复可见性
            if (FullScreenDetector.IsFullScreenActive && ConfigManager.CurrentConfig.HideOnFullscreen)
            {
                Logger.Info("全屏模式激活，跳过自动可见性恢复");
                return;
            }

            if (!this.IsVisible)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!_isClosing && !this.IsVisible)
                    {
                        this.Visibility = Visibility.Visible;
                        TaskbarMonitor.ForceShowWindow(this);
                        Logger.Info("自动恢复窗口可见性");
                    }
                }), DispatcherPriority.Background);
            }
        }

        private void SetupTimers()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _updateTimer.Tick += async (s, e) => await UpdateLyrics();
            _updateTimer.Start();

            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _positionTimer.Tick += (s, e) => UpdateWindowPosition();
            _positionTimer.Start();

            _restoreTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _restoreTimer.Tick += (s, e) => EnsureWindowOnTop();
            _restoreTimer.Start();

            _nowPlayingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _nowPlayingTimer.Tick += async (s, e) => await UpdateNowPlaying();
            _nowPlayingTimer.Start();

            _smoothUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(32)
            };
            _smoothUpdateTimer.Tick += (s, e) => SmoothUpdateLyrics();
            _smoothUpdateTimer.Start();

            _mouseLeaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _mouseLeaveTimer.Tick += (s, e) =>
            {
                _mouseLeaveTimer.Stop();
                if (!_isMouseOver)
                {
                    HideControlPanel();
                }
            };
        }

        private async Task UpdateNowPlaying()
        {
            try
            {
                var nowPlaying = await _apiService.GetNowPlayingAsync();
                if (nowPlaying?.Status == "success")
                {
                    _currentPosition = nowPlaying.Position;
                    _isPlaying = nowPlaying.IsPlaying;

                    // 检测歌曲变化
                    string currentSongTitle = $"{nowPlaying.Artist} - {nowPlaying.Title}";
                    if (!string.IsNullOrEmpty(currentSongTitle) && currentSongTitle != _lastSongTitle)
                    {
                        Logger.Info($"检测到歌曲变化: {_lastSongTitle} -> {currentSongTitle}");
                        _lastSongTitle = currentSongTitle;

                        // 歌曲变化时清空歌词，强制重新加载
                        ClearLyrics();
                        _lastLyricsText = "";
                        _lyricsLines.Clear();
                        _forceRefresh = true; // 强制刷新歌词
                    }

                    PlayPauseButton.Content = _isPlaying ? "⏸" : "▶";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"获取当前播放信息时出错: {ex.Message}");
            }
        }

        private void SmoothUpdateLyrics()
        {
            if (_lyricsLines.Count > 0 && !_isClosing)
            {
                UpdateCurrentLyricsLine();
            }
        }

        private void EnsureWindowOnTop()
        {
            if (_isClosing) return;

            // 如果全屏模式激活，不要强制显示窗口
            if (FullScreenDetector.IsFullScreenActive && ConfigManager.CurrentConfig.HideOnFullscreen)
            {
                return;
            }

            try
            {
                TaskbarMonitor.SetWindowToTaskbarLevel(this);

                if (this.Visibility != Visibility.Visible && !_isClosing)
                {
                    this.Visibility = Visibility.Visible;
                }

                if (this.WindowState != WindowState.Normal && !_isClosing)
                {
                    this.WindowState = WindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"设置窗口置顶时出错: {ex.Message}");
            }
        }

        private void InitializeWindow()
        {
            this.WindowState = WindowState.Normal;
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
            this.Topmost = true;
            
            this.Focusable = true;
            
            TaskbarMonitor.PositionWindowOnTaskbar(this);
            SetWindowTransparency();
            TaskbarMonitor.SetWindowToTaskbarLevel(this);
            
            ApplyConfig();
        }

        private void SetWindowTransparency()
        {
            TaskbarMonitor.EnableMouseEvents(this);
        }

        public void RefreshConfig()
        {
            ApplyConfig();
            _forceRefresh = true;

            // 清理歌词渲染缓存，确保过滤规则立即生效
            LyricsRenderer.ClearCache();

            // 根据配置重新设置全屏检测
            if (ConfigManager.CurrentConfig.HideOnFullscreen)
            {
                if (!FullScreenDetector.IsFullScreenActive)
                {
                    FullScreenDetector.Start();
                    Logger.Info("配置更新：全屏检测已启动");
                }
            }
            else
            {
                FullScreenDetector.Stop();
                Logger.Info("配置更新：全屏检测已停止");

                // 如果之前因为全屏而隐藏了窗口，现在要显示出来
                if (this.Visibility == Visibility.Collapsed)
                {
                    this.Visibility = Visibility.Visible;
                    TaskbarMonitor.ForceShowWindow(this);
                }
            }
        }

        public void ForceRefreshLyrics()
        {
            _forceRefresh = true;
            LyricsRenderer.ClearCache();
        }

        private void ApplyConfig()
        {
            var config = ConfigManager.CurrentConfig;

            this.Background = Brushes.Transparent;
            LyricsContainer.Background = Brushes.Transparent;

            if (!string.IsNullOrEmpty(config.BackgroundColor) && 
                config.BackgroundColor != "#00000000")
            {
                try
                {
                    var brush = (Brush)new BrushConverter().ConvertFromString(config.BackgroundColor);
                    LyricsContainer.Background = brush;
                }
                catch (Exception ex)
                {
                    Logger.Error($"应用背景颜色时出错: {ex.Message}");
                    LyricsContainer.Background = Brushes.Transparent;
                }
            }

            ApplyAlignment();
        }

        private void ApplyAlignment()
        {
            var config = ConfigManager.CurrentConfig;
            if (string.IsNullOrEmpty(config.Alignment))
                return;

            HorizontalAlignment alignment = HorizontalAlignment.Center;
            Thickness margin = new Thickness(0);
            
            switch (config.Alignment.ToLower())
            {
                case "left":
                    alignment = HorizontalAlignment.Left;
                    break;
                case "right":
                    alignment = HorizontalAlignment.Right;
                    margin = new Thickness(0, 0, this.ActualWidth * 0.25, 0);
                    break;
                case "center":
                default:
                    alignment = HorizontalAlignment.Center;
                    break;
            }

            LyricsContainer.HorizontalAlignment = alignment;
            ControlPanelBorder.HorizontalAlignment = alignment;
            
            LyricsContainer.Margin = margin;
            ControlPanelBorder.Margin = margin;
            
            // 移除频繁的对齐方式日志输出
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            _forceRefresh = true;
            
            ApplyAlignment();
        }

        private async Task UpdateLyrics()
        {
            if (_isClosing) return;

            try
            {
                var lyricsResponse = await _apiService.GetLyricsAsync();
                if (lyricsResponse?.Status != "success" || string.IsNullOrEmpty(lyricsResponse.Lyric))
                {
                    // 没有歌词时不清空显示，保持当前歌词
                    _forceRefresh = false;
                    return;
                }

                string currentLyrics = lyricsResponse.Lyric.Trim();

                if (currentLyrics == _lastLyricsText && !_forceRefresh)
                {
                    return; // 歌词没有变化，跳过解析
                }

                _lastLyricsText = currentLyrics;
                _forceRefresh = false;

                // 只在歌词真正变化时才执行ParseLyrics，并传递歌曲标题
                _lyricsLines = LyricsRenderer.ParseLyrics(currentLyrics, _lastSongTitle);

                // 只在歌词行数变化时记录日志
                if (_lyricsLines.Count != _lastLyricsLineCount)
                {
                    Logger.Info($"已加载 {_lyricsLines.Count} 行歌词");
                    _lastLyricsLineCount = _lyricsLines.Count;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"更新歌词时出错: {ex.Message}");
                _forceRefresh = false;
            }
        }

        private void UpdateCurrentLyricsLine()
        {
            if (_isClosing)
            {
                return;
            }

            // 如果有歌词数据，尝试更新显示
            if (_lyricsLines != null && _lyricsLines.Count > 0)
            {
                var currentLine = LyricsRenderer.GetCurrentLyricsLine(_lyricsLines, _currentPosition);
                if (currentLine != null)
                {
                    var config = ConfigManager.CurrentConfig;
                    var lyricsVisual = LyricsRenderer.CreateDualLineLyricsVisual(currentLine, config, ActualWidth, _currentPosition);

                    if (LyricsContent.Content != lyricsVisual)
                    {
                        LyricsContent.Content = lyricsVisual;
                    }
                }
                // 没有匹配的歌词行时，不清空显示，保持当前歌词
            }
            // 没有歌词数据时，也保持当前显示，不清空
        }

        private void ClearLyrics()
        {
            if (!_isClosing)
            {
                LyricsContent.Content = null;
            }
            _lyricsLines.Clear();
        }

        private void UpdateWindowPosition()
        {
            if (_isClosing) return;
            TaskbarMonitor.PositionWindowOnTaskbar(this);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            TaskbarMonitor.SetWindowToTaskbarLevel(this);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            if (this.WindowState != WindowState.Normal && !_isClosing)
            {
                this.WindowState = WindowState.Normal;
                TaskbarMonitor.ForceShowWindow(this);
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && !_isClosing)
                this.DragMove();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
                ShowControlPanel();
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
            {
                _isMouseOver = false;
                _mouseLeaveTimer.Start();
            }
        }

        private void LyricsContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
                ShowControlPanel();
        }

        private void LyricsContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
            {
                _isMouseOver = false;
                _mouseLeaveTimer.Start();
            }
        }

        private void ControlPanelBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
            {
                _isMouseOver = true;
                _mouseLeaveTimer.Stop();
            }
        }

        private void ControlPanelBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isClosing)
            {
                _isMouseOver = false;
                _mouseLeaveTimer.Start();
            }
        }

        private void ShowControlPanel()
        {
            if (_isClosing) return;

            _isMouseOver = true;
            _mouseLeaveTimer.Stop();
            
            ControlPanelBorder.Visibility = Visibility.Visible;
            LyricsContent.Visibility = Visibility.Collapsed;
        }

        private void HideControlPanel()
        {
            if (_isClosing) return;

            ControlPanelBorder.Visibility = Visibility.Collapsed;
            LyricsContent.Visibility = Visibility.Visible;
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing) return;

            try
            {
                bool result = await _apiService.PlayPauseAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"播放/暂停时出错: {ex.Message}");
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing) return;

            try
            {
                bool result = await _apiService.NextTrackAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"下一曲时出错: {ex.Message}");
            }
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isClosing) return;

            try
            {
                bool result = await _apiService.PreviousTrackAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"上一曲时出错: {ex.Message}");
            }
        }

        private void SetupFullScreenDetection()
        {
            FullScreenDetector.FullScreenStatusChanged += OnFullScreenStatusChanged;

            // 总是先启动检测，OnFullScreenStatusChanged 会根据配置决定是否响应
            FullScreenDetector.Start();
            Logger.Info($"全屏检测已启动，配置全屏时隐藏: {ConfigManager.CurrentConfig.HideOnFullscreen}");
        }

        private void OnFullScreenStatusChanged(object sender, bool isFullScreen)
        {
            if (_isClosing) return;

            Logger.Info($"全屏状态变化: {isFullScreen}, 配置全屏时隐藏: {ConfigManager.CurrentConfig.HideOnFullscreen}, 当前窗口可见性: {this.Visibility}");

            // 只在配置启用时响应全屏状态变化
            if (!ConfigManager.CurrentConfig.HideOnFullscreen)
            {
                Logger.Info("全屏隐藏功能已禁用，忽略状态变化");
                return;
            }

            try
            {
                if (isFullScreen)
                {
                    // 检测到全屏应用，隐藏歌词
                    if (this.Visibility == Visibility.Visible)
                    {
                        Logger.Info($"全屏应用检测到，隐藏歌词 - {FullScreenDetector.GetActiveWindowTitle()}");

                        // 使用多种方法确保窗口隐藏
                        this.Visibility = Visibility.Hidden;
                        this.Hide();

                        // 额外的Win32 API调用确保隐藏
                        try
                        {
                            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                            TaskbarMonitor.ShowWindow(hwnd, 0); // SW_HIDE = 0
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"使用Win32 API隐藏窗口失败: {ex.Message}");
                        }

                        Logger.Info($"窗口状态 - Visibility: {this.Visibility}, IsVisible: {this.IsVisible}");
                    }
                    else
                    {
                        Logger.Info($"窗口已经隐藏，当前状态 - Visibility: {this.Visibility}, IsVisible: {this.IsVisible}");
                    }
                }
                else
                {
                    // 退出全屏，恢复显示
                    if (this.Visibility != Visibility.Visible || !this.IsVisible)
                    {
                        Logger.Info("退出全屏，恢复歌词显示");

                        // 使用多种方法确保窗口显示
                        this.Visibility = Visibility.Visible;
                        this.Show();

                        // 强制显示并重新设置窗口属性
                        TaskbarMonitor.ForceShowWindow(this);

                        Logger.Info($"窗口恢复后状态 - Visibility: {this.Visibility}, IsVisible: {this.IsVisible}");
                    }
                    else
                    {
                        Logger.Info($"窗口已经可见，当前状态 - Visibility: {this.Visibility}, IsVisible: {this.IsVisible}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"处理全屏状态变化时出错: {ex.Message}");
            }
        }
    }
}