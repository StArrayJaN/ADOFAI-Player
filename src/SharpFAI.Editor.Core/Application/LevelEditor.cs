using System.Drawing;
using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Editor.Core.Framework.Audio;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Editor.Core.Models;
using SharpFAI.Editor.Core.Platform.Audio;
using SharpFAI.Editor.Core.Platform.FileProvider;
using SharpFAI.Editor.Core.UI;
using SharpFAI.Editor.Core.Util;
using SharpFAI.Framework;
using SharpFAI.Serialization;
using SharpFAI.Util;
using IGraphicsContext = SharpFAI.Editor.Core.Platform.Graphics.IGraphicsContext;

namespace SharpFAI.Editor.Core.Application;

/// <summary>
/// 关卡编辑器 - 完整的编辑和播放功能
/// 从 EditorPlayer 迁移，实现 IGameWindow 和 IPlayer
/// 跨平台实现，不依赖于GameWindow
/// </summary>
public partial class LevelEditor : IGameWindow, IPlayer
{
    #region ImGui Fields
    private ImGuiController? _imGuiController;
    private readonly Vector4 _clearColor = new(0.05f, 0.05f, 0.05f, 1.0f);
    #endregion

    #region Editor UI State
    private bool _showAboutWindow;

    // 面板折叠状态
    private bool _leftPanelCollapsed;
    private bool _rightPanelCollapsed;
    private bool _bottomPanelCollapsed;

    // 双击检测（简化为单个标签）
    private double _lastLeftTabClickTime;
    private double _lastRightTabClickTime;
    private double _lastBottomTabClickTime;
    private const double DoubleClickTime = 0.3;
    #endregion

    #region Level Data
    private Level? _level;
    private string? _levelPath;
    private List<Floor>? _floors;
    private List<int> _selectedFloorIndices = new();
    private List<Floor> _selectedFloors = new();

    // 渲染相关
    private List<PlayerFloor>? _playerFloors;
    private int[] _cachedRenderOrder = Array.Empty<int>(); // 缓存的渲染顺序索引
    private bool _needRenderOrderUpdate = true; // 标记是否需要更新渲染顺序
    private GLShader? _shader;
    private Camera2D? _camera2D;
    private bool _initialized;
    private bool _needsGLInitialization; // 标记需要在主线程初始化 OpenGL 对象
    #endregion

    #region Editor State
    private string _statusMessage = "就绪 Ready";

    // 可调整的面板宽度
    private float _leftPanelWidth = 350f;
    private float _rightPanelWidth = 350f;
    private float _bottomPanelHeight = 200f;
    #endregion

    #region Playback State
    private bool _isPlaying;
    private bool _showPlanets; // 控制星球显示但不影响时间进度
    private double _currentTime;
    private double _playbackSpeed = 1.0;
    private int _currentIndex;
    private Floor? _currentFloor;

    // 音频
    private Music? _music;
    private Music? _hitSound;
    private List<double>? _noteTimes;

    // 音频设置
    private string _musicFilePath = "";
    private float _bpm = 120f;
    private float _pitch = 100f;

    // 星球
    private Planet? _redPlanet;
    private Planet? _bluePlanet;
    private Planet? _currentPlanet;
    private Planet? _lastPlanet;

    // 旋转
    private double _angle;
    private bool _isCw;
    private double _rotationSpeed;

    // 摄像机跟随
    private Vector2 _cameraFromPos;
    private Vector2 _cameraToPos;
    private float _cameraTimer;
    private float _cameraSpeed = 2.0f;
    #endregion

    #region Input State
    private bool _isShiftPressed;
    private bool _isButtonWindowHovered; // 标记鼠标是否在按钮窗口上
    private OpenTK.Mathematics.Vector2 _lastScrollDelta = OpenTK.Mathematics.Vector2.Zero; // 缓存上一帧的滚轮值
    #endregion

    #region IGameWindow Properties
    public IGraphicsContext GraphicsContext { get; set; } = null!;
    public IAudioProvider AudioProvider { get; set; } = null!;

    // 用于 FPS 计算的更新时间（秒）
    private double _updateTime = 0.016; // 默认 60 FPS
    public double UpdateTime => _updateTime;
    #endregion

    public LevelEditor(string? levelPath = null)
    {
        _levelPath = levelPath;

        // 如果没有提供关卡路径，创建一个新的默认关卡
        // If no level path is provided, create a new default level
        if (string.IsNullOrEmpty(levelPath))
        {
            _level = Level.CreateNewLevel();
            _statusMessage = "已创建新关卡 New level created";
        }
    }

    #region IGameWindow Implementation
    public void LoadWindow()
    {
        GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _imGuiController = new ImGuiController(GraphicsContext.Width, GraphicsContext.Height);

        // 初始化摄像机
        _camera2D = new Camera2D(GraphicsContext.Width, GraphicsContext.Height);
        _camera2D.Zoom = 3f;

        // 初始化着色器
        _shader = GLShader.CreateDefault2D();
        _shader.Compile();

        if (!string.IsNullOrEmpty(_levelPath))
        {
            LoadLevel(_levelPath);
        }
        else if (_level != null)
        {
            // 如果有默认关卡，初始化它
            // If there's a default level, initialize it
            LoadLevel(null);
        }
        else
        {
            _statusMessage = "请打开关卡文件或使用默认关卡 Please open a level file or use default level";
        }
    }

    public void CloseWindow()
    {
        Dispose();
    }

    public void ResizeWindow(int width, int height)
    {
        if (_camera2D != null)
        {
            _camera2D.ViewportWidth = width;
            _camera2D.ViewportHeight = height;
        }
        _imGuiController?.WindowResized(width, height);
    }

    public void OnKeyEvent(KeyboardKeyEventArgs e)
    {
        // 键盘事件处理
        // 可以在这里处理需要立即响应的键盘事件
        // 大多数输入处理在 UpdateFrame 中进行
    }

    public void OnMouseEvent(MouseButtonEventArgs e)
    {
        // 鼠标事件处理
        // 在这里处理鼠标按钮事件
    }

    public void OnMouseWheel(MouseWheelEventArgs e)
    {
        // 鼠标滚轮事件处理
        // 缓存滚轮值供 ImGui 使用
        _lastScrollDelta = new OpenTK.Mathematics.Vector2(e.OffsetX, e.OffsetY);

        // 同时传递给 ImGui
        _imGuiController?.MouseScroll(new OpenTK.Mathematics.Vector2(e.OffsetX, e.OffsetY));

        // 处理摄像机缩放（仅在 ImGui 不需要鼠标输入时）
        var io = ImGui.GetIO();
        if (!io.WantCaptureMouse && _camera2D != null)
        {
            HandleMouseWheelZoom();
        }
    }

    public void Minimized()
    {
        // 最小化处理
    }

    public void RenderFrame(double delta)
    {
        // 如果需要初始化 OpenGL 对象，在渲染循环中执行（确保在主线程）
        if (_needsGLInitialization)
        {
            InitializeGLObjects();
        }

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // 渲染轨道
        RenderTracks();

        // 渲染播放器
        RenderPlayer(delta);

        if (_imGuiController != null)
        {
            try
            {
                // ImGui.Update 已经在 UpdateFrame 中调用
                RenderEditorUi();
                _imGuiController.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ImGui render error: {ex}");
            }
        }

        GraphicsContext.SwapBuffers();
    }

    public void UpdateFrame(double delta)
    {
        // 更新时间用于 FPS 计算
        _updateTime = delta;

        // 先更新 ImGui 以获取最新的 IO 状态
        if (_imGuiController != null)
        {
            _imGuiController.Update(GraphicsContext, (float)delta);
        }

        // ImGui 是否捕获输入（现在是最新状态）
        var io = ImGui.GetIO();
        var imguiWantsMouse = io.WantCaptureMouse;
        var imguiWantsKeyboard = io.WantCaptureKeyboard;
        var imguiWantsTextInput = io.WantTextInput;

        // ESC 键停止播放（仅在 ImGui 不需要键盘输入时处理）
        if (GraphicsContext.KeyboardState.IsKeyPressed(Keys.Escape) && !imguiWantsKeyboard && !imguiWantsTextInput)
        {
            ResetPlayer();
        }

        // Shift 状态
        _isShiftPressed = GraphicsContext.KeyboardState.IsKeyDown(Keys.LeftShift) || GraphicsContext.KeyboardState.IsKeyDown(Keys.RightShift);

        // 输入处理（仅在 ImGui 不需要任何输入时）
        if (!imguiWantsMouse && !imguiWantsKeyboard && !imguiWantsTextInput)
        {
            HandleInput();
        }
        else if (imguiWantsTextInput || imguiWantsKeyboard)
        {
            // ImGui 正在处理文本输入，清除游戏输入状态
            _isDragging = false;
            _wasMouseMoving = false;
        }

        // 更新摄像机
        _camera2D?.Update((float)delta);

        // 更新星球（拖尾效果）
        _currentPlanet?.Update((float)delta);
        _lastPlanet?.Update((float)delta);

        // 更新播放
        UpdatePlayer(delta);
    }

    public void Dispose()
    {
        // 停止播放
        _isPlaying = false;

        // 释放音频资源
        _music?.Dispose();
        _music = null;
        _hitSound?.Dispose();
        _hitSound = null;

        // 释放轨道资源
        if (_playerFloors != null)
        {
            foreach (var floor in _playerFloors)
            {
                floor?.Dispose();
            }
            _playerFloors = null;
        }
        _cachedRenderOrder = Array.Empty<int>();
        _needRenderOrderUpdate = true;

        // 释放星球
        _redPlanet?.Dispose();
        _bluePlanet?.Dispose();
        _redPlanet = null;
        _bluePlanet = null;
        _lastPlanet = null;
        _currentPlanet = null;

        // 释放着色器
        _shader?.Dispose();
        _shader = null;

        _imGuiController?.Dispose();
    }
    #endregion
}
