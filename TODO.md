# SharpFAI 任务清单

## 📁 当前项目结构（已实现）

```
SharpFAI.Editor/
├── SharpFAI.Editor.sln                      # 统一解决方案
│
├── src/
│   ├── SharpFAI.Editor.Core/               # 共享核心库（已实现）
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
│   │   ├── Editor/                         # 编辑器逻辑
│   │   ├── Framework/                      # 框架组件
│   │   │   ├── Audio/                      # 音频处理
│   │   │   │   └── Music.cs                # Desktop 音频实现（LibVLC）
│   │   │   ├── Graphics/                   # 图形渲染
│   │   │   └── Particles/                  # 粒子系统
│   │   ├── Game/                           # 游戏逻辑
│   │   ├── Models/                         # 数据模型
│   │   ├── Player/                         # 播放器逻辑
│   │   ├── Platform/                       # 平台接口
│   │   │   └── Audio/                      # 音频接口
│   │   │       ├── IAudioProvider.cs       # 音频提供者接口
│   │   │       └── IMusic.cs               # 音乐播放接口
│   │   ├── UI/                             # 用户界面
│   │   ├── Util/                           # 工具函数
│   │   ├── MainApplication.cs              # 应用程序主类
│   │   └── SharpFAI.Editor.Core.csproj     # 核心库项目文件
│   │
│   └── SharpFAI.Editor.Platform/           # 平台层（已实现）
│       ├── Windows/                        # Windows 应用（已实现）
│       │   └── Program.cs                  # Windows 入口点
│       ├── Linux/                          # Linux 应用（已实现）
│       │   └── Program.cs                  # Linux 入口点
│       ├── macOS/                          # macOS 应用（已实现）
│       │   └── Program.cs                  # macOS 入口点
│       └── Android/                        # Android 应用（已实现）
│           └── MainActivity.cs             # Android 入口点
│
├── tests/                                  # 测试项目（待实现）
└── docs/                                   # 文档（待实现）
```

### 编译输出
```
bin/
├── Windows/
│   └── SharpFAI.Editor.Platform.Windows.exe  # Windows 可执行文件
├── Linux/
│   └── SharpFAI.Editor.Platform.Linux        # Linux 可执行文件
├── macOS/
│   └── SharpFAI.Editor.Platform.macOS        # macOS 可执行文件
└── Android/
    └── SharpFAI.Editor.Platform.Android.apk  # Android 安装包
```

### 运行方式
```bash
# Windows
SharpFAI.Editor.Platform.Windows.exe                 # 默认编辑器模式
SharpFAI.Editor.Platform.Windows.exe --mode=player   # 仅播放器模式
SharpFAI.Editor.Platform.Windows.exe --mode=editor   # 仅编辑器模式

# Linux
./SharpFAI.Editor.Platform.Linux --mode=player
./SharpFAI.Editor.Platform.Linux --mode=editor

# macOS
./SharpFAI.Editor.Platform.macOS --mode=player
./SharpFAI.Editor.Platform.macOS --mode=editor

# Android
# 通过 UI 菜单切换编辑器和播放器模式
```

### 项目状态验证
- ✅ 多平台架构已实现
- ✅ 平台抽象层已实现（IGraphicsContext, IAudioProvider等）
- ✅ 各平台入口点已实现（Windows, Linux, macOS, Android）
- ✅ 应用程序模式支持（EditorOnly, PlayerOnly, Combined）
- ✅ 统一的解决方案文件（SharpFAI.Editor.sln）

---

## 🏗️ 架构重构与多平台支持（已完成）

### 第一阶段：项目结构规划 ✅
- [x] 分析现有编辑器和播放器分离的代码结构
- [x] 设计统一架构：编辑器+播放器合并，单一可执行文件
- [x] 规划目录结构（Core、UI、Platform、Apps）
- [x] 规划平台支持：Windows、Linux、macOS、Android
- [x] 确定编译条件和预处理符号（#if WINDOWS/LINUX/MACOS/ANDROID）
- [x] 规划跨平台 UI 框架（ImGUI + OpenTK）

