# SharpFAI Editor 多平台架构

这是 SharpFAI 项目的新架构重构。本项目将编辑器和播放器合并为单一可执行文件，同时支持多平台部署。

## 项目结构

```
SharpFAI.Editor/
├── src/
│   ├── SharpFAI.Editor.Core/               # 核心游戏逻辑（编辑器+播放器共享）
│   │   ├── Application/                    # 窗口实现（部分类）
│   │   │   ├── PlayerWindow.cs             # 播放器窗口基类
│   │   │   ├── PlayerWindow.Playback.cs    # 播放控制
│   │   │   ├── PlayerWindow.UI.cs          # 播放器 UI 渲染
│   │   │   ├── EditorWindow.cs             # 编辑器窗口
│   │   │   ├── EditorWindow.Playback.cs    # 编辑器播放控制
│   │   │   ├── EditorWindow.Editing.cs     # 编辑功能
│   │   │   ├── EditorWindow.UI.cs          # 编辑器 UI 渲染
│   │   │   ├── IGameWindow.cs              # 窗口接口
│   │   │   └── ApplicationMode.cs          # 应用模式枚举
│   │   ├── Framework/                      # 框架组件
│   │   │   ├── Audio/                      # 音频处理
│   │   │   │   └── Music.cs                # Desktop 音频实现（LibVLC）
│   │   │   ├── Graphics/                   # 图形渲染
│   │   │   └── Particles/                  # 粒子系统
│   │   ├── Platform/                       # 平台接口
│   │   │   └── Audio/                      # 音频接口
│   │   │       ├── IAudioProvider.cs       # 音频提供者接口
│   │   │       └── IMusic.cs               # 音乐播放接口
│   │   ├── MainApplication.cs              # 应用程序主类
│   │   └── 其他核心模块...
│   │
│   └── SharpFAI.Editor.Platform/           # 平台层
│       ├── Windows/                        # Windows 应用程序
│       │   └── Program.cs                  # Windows 入口点
│       ├── Linux/                          # Linux 应用程序
│       │   └── Program.cs                  # Linux 入口点
│       ├── macOS/                          # macOS 应用程序
│       │   └── Program.cs                  # macOS 入口点
│       └── Android/                        # Android 应用程序
│           ├── MainActivity.cs             # Android 入口点
│           ├── AndroidMusic.cs             # Android 音频实现（MediaPlayer）
│           └── AndroidAudioProvider.cs     # Android 音频提供者
├── tests/                                  # 单元测试（待实现）
└── docs/                                   # 文档（待实现）
```

## 编译条件

每个平台应用通过 `-DEFINE` 预处理器符号来选择正确的实现：

- **Windows**: `WINDOWS`
- **Linux**: `LINUX`
- **macOS**: `MACOS`
- **Android**: `ANDROID`

## 运行方式

### Windows
```bash
# 编辑器模式（默认）
SharpFAI.Editor.Platform.Windows.exe

# 仅播放器模式
SharpFAI.Editor.Platform.Windows.exe --mode=player

# 编辑器模式
SharpFAI.Editor.Platform.Windows.exe --mode=editor
```

### Linux/macOS
```bash
# 编辑器模式（默认）
./SharpFAI.Editor.Platform.Linux
./SharpFAI.Editor.Platform.macOS

# 仅播放器模式
./SharpFAI.Editor.Platform.Linux --mode=player
./SharpFAI.Editor.Platform.macOS --mode=player

# 编辑器模式
./SharpFAI.Editor.Platform.Linux --mode=editor
./SharpFAI.Editor.Platform.macOS --mode=editor
```

### Android
通过 UI 菜单切换编辑器和播放器模式

## 架构设计

### 1. 窗口架构（部分类）
- **PlayerWindow**: 播放器窗口基类，实现 `IGameWindow` 接口
  - `PlayerWindow.Playback.cs`: 播放控制方法（StartPlay、PausePlay、ResetPlayer 等）
  - `PlayerWindow.UI.cs`: ImGui 渲染（控制面板、关卡信息、关于窗口等）
- **EditorWindow**: 编辑器窗口，继承 PlayerWindow
  - `EditorWindow.Playback.cs`: 编辑器特定的播放方法
  - `EditorWindow.Editing.cs`: 编辑功能（输入处理、地板选择、摄像机控制等）
  - `EditorWindow.UI.cs`: 编辑器 UI 渲染（菜单栏、面板、状态栏等）

### 2. 音频架构
- **IAudioProvider**: 简化的音频提供者接口
  - 仅包含 `LoadAudio(string filePath)` 方法，返回 `IMusic` 实例
- **IMusic**: 音乐播放接口
  - 处理所有播放操作：Play、Pause、Stop、Resume、Seek、Dispose
  - 属性：Duration、Position、Volume、Pitch、IsPlaying、IsPaused、IsLooping
- **Desktop 实现**: `Music.cs` 使用 LibVLC 进行音频播放
- **Android 实现**: `AndroidMusic.cs` 使用 Android MediaPlayer

### 3. 平台实现
- **DesktopAudioProvider**: 桌面平台音频提供者，返回 Music 实例
- **AndroidAudioProvider**: Android 平台音频提供者，返回 AndroidMusic 实例
- **MainApplication**: 应用程序编排器，管理图形上下文、音频和窗口生命周期

### 4. 平台应用
- 各平台应用仅包含最小化的入口点（`Program.cs` 或 `MainActivity.cs`）
- 创建平台特定的实现并注入到 `MainApplication`

## 编译

```bash
# 构建 Windows 版本
dotnet build src/SharpFAI.Editor.Platform/Windows/SharpFAI.Editor.Platform.Windows.csproj

# 构建 Linux 版本
dotnet build src/SharpFAI.Editor.Platform/Linux/SharpFAI.Editor.Platform.Linux.csproj

# 构建 macOS 版本
dotnet build src/SharpFAI.Editor.Platform/macOS/SharpFAI.Editor.Platform.macOS.csproj

# 构建 Android 版本
dotnet build src/SharpFAI.Editor.Platform/Android/SharpFAI.Editor.Platform.Android.csproj

# 构建所有平台
dotnet build SharpFAI.Editor.sln
```

## 项目状态

### ✅ 已完成
- 多平台架构实现（Windows、Linux、macOS、Android）
- 平台抽象层（IGraphicsContext、IAudioProvider 等）
- 各平台入口点和实现
- 应用程序模式支持（EditorOnly、PlayerOnly、Combined）
- 统一的解决方案文件
- PlayerWindow 和 EditorWindow 部分类实现
- 简化的 IAudioProvider 接口和 IMusic 播放接口
- Desktop 音频实现（LibVLC）
- Android 音频实现（MediaPlayer）

### 🔄 进行中
- 编辑器功能完善
- 播放器功能完善
- UI 优化

### 📋 待完成
- 测试框架建立
- 文档编写
- 性能优化
- 编辑器高级功能（撤销/重做、快捷键等）
