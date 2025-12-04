# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

TaskbarLyricsPlugin-resources 是一个基于 C#/.NET 8 和 WPF 的 Windows 任务栏歌词显示应用程序。该应用程序在 Windows 任务栏上显示当前播放歌曲的歌词，并支持逐字高亮、翻译显示和多种自定义选项。

## 技术栈

- **.NET 8** (target framework: net8.0-windows)
- **WPF** (Windows Presentation Foundation) - 用户界面框架
- **Windows Forms** - 用于系统托盘图标和上下文菜单
- **Newtonsoft.Json** - JSON 序列化和反序列化
- **Win32 API** - 通过 P/Invoke 调用原生 Windows API 进行窗口管理

## 构建和开发命令

```bash
# 构建项目
dotnet build

# 运行项目（开发模式）
dotnet run

# 发布为可执行文件
dotnet publish -c Release -r win-x64 --self-contained
```

## 架构和核心组件

### 主要架构模式
- **MVVM-like 架构**：虽然不是严格的 MVVM，但分离了 UI (XAML) 和逻辑 (C#)
- **事件驱动模型**：使用 DispatcherTimer 进行定时更新和动画
- **API 客户端模式**：通过 HTTP API 与外部歌词服务通信

### 核心组件

1. **App.xaml.cs** - 应用程序入口点
   - 系统托盘图标管理
   - 全局异常处理
   - 配置管理初始化
   - 右键菜单（字体、颜色、对齐、翻译设置）

2. **MainWindow.xaml.cs** - 主窗口和歌词显示
   - 多个定时器管理（歌词更新、位置同步、动画）
   - 歌词渲染和同步
   - 播放控制（播放/暂停、上一首、下一首）
   - 鼠标交互处理

3. **LyricsApiService.cs** - API 服务层
   - 与本地歌词 API 服务器通信（端口 35374）
   - 获取歌词、当前播放状态和控制播放
   - 错误处理和备用 API 端点

4. **LyricsRenderer.cs** - 歌词渲染引擎
   - 解析带时间戳的歌词（LRC 格式）
   - 逐字动画和高亮效果
   - 双行显示（原文+翻译）
   - CJK 字符支持
   - 平滑动画和渐变效果

5. **TaskbarMonitor.cs** - Windows API 封装
   - 任务栏位置检测和窗口定位
   - 窗口样式设置（透明、置顶、穿透等）
   - Win32 API 调用封装

6. **ConfigManager.cs** - 配置管理
   - JSON 配置文件读写
   - 用户偏好设置持久化
   - 配置位置：%AppData%/TaskbarLyrics/config.json

7. **Models/** - 数据模型
   - `LyricsResponse.cs` - API 响应模型
   - `NowPlayingResponse.cs` - 播放状态模型
   - `ConfigResponse.cs` - 配置响应模型
   - `WordTiming.cs` - 词语时间同步模型

### 关键特性

1. **逐字同步动画**：使用精确的时间戳实现词语级别的同步高亮
2. **多语言支持**：特别优化了 CJK 字符的渲染和动画
3. **任务栏集成**：通过 Win32 API 实现真正的任务栏嵌入
4. **实时控制**：可以通过 API 控制音乐播放
5. **高度可定制**：字体、颜色、对齐方式、翻译等均可自定义

### 开发注意事项

1. **窗口管理**：应用程序使用特殊的窗口样式来嵌入任务栏，需要小心处理窗口状态
2. **定时器管理**：多个 DispatcherTimer 需要在窗口关闭时正确清理
3. **API 依赖**：依赖于本地运行的歌词 API 服务器（localhost:35374）
4. **DPI 缩放**：需要考虑不同 DPI 设置下的显示效果
5. **内存管理**：歌词渲染缓存需要定期清理以避免内存泄漏

### 窗口 XAML 文件
- `MainWindow.xaml` - 主歌词显示窗口
- `SettingsWindow.xaml` - 设置配置窗口
- `AboutWindow.xaml` - 关于信息窗口
- `App.xaml` - 应用程序资源和启动配置