### 第二阶段：平台抽象层 ✅
#### 图形上下文
- [x] 创建 `IGraphicsContext` 接口
- [x] 实现 `DesktopGraphicsContext`（Windows/Linux/macOS）
  - [x] 包装 OpenTK GameWindow
  - [x] 事件循环处理
- [x] 实现 `AndroidGraphicsContext`
  - [x] 包装 GLSurfaceView
  - [x] Android 事件处理
- [x] 创建 `PlatformFactory` 工厂类（已弃用，各平台自行实现）

#### 音频系统
- [x] 创建 `IAudioProvider` 接口
- [x] 实现 Desktop 音频提供者
- [x] 实现 Android 音频提供者
- [x] 共享 `AudioManager` 逻辑

#### 输入系统
- [x] 创建 `IInputManager` 接口
- [ ] 实现各平台输入处理（待完善）

#### 文件系统
- [x] 创建 `IFileProvider` 接口
- [ ] 处理各平台路径差异（待完善）

### 第三阶段：项目重构 ✅
- [x] 创建新项目结构
  - [x] SharpFAI.Editor.Core（核心库）
  - [x] SharpFAI.Editor.Platform.Abstractions（平台抽象层）
  - [x] SharpFAI.Editor.Platform.Windows（Windows 可执行）
  - [x] SharpFAI.Editor.Platform.Linux（Linux 可执行）
  - [x] SharpFAI.Editor.Platform.macOS（macOS 可执行）
  - [x] SharpFAI.Editor.Platform.Android（Android APK）
- [x] 迁移现有代码到新结构
- [x] 实现模式切换逻辑（--mode=player/editor）
- [ ] 配置 ImGUI 跨平台集成（待实现）

### 第四阶段：功能验证（进行中）
- [x] Windows 编译和测试
- [x] Linux 编译和测试
- [x] macOS 编译和测试
- [x] Android 编译和测试
- [x] 迁移 MainPlayer 和 EditorPlayer 逻辑到 PlayerWindow 和 EditorWindow（部分类）
- [x] 实现 IAudioProvider 简化接口（仅 LoadAudio 方法）
- [x] 实现 IMusic 接口用于音频播放控制
- [x] 创建 Music.cs（Desktop 使用 LibVLC）
- [x] 创建 AndroidMusic.cs（Android 使用 MediaPlayer）
- [ ] 编辑器模式功能验证（待完善）
- [ ] 播放器模式功能验证（待完善）
- [x] 模式切换验证

---

## 🪟 窗口架构重构（已完成）

### PlayerWindow 实现 ✅
- [x] 创建 PlayerWindow.cs 作为 IGameWindow 实现
- [x] 创建 PlayerWindow.Playback.cs（播放控制方法）
  - [x] StartPlay, PausePlay, ResumePlay, ResetPlayer
  - [x] MoveToNextFloor, DestroyPlayer
- [x] 创建 PlayerWindow.UI.cs（ImGui 渲染）
  - [x] RenderControlPanel（控制面板）
  - [x] RenderLevelInfo（关卡信息）
  - [x] RenderAboutWindow（关于窗口）
  - [x] RenderForegroundInfo（前景信息显示）
  - [x] OpenLevelFile（打开关卡文件）

### EditorWindow 实现 ✅
- [x] 创建 EditorWindow.cs 继承 PlayerWindow
- [x] 创建 EditorWindow.Playback.cs（编辑器播放方法）
  - [x] UpdatePlayerPlayback（播放更新）
  - [x] StartPlayEditor, PausePlayEditor, ResumePlayEditor
  - [x] ResetPlayerEditor, MoveToNextFloorEditor
