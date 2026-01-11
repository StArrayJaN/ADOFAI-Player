using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI_Player;

/// <summary>
/// GameWindow with ImGui integration for OpenTK
/// 集成 ImGui 的 OpenTK GameWindow
/// </summary>
public class ImGuiGameWindow : GameWindow
{
    private ImGuiController? _imGuiController;
    private bool _showDemoWindow = true;
    private bool _showDebugWindow = true;
    private System.Numerics.Vector4 _clearColor = new System.Numerics.Vector4(0.1f, 0.1f, 0.1f, 1.0f);

    public ImGuiGameWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // Initialize OpenGL
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Initialize ImGui
        _imGuiController = new ImGuiController(ClientSize.X, ClientSize.Y);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Clear screen
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Update ImGui
        _imGuiController?.Update(this, (float)args.Time);

        // Render ImGui UI
        RenderImGui();

        // Render ImGui
        _imGuiController?.Render();

        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Handle input
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        _imGuiController?.WindowResized(e.Width, e.Height);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        _imGuiController?.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _imGuiController?.MouseScroll(e.Offset);
    }

    private void RenderImGui()
    {
        // Demo window
        if (_showDemoWindow)
        {
            ImGui.ShowDemoWindow(ref _showDemoWindow);
        }

        // Debug window
        if (_showDebugWindow)
        {
            ImGui.Begin("Debug Info", ref _showDebugWindow);
            
            ImGui.Text($"FPS: {1.0 / UpdateTime:F1}");
            ImGui.Text($"Frame Time: {UpdateTime * 1000:F2} ms");
            ImGui.Text($"Window Size: {ClientSize.X} x {ClientSize.Y}");
            
            ImGui.Separator();
            
            if (ImGui.ColorEdit4("Clear Color", ref _clearColor))
            {
                GL.ClearColor(_clearColor.X, _clearColor.Y, _clearColor.Z, _clearColor.W);
            }
            
            ImGui.Separator();
            
            ImGui.Checkbox("Show Demo Window", ref _showDemoWindow);
            
            if (ImGui.Button("Close Application"))
            {
                Close();
            }
            
            ImGui.End();
        }

        // Main menu bar
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Exit", "ESC"))
                {
                    Close();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("View"))
            {
                ImGui.MenuItem("Demo Window", null, ref _showDemoWindow);
                ImGui.MenuItem("Debug Window", null, ref _showDebugWindow);
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }

    protected override void OnUnload()
    {
        _imGuiController?.Dispose();
        base.OnUnload();
    }
}
