using Android.Views;
using OpenTK.Windowing.Common;
using SharpFAI.Editor.Core.Platform.Graphics;
using IGraphicsContext = SharpFAI.Editor.Core.Platform.Graphics.IGraphicsContext;

namespace SharpFAI.Editor.Platform.Android;

/// <summary>
/// Android 平台的图形上下文实现
/// 使用 GLSurfaceView 作为底层
/// </summary>
public class AndroidGraphicsContext : IGraphicsContext
{
    private readonly ImGuiView _glSurfaceView;
    private bool _isClosing;
    private int _width = 1280;
    private int _height = 720;
    private string _title = "SharpFAI";
    private VSyncMode _vSync = VSyncMode.On;
    private bool _isVisible = true;
    private bool _isFullscreen;
    private int _x;
    private int _y;

    private readonly AndroidKeyboardState _keyboardState = new();
    private readonly AndroidMouseState _mouseState = new();

    public event Action<double>? RenderFrame;
    public event Action? Load;
    public event Action? Unload;
    public event Action<int, int>? Resize;
    public event Action<KeyboardKeyEventArgs>? KeyInput;
    public event Action<MouseButtonEventArgs>? MouseInput;
    public event Action<MouseWheelEventArgs>? MouseWheel;
    public event Action? Minimized;
    public event Action<double>? UpdateFrame;

    public AndroidGraphicsContext(ImGuiView glSurfaceView)
    {
        _glSurfaceView = glSurfaceView ?? throw new ArgumentNullException(nameof(glSurfaceView));
        InputProcessor.Instance.SetKeyboardState(_keyboardState);
        InputProcessor.Instance.SetMouseState(_mouseState);
    }

    #region 属性实现

    public bool IsClosing => _isClosing;
    public int Width => _width;
    public int Height => _height;
    public string Title
    {
        get => _title;
        set => _title = value;
    }

    public VSyncMode VSync
    {
        get => _vSync;
        set => _vSync = value;
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            _glSurfaceView.Visibility = value ? ViewStates.Visible : ViewStates.Gone;
        }
    }

    public bool IsFullscreen
    {
        get => _isFullscreen;
        set => _isFullscreen = value;
    }

    public int X
    {
        get => _x;
        set => _x = value;
    }

    public int Y
    {
        get => _y;
        set => _y = value;
    }

    public bool IsFocused => _glSurfaceView.HasFocus;

    public IKeyboardState KeyboardState => _keyboardState;

    public IMouseState MouseState => _mouseState;

    #endregion

    #region 核心操作

    public void MakeCurrent()
    {
        // Android 上 GLSurfaceView 自动管理 GL 上下文
    }

    public void SwapBuffers()
    {
        // Android 上 GLSurfaceView 自动交换缓冲区
    }

    public void ProcessEvents()
    {
        // Android 上事件由系统处理
    }

    public void Start()
    {
        // Android 上由 Activity 管理生命周期
    }

    #endregion

    #region 窗口操作

    public void SetSize(int width, int height)
    {
        _width = width;
        _height = height;
        Resize?.Invoke(width, height);
    }

    public void SetPosition(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public void Show()
    {
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }

    public void Close()
    {
        _isClosing = true;
    }

    #endregion

    #region 事件触发

    public void OnRenderFrame(double delta)
    {
        RenderFrame?.Invoke(delta);
    }

    public void OnLoad()
    {
        Load?.Invoke();
    }

    public void OnUnload()
    {
        Unload?.Invoke();
    }

    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;
        Resize?.Invoke(width, height);
    }

    public void OnKeyInput(KeyboardKeyEventArgs args)
    {
        KeyInput?.Invoke(args);
    }

    public void OnMouseInput(MouseButtonEventArgs args)
    {
        MouseInput?.Invoke(args);
    }

    public void OnMinimized()
    {
        Minimized?.Invoke();
    }

    public void OnUpdateFrame(double delta)
    {
        UpdateFrame?.Invoke(delta);
    }

    #endregion

    public void Dispose()
    {
        _glSurfaceView?.Dispose();
    }
}