- [x] 创建 EditorWindow.Editing.cs（编辑功能）
  - [x] HandleEditorInput（输入处理）
  - [x] HandleCameraDrag（摄像机拖拽）
  - [x] HandleMouseWheelZoom（鼠标滚轮缩放）
  - [x] ScreenToWorld（屏幕坐标转世界坐标）
  - [x] HandleFloorClick（地板点击处理）
- [x] 创建 EditorWindow.UI.cs（编辑器 UI）
  - [x] RenderEditorUI（编辑器 UI 主方法）
  - [x] RenderMenuBar（菜单栏）
  - [x] RenderMainLayout（主布局）
  - [x] RenderSettingsPanelWithTabs（设置面板）
  - [x] RenderEventInfoPanelWithTabs（事件信息面板）
  - [x] RenderEventSetPanelWithTabs（事件集面板）
  - [x] RenderTrackButtons（轨道操作按钮）
  - [x] RenderStatusBar（状态栏）
- [x] 实现 SaveLevel 和 SaveLevelAs 方法
- [x] 实现 SelectFloor 地板选择逻辑

### 音频架构重构 ✅
- [x] 简化 IAudioProvider 接口（仅 LoadAudio 方法）
- [x] 创建 IMusic 接口用于音频播放控制
- [x] 实现 Music.cs（Desktop 使用 LibVLC）
  - [x] Load, Play, Pause, Stop, Resume, Seek, Preload
  - [x] Duration, Position, Volume, Pitch 属性
- [x] 实现 AndroidMusic.cs（Android 使用 MediaPlayer）
- [x] 更新 DesktopAudioProvider 返回 Music 实例
- [x] 更新 AndroidAudioProvider 返回 AndroidMusic 实例
- [x] 移除 _audioProvider.GetDuration 等调用
- [x] 直接通过 IMusic 对象调用播放方法

---

### 轨道编辑功能
- [ ] 实现选择多个轨道并绿色高亮
- [ ] 实现添加轨道功能
- [ ] 实现删除轨道功能
- [ ] 实现拖拽轨道功能
- [ ] 实现复制、粘贴轨道功能
- [ ] 实现鼠标右键添加轨道功能
- [ ] 实现添加轨道按钮

### 事件系统
- [ ] 实现添加事件功能
- [ ] 实现删除事件功能
- [ ] 实现事件信息修改功能
- [ ] 实现添加事件按钮

### 播放与预览
- [ ] 实现从选择的轨道开始播放
- [ ] 实现手打模式

### 数据管理
- [ ] 实现保存关卡功能

### 用户界面优化
- [ ] 优化 UI 布局，提升用户体验
- [ ] 添加快捷键支持
- [ ] 实现撤销/重做功能
- [ ] 添加搜索和过滤功能

---

## 🎮 播放器功能

### 基础功能
- [ ] 关卡加载和显示
- [ ] 音频播放
- [ ] 轨道渲染
- [ ] 基础播放控制

### 高级功能
- [ ] 多语言支持
- [ ] 配置系统
- [ ] 统计信息显示

---

## 🐛 Bug 修复与优化
- [ ] 修复音频同步问题
- [ ] 处理各平台文件路径差异
- [ ] 优化 OpenGL 性能

---

## 📝 已完成的任务
- [x] 项目从 SharpFAI-Player 重命名为 SharpFAI-Editor
- [x] 修复 PowerShell 编码问题
- [x] 分析现有代码结构
- [x] 规划统一架构（编辑器+播放器）
- [x] 规划多平台支持结构
- [x] 实现基本的关卡加载和显示
- [x] 实现基础播放功能
- [x] 实现 UI 界面
- [x] 实现轨道渲染
- [x] 实现音频播放
- [x] 实现多平台架构
- [x] 实现平台抽象层（IGraphicsContext, IAudioProvider等）
- [x] 实现各平台入口点（Windows, Linux, macOS, Android）
- [x] 实现应用程序模式支持（EditorOnly, PlayerOnly, Combined）
- [x] 创建统一的解决方案文件（SharpFAI.Editor.sln）
- [x] 实现 SharpFAIApplication 基类管理图形和音频