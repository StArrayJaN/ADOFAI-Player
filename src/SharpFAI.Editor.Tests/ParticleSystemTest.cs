using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Editor.Core.Framework.Assets;
using SharpFAI.Editor.Core.Models;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Util;
using Vector2 = System.Numerics.Vector2;

namespace SharpFAI.Editor.Tests;

public class ParticleSystemTest : GameWindow
{
    private Camera2D? _camera;
    private GLShader? _shader;
    private Planet? _planet1;
    private Planet? _planet2;
    private Stopwatch _stopwatch;

    private const float RPM = 100f;
    private const float RotationsPerSecond = RPM / 60f;
    private const float OrbitRadius = 8.0f;

    private Vector2 _lastMousePos = Vector2.Zero;
    private bool _isDragging = false;

    private int _triangleVao;
    private int _triangleVbo;

    private bool _showTriangle = true;
    private bool _showPlanets = true;
    private bool _showParticles = true;

    // 使用 MainPlayer 的算法
    private double _angle = 0;
    private double _rotationSpeed = 0;

    public ParticleSystemTest() : base(
        GameWindowSettings.Default,
        new NativeWindowSettings
        {
            Title = "Particle System Test",
            ClientSize = (1280, 720),
            Flags = ContextFlags.ForwardCompatible
        })
    {
        _stopwatch = Stopwatch.StartNew();
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        AssetManager.Initialize(new DesktopAssetManager());

        _camera = new Camera2D(Size.X, Size.Y);

        _camera.Position = Vector2.Zero;
        _camera.Zoom = 0.5f;  // 缩小缩放，看到更大的区域

        string vertexSource = @"#version 330 core
layout(location = 0) in vec3 position;
layout(location = 1) in vec4 color;

uniform mat4 uProjectionMatrix;
uniform mat4 uViewMatrix;
uniform mat4 uModelMatrix;

out vec4 vertexColor;

void main()
{
    gl_Position = uProjectionMatrix * uViewMatrix * uModelMatrix * vec4(position, 1.0);
    vertexColor = color;
}
";

        string fragmentSource = @"#version 330 core
in vec4 vertexColor;
out vec4 FragColor;

void main()
{
    FragColor = vertexColor;
}
";

        _shader = new GLShader(vertexSource, fragmentSource);
        _shader.Compile();

        // Initialize planets after OpenGL context is ready
        _planet1 = new Planet(System.Drawing.Color.FromArgb(255, 100, 100, 255), trailEnabled: true);
        _planet2 = new Planet(System.Drawing.Color.FromArgb(255, 100, 150, 255), trailEnabled: true);

        // 计算旋转速度：100 RPM = 100 * 360 度/分钟 = 600 度/秒
        _rotationSpeed = RPM * 360.0 / 60.0;

        InitializeTriangle();

        Console.WriteLine("[Test] Ready");
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        if (e.Key == Keys.T)
        {
            _showTriangle = !_showTriangle;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _planet1?.Update((float)args.Time);
        _planet2?.Update((float)args.Time);

        if (_shader != null && _camera != null)
        {
            _shader.Use();

            // Set camera matrices once for all rendering
            Matrix4 projection = Matrix4.CreateOrthographicOffCenter(
                -_camera.ViewportWidth / 2 / _camera.Zoom,
                _camera.ViewportWidth / 2 / _camera.Zoom,
                -_camera.ViewportHeight / 2 / _camera.Zoom,
                _camera.ViewportHeight / 2 / _camera.Zoom,
                _camera.NearPlane,
                _camera.FarPlane
            );

            Matrix4 view = Matrix4.CreateTranslation(-_camera.Position.X, -_camera.Position.Y, 0);

            _shader.SetMatrix4x4("uProjectionMatrix", ConvertMatrix(projection));
            _shader.SetMatrix4x4("uViewMatrix", ConvertMatrix(view));

            if (_showPlanets)
            {
                _planet1?.Render(_shader, _camera);
                _planet2?.Render(_shader, _camera);
            }

            if (_showTriangle)
            {
                RenderTriangle();
            }
        }

        SwapBuffers();
    }

    private void InitializeTriangle()
    {
        float[] vertices = new float[]
        {
            // Position          Color
            -2.0f, -2.0f, 0.0f,  1.0f, 0.0f, 0.0f, 1.0f,  // Red vertex
             2.0f, -2.0f, 0.0f,  0.0f, 1.0f, 0.0f, 1.0f,  // Green vertex
             0.0f,  2.0f, 0.0f,  0.0f, 0.0f, 1.0f, 1.0f   // Blue vertex
        };

        _triangleVao = GL.GenVertexArray();
        GL.BindVertexArray(_triangleVao);

        _triangleVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _triangleVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        // Position attribute
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        // Color attribute
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    private void RenderTriangle()
    {
        if (_shader == null || _camera == null)
            return;

        Matrix4 model = Matrix4.Identity;
        _shader.SetMatrix4x4("uModelMatrix", ConvertMatrix(model));

        GL.BindVertexArray(_triangleVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        GL.BindVertexArray(0);
    }

    private static System.Numerics.Matrix4x4 ConvertMatrix(Matrix4 matrix)
    {
        return new System.Numerics.Matrix4x4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        );
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _planet1?.Update((float)args.Time);
        _planet2?.Update((float)args.Time);

        // 使用 MainPlayer 的算法更新角度
        _angle += _rotationSpeed * args.Time;
        if (_angle >= 360) _angle = 0;

        // 星球1在中心
        _planet1!.Position = Vector2.Zero;
        _planet1!.Radius = 3.0f;  // 增大红星球

        // 星球2绕星球1旋转，使用角度计算位置
        Vector2 offset = new Vector2(
            (float)FloatMath.Cos(_angle, true) * OrbitRadius,
            (float)FloatMath.Sin(_angle, true) * OrbitRadius
        );
        _planet2!.Position = offset;
        _planet2!.Radius = 2.5f;  // 增大蓝星球

        if ((int)_stopwatch.Elapsed.TotalSeconds % 1 == 0 && _stopwatch.Elapsed.TotalMilliseconds % 1000 < 16)
        {
            Console.WriteLine($"[Test] Angle: {_angle:F1}°, Planet1: ({_planet1.Position.X:F2}, {_planet1.Position.Y:F2}), Planet2: ({_planet2.Position.X:F2}, {_planet2.Position.Y:F2})");
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
        if (_camera != null)
        {
            _camera.ViewportWidth = Size.X;
            _camera.ViewportHeight = Size.Y;
        }
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        Vector2 currentMousePos = new Vector2(e.X, e.Y);

        if (_isDragging && _camera != null)
        {
            Vector2 delta = currentMousePos - _lastMousePos;
            // Convert screen space delta to world space
            delta /= _camera.Zoom;
            _camera.Position -= delta;
        }

        _lastMousePos = currentMousePos;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButton.Left)
        {
            _isDragging = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButton.Left)
        {
            _isDragging = false;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (_camera != null)
        {
            float zoomFactor = 1.1f;
            if (e.OffsetY > 0)
            {
                _camera.Zoom *= zoomFactor;
            }
            else
            {
                _camera.Zoom /= zoomFactor;
            }
            _camera.Zoom = Math.Max(0.1f, Math.Min(10f, _camera.Zoom));
        }
    }

    protected override void OnUnload()
    {
        _planet1?.Dispose();
        _planet2?.Dispose();
        _shader?.Dispose();
        if (_triangleVao != 0)
            GL.DeleteVertexArray(_triangleVao);
        if (_triangleVbo != 0)
            GL.DeleteBuffer(_triangleVbo);
        base.OnUnload();
    }
    
}
