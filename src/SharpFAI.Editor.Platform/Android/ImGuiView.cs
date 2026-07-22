using Android.Content;
using Android.Opengl;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using ImGuiNET;
using Javax.Microedition.Khronos.Opengles;
using OpenTK.Graphics.ES30;

namespace SharpFAI.Editor.Platform.Android;

/// <summary>
/// ImGui 视图 - 基于 GLSurfaceView
/// </summary>
public class ImGuiView : GLSurfaceView, GLSurfaceView.IRenderer
{
    private static readonly string Tag = nameof(ImGuiView);

    private ImGuiRenderer? imguiRenderer;
    private long lastFrameTime;
    private int surfaceWidth;
    private int surfaceHeight;
    bool lastWantTextInput = false;

    public ImGuiView(Context context) : base(context)
    {
        Init();
    }

    public ImGuiView(Context context, IAttributeSet attrs) : base(context, attrs)
    {
        Init();
    }

    private void Init()
    {
        // 设置 EGL 配置
        SetEGLContextClientVersion(3);
        SetEGLConfigChooser(8, 8, 8, 8, 16, 0);
        SetRenderer(this);
        RenderMode = Rendermode.Continuously;

        lastFrameTime = System.Environment.TickCount64;
    }

    #region GLSurfaceView.IRenderer Implementation

    public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
    {
        Log.Info(Tag, "OnSurfaceCreated");

        // 加载 OpenGL 绑定
        GL.LoadBindings(new GLBindingsContext());

        // 设置背景颜色
        GL.ClearColor(0.45f, 0.55f, 0.60f, 1.00f);

        // 创建 ImGui 渲染器
        imguiRenderer = new ImGuiRenderer();
        imguiRenderer.Init();

        // 根据屏幕密度设置缩放（可选）
        // 对于高 DPI 屏幕，可以自动调整缩放
        float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
        if (density > 1.5f)
        {
            // 对于高密度屏幕，可以设置更大的缩放
            imguiRenderer.SetGlobalScale(density * 0.8f);
            Log.Error(Tag, $"Set ImGui scale to {density * 0.8f} (density: {density})");
        }
    }

    public void OnSurfaceChanged(IGL10? gl, int width, int height)
    {
        Log.Info(Tag, $"OnSurfaceChanged: {width}x{height}");

        surfaceWidth = width;
        surfaceHeight = height;

        GL.Viewport(0, 0, width, height);
    }

    public void OnDrawFrame(IGL10? gl)
    {
        // 计算帧时间
        long currentTime = System.Environment.TickCount64;
        float deltaTime = (currentTime - lastFrameTime) / 1000.0f;
        lastFrameTime = currentTime;

        // 清除屏幕
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        if (imguiRenderer != null)
        {
            // 开始 ImGui 新帧
            imguiRenderer.NewFrame(surfaceWidth, surfaceHeight, deltaTime);

            // 绘制 ImGui UI
            DrawImGui();

            // 渲染 ImGui
            imguiRenderer.Render();
        }
        InputProcessor.Instance.OnImGUIRender(this);
    }

    #region InputEvent

    // 在输入法处理之前拦截按键
    public override bool OnKeyPreIme(Keycode keyCode, KeyEvent? e)
    {
        // 在输入法处理之前拦截删除键
        if (keyCode == Keycode.Del && e != null)
        {
            InputProcessor.Instance.ProcessKeyEvent(e);
            Log.Error(Tag, $"OnKeyPreIme: Del key intercepted, action={e.Action}");
            // 返回 true 阻止输入法处理
            return true;
        }

        return base.OnKeyPreIme(keyCode, e);
    }

    // 拦截按键事件
    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e != null && e.KeyCode == Keycode.Del)
        {
            InputProcessor.Instance.ProcessKeyEvent(e);
            Log.Error(Tag, $"DispatchKeyEvent: Del key, action={e.Action}");
            return true;
        }

        return base.DispatchKeyEvent(e);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        InputProcessor.Instance.ProcessTouchEvent(e);
        return true;
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        InputProcessor.Instance.ProcessKeyEvent(e);
        return base.OnKeyDown(keyCode, e);
    }

    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
    {
        InputProcessor.Instance.ProcessKeyEvent(e);
        return base.OnKeyUp(keyCode, e);
    }

    #endregion
    #endregion

    private string text = "Hello, World!";
    private float uiScale = 1.0f;

    /// <summary>
    /// 绘制 ImGui UI
    /// </summary>
    private void DrawImGui()
    {
        // 创建一个简单的窗口
        bool showWindow = true;
        if (ImGui.Begin("Hello ImGui!", ref showWindow, ImGuiWindowFlags.None))
        {
            ImGui.Text("Welcome to SharpFAI Editor!");
            ImGui.Separator();

            ImGui.Text($"Display Size: {surfaceWidth}x{surfaceHeight}");
            ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");

            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            ImGui.Text($"Screen Density: {density:F2}");
            if (ImGui.Button("input method"))
            {
                InputMethodManager inputMethodManager = InputMethodManager.FromContext(Context);
                inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);
            }

            ImGui.Separator();

            // UI 缩放滑块
            if (imguiRenderer != null)
            {
                uiScale = imguiRenderer.GetGlobalScale();
                if (ImGui.SliderFloat("UI Scale", ref uiScale, 0.5f, 3.0f))
                {
                    imguiRenderer.SetGlobalScale(uiScale);
                    Log.Error(Tag, $"UI Scale changed to: {uiScale}");
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Click Me!"))
            {
                Log.Info(Tag, "Button clicked!");
            }

            ImGui.SameLine();
            ImGui.Text("Button was clicked");

            ImGui.Separator();
            ImGui.InputText("Label", ref text,50);
            // 颜色选择器
            var color = new System.Numerics.Vector4(0.45f, 0.55f, 0.60f, 1.00f);
            if (ImGui.ColorEdit4("Background Color", ref color))
            {
                GL.ClearColor(color.X, color.Y, color.Z, color.W);
            }

            ImGui.End();
        }

        // 显示 ImGui Demo 窗口（可选）
        // ImGui.ShowDemoWindow();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            imguiRenderer?.Dispose();
            imguiRenderer = null;
        }
        base.Dispose(disposing);
    }
}