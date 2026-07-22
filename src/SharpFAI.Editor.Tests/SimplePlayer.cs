using System.Drawing;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Editor.Core.Framework.Assets;
using SharpFAI.Editor.Core.Framework.Graphics;
using SharpFAI.Editor.Core.Models;
using SharpFAI.Events;
using SharpFAI.Framework;
using SharpFAI.Serialization;
using SharpFAI.Util;
using Vector2 = System.Numerics.Vector2;

namespace SharpFAI.Editor.Tests;

/// <summary>
/// 最小化的 SimplePlayer，只包含基本的渲染功能
/// 用于测试纹理渲染等核心功能
/// </summary>
public class SimplePlayer : GameWindow
{
    private Level? level;
    private List<Floor> floors;
    private List<PlayerFloor> renderFloors;
    private Camera2D camera2D;
    private GLShader shader;
    private GLShader textureShader;
    private GLTexture swirlRed;
    private GLTexture speedUp;
    private GLTexture speedDown;
    private bool initialized;
    
    public SimplePlayer(string? levelPath = null) : base(
        GameWindowSettings.Default,
        new NativeWindowSettings
        {
            Title = "SimplePlayer - 最小化测试播放器",
            ClientSize = (1280, 720),
            Flags = ContextFlags.ForwardCompatible,
            Profile = ContextProfile.Core,
            APIVersion = new Version(3, 3)
        })
    {
        level = Level.CreateNewLevel();
    }
    
    protected override void OnLoad()
    {
        base.OnLoad();
        
        // 初始化 OpenGL
        GL.ClearColor(0.05f, 0.05f, 0.05f, 1.0f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        // 创建摄像机
        camera2D = new Camera2D(ClientSize.X, ClientSize.Y)
        {
            Zoom = 3f
        };
        
        // 创建着色器
        shader = GLShader.CreateDefault2D();
        shader.Compile();
        
        // 加载纹理着色器
        try
        {
            string texVertSource = AssetManager.LoadText(Path.Combine("shaders", "texture2d.vert"));
            string texFragSource = AssetManager.LoadText(Path.Combine("shaders", "texture2d.frag"));
            textureShader = new GLShader(texVertSource, texFragSource);
            textureShader.Compile();
            Console.WriteLine("纹理着色器加载成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"纹理着色器加载失败：{ex.Message}");
        }
        
        // 加载纹理
        LoadTextures();
        // 初始化关卡
        if (level != null)
        {
            InitializeLevel();
        }
    }
    
    private void LoadTextures()
    {
        try
        {
            swirlRed = new GLTexture();
            swirlRed.Load("swirl_red.png");
            Console.WriteLine($"加载 swirl_red.png: {swirlRed.IsLoaded}, 尺寸：{swirlRed.Width}x{swirlRed.Height}");

            speedUp = new GLTexture();
            speedUp.Load("tile_rabbit_light_new0.png");
            Console.WriteLine($"加载 tile_rabbit_light_new0.png: {speedUp.IsLoaded}, 尺寸：{speedUp.Width}x{speedUp.Height}");

            speedDown = new GLTexture();
            speedDown.Load("tile_snail_light_new0.png");
            Console.WriteLine($"加载 tile_snail_light_new0.png: {speedDown.IsLoaded}, 尺寸：{speedDown.Width}x{speedDown.Height}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"纹理加载失败：{ex.Message}");
        }
    }
    
    private void InitializeLevel()
    {
        try
        {
            Console.WriteLine("开始初始化关卡...");
            level = Level.CreateNewLevel();
            // 创建地板
            floors = level.CreateFloors(usePositionTrack: true);
            Console.WriteLine($"创建了 {floors.Count} 个地板");
            
            // 创建 PlayerFloor 并应用纹理
            renderFloors = floors.Select(x =>
            {
                var floor = new PlayerFloor(x);
                Twirl twirl = null;
                SetSpeed setSpeed = null;
                
                foreach (var e in x.events)
                {
                    if (e.EventType == EventType.Twirl) twirl = e.ToEvent<Twirl>();
                    if (e.EventType == EventType.SetSpeed) setSpeed = e.ToEvent<SetSpeed>();
                }

                if (twirl != null)
                {
                    floor.isCW = !floor.isCW;
                    floor.texture = swirlRed;
                    Console.WriteLine($"应用 Twirl 纹理到地板 {x.position}");
                }
                if (setSpeed != null)
                {
                    if (setSpeed.SpeedType == EventEnums.SpeedType.Multiplier)
                        floor.texture = setSpeed.BpmMultiplier > 1f ? speedUp : speedDown;
                    else
                        floor.texture = setSpeed.BeatsPerMinute > (x.lastFloor?.bpm ?? x.bpm) ? speedUp : speedDown;
                    Console.WriteLine($"应用 SetSpeed 纹理到地板 {x.position}");
                }
                return floor;
            }).OrderBy(x => x.floor.renderOrder).ToList();
            
            
            camera2D.Position = Vector2.Zero;

            initialized = true;
            Console.WriteLine("关卡初始化完成");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"关卡初始化失败：{ex}");
            initialized = false;
        }
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        // 清屏
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // 渲染游戏
        RenderPlayer(args.Time);
        
        SwapBuffers();
    }
    
    public void RenderPlayer(double delta)
    {
        if (!initialized || shader == null || camera2D == null)
            return;

        // 渲染地板
        shader.Use();
        camera2D.Render(shader);
        shader.SetMatrix4x4("uModel", Matrix4x4.Identity);

        for (int i = 0; i < renderFloors.Count; i++)
        {
            var floor = renderFloors[i];
            if (camera2D.IsPointVisible(new Vector2(floor.floor.position.X, floor.floor.position.Y)))
            {
                floor.Render(shader);
            }
        }

        // 渲染纹理覆盖层
        if (textureShader != null && textureShader.IsCompiled)
        {
            textureShader.Use();
            camera2D.Render(textureShader);
            textureShader.SetMatrix4x4("uModel", Matrix4x4.Identity);
            GL.ActiveTexture(TextureUnit.Texture0);

            for (int i = 0; i < renderFloors.Count; i++)
            {
                var floor = renderFloors[i];
                if (camera2D.IsPointVisible(new Vector2(floor.floor.position.X, floor.floor.position.Y)))
                {
                    if (floor.texture != null && floor.texture.IsLoaded)
                    {
                        floor.RenderTextureOverlay(textureShader);
                    }
                }
            }
        }
    }
    
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        
        // 简单的摄像机控制
        if (KeyboardState.IsKeyDown(Keys.W))
            camera2D.Position += new Vector2(0, 0.1f);
        if (KeyboardState.IsKeyDown(Keys.S))
            camera2D.Position -= new Vector2(0, 0.1f);
        if (KeyboardState.IsKeyDown(Keys.A))
            camera2D.Position -= new Vector2(0.1f, 0);
        if (KeyboardState.IsKeyDown(Keys.D))
            camera2D.Position += new Vector2(0.1f, 0);
    }
    
    protected override void OnUnload()
    {
        // 清理资源
        shader?.Dispose();
        textureShader?.Dispose();
        swirlRed?.Dispose();
        speedUp?.Dispose();
        speedDown?.Dispose();
        
        foreach (var floor in renderFloors)
        {
            floor?.Dispose();
        }
        base.OnUnload();
    }
    
    public static void TestSimplePlayer()
    {
        var player = new SimplePlayer();
        AssetManager.Initialize(new DesktopAssetManager());
        player.Run();
    }
}