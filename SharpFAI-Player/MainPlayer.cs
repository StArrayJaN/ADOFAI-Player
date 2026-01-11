using System.Drawing;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI_Player.Framework;
using SharpFAI.Events;
using SharpFAI.Framework;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI_Player;

public class MainPlayer: GameWindow, IPlayer
{
    [Note("对象变量")]
    private readonly Level level;
    private List<Floor> floors;
    private Camera2D camera2D;
    private Music music;
    private Music hitSound;
    private List<PlayerFloor> renderFloors;
    private List<PlayerFloor> playerFloors;
    private List<GLMesh> meshes;
    private GLShader shader;
    private Floor currentFloor;
    private Planet lastPlanet;
    private Planet currentPlanet;
    private Planet redPlanet;
    private Planet bluePlanet;
    private List<double> noteTimes;

    [Note("基础类型变量")]
    private bool isStarted;
    private double angle;
    private bool isCw;
    private int currentIndex;
    private bool initialized;
    private string state;
    private nint hwnd;
    private double rotationSpeed;
    private double currentTime;
    
    [Note("摄像机相关")]
    private Vector2 cameraFromPos;
    private Vector2 cameraToPos;
    private float cameraTimer;
    private float cameraSpeed = 2.0f;

    [Note("捅死ModsTag和Yqloss和翼龙和Xbodw喵")]
    [Note("这不是Main")]
    public static void Init(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (OperatingSystem.IsWindows())
            {
                Win32API.MessageBox(IntPtr.Zero, e.ExceptionObject.ToString(),"SharpFAI Player", 0);
            }
        };
        NativeWindowSettings gameWindowSettings = new NativeWindowSettings
        {
            Title = "SharpFAI Main Player",
            Flags = ContextFlags.Default,
            APIVersion = new Version(3, 3),
            Vsync = VSyncMode.Off
        };
        MainPlayer? player = null;
        if (OperatingSystem.IsLinux())
        {
            if (string.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("请输入关卡文件路径(.adofai)");
                return;
            }
            player = new MainPlayer(GameWindowSettings.Default, gameWindowSettings, args[0]);
        }
        else
        {
            player = new MainPlayer(GameWindowSettings.Default, gameWindowSettings, Win32API.OpenFileDialog("关卡文件(*.adofai)\0*.adofai"));
        }
        player.Run();
    }
    
    public MainPlayer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings,string levelPath) 
        : base(gameWindowSettings, nativeWindowSettings)
    {
        level = new Level(levelPath);
    }

    #region GL LifeCycle Methods 
    protected override void OnLoad()
    {
         CreatePlayer();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        RenderPlayer(args.Time);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        UpdatePlayer(args.Time);
    }

    protected override void OnUnload()
    {
        DestroyPlayer();
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        if (camera2D != null)
        {
            camera2D.ViewportWidth = e.Width;
            camera2D.ViewportHeight = e.Height;
        }
        // Update OpenGL viewport
        GL.Viewport(0, 0, e.Width, e.Height);
    }
    #endregion
    
    public async void CreatePlayer()
    {
        if (OperatingSystem.IsWindows())
        {
            hwnd = Win32API.GetForegroundWindow();
        }
        LevelUtils.progressCallback += (i, s) =>
        {
            state = $"{i}%:{s}";
        };
        camera2D = new (ClientSize.X, ClientSize.Y);
        camera2D.Zoom = 3;
        redPlanet = new Planet(Color.Red);
        bluePlanet = new Planet(Color.Blue);
        redPlanet.Radius = Floor.width;
        bluePlanet.Radius = Floor.width;
        lastPlanet = redPlanet;
        currentPlanet = bluePlanet;
        shader = GLShader.CreateDefault2D();
        shader.Compile();
        state = "初始化轨道";
        noteTimes = await Task.Run(() => level.GetNoteTimes());
        noteTimes = noteTimes.Select(x => x - noteTimes[0]).ToList();
        floors = await Task.Run(() => level.CreateFloors(usePositionTrack:true));
        if (!shader.IsCompiled)
        {
            Console.WriteLine(shader.CompileLog);
        }
        state = "初始化音频";
        if (OperatingSystem.IsWindows())
        {
            try
            {
                music = new (level.GetAudioPath());
                music.Volume = 0.5f;
                // Preload music to reduce playback latency / 预加载音乐以减少播放延迟
                music.Preload();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await Task.Run(() => new AudioMerger().Export("kick.wav".ExportAssets(), noteTimes,
                Path.Combine(Path.GetDirectoryName(level.pathToLevel), "hitSound.wav")));
            hitSound = new (Path.Combine(Path.GetDirectoryName(level.pathToLevel), "hitSound.wav"));
            // Preload hitsound to reduce playback latency / 预加载命中音效以减少播放延迟
            hitSound.Preload();
        }
        state = "生成轨道网格";

        playerFloors = await Task.Run(() => floors.Select(x =>
        {
            GLTexture texture = null;
            Twirl twirl = null;
            SetSpeed setSpeed = null;
            foreach (var e in x.events)
            {
                if (e.EventType == EventType.Twirl) twirl = e.ToEvent<Twirl>();
                if (e.EventType == EventType.SetSpeed) setSpeed = e.ToEvent<SetSpeed>();
            }

            return new PlayerFloor(x);
        }).ToList());
        state = "排序轨道";
        renderFloors = playerFloors.OrderBy(x => x.floor.renderOrder).ToList();
        currentFloor = floors[0];
        cameraFromPos = currentFloor.position;
        cameraToPos = currentFloor.position;
        camera2D.Position = currentFloor.position;
        cameraTimer = 0f;
        initialized = true;
        state = "初始化完成，按空格播放，按R重新播放";
        if (OperatingSystem.IsWindows())
        {
            Win32API.ShowWindow(hwnd, 3);
        }
    }

    public void UpdatePlayer(double delta)
    {
        // Manual camera control with mouse / 用鼠标手动控制摄像机
        if (MouseState.IsButtonDown(MouseButton.Left))
        {
            Vector2 deltaMove = new Vector2(-MouseState.Delta.X, MouseState.Delta.Y);
            camera2D.Position += deltaMove;
            cameraFromPos += deltaMove;
            cameraToPos += deltaMove;
        }
        if (initialized)
        {
            if (KeyboardState.IsKeyPressed(Keys.Space) && !isStarted)
            {
                StartPlay();
            }
        
            if (KeyboardState.IsKeyPressed(Keys.R) && isStarted)
            {
                ResetPlayer();
                StartPlay();
            }

            if (isStarted)
            {
                currentTime += delta;
                while (currentIndex < noteTimes.Count -1 && currentTime >= noteTimes[currentIndex] / 1000)
                {
                    currentIndex++;
                    MoveToNextFloor(floors[currentIndex]);
                }
            }
            // Update rotation / 更新旋转
            rotationSpeed = (isCw ? -1 : 1) * (currentFloor.bpm / 60f) * 180 * delta;
            angle += rotationSpeed;
            if (angle >= 360) angle = 0;
            isCw = currentFloor.isCW;
            
            float bpm = (float)currentFloor.bpm;
            cameraSpeed = (60f / bpm) * 2f; // crotchet * 2
            
            cameraTimer += (float)delta;
            
            float distance = Vector2.Distance(cameraFromPos, cameraToPos);
            float speedMultiplier = 1.0f;
            if (distance > 5f)
            {
                float distanceFactor = FloatMath.Min(1.0f, (distance - 5f) / 5f);
                speedMultiplier = distanceFactor * 0.5f + 1f;
            }
        
            // Lerp camera position / 插值摄像机位置
            float t = cameraTimer / (cameraSpeed / speedMultiplier);
            if (t > 1.0f) t = 1.0f;
        
            camera2D.Position = Vector2.Lerp(cameraFromPos, cameraToPos, t);
        
            // 更新摄像机状态，包括震动效果
            camera2D.Update((float)delta);
        }
    }

    public void RenderPlayer(double delta)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        shader.Use();
        camera2D.Render(shader);
        if (initialized)
        {
            for (int i = 0; i < renderFloors.Count; i++)
            {
                var floor = renderFloors[i];
                if (camera2D.IsPointVisible(new (floor.floor.position.X,floor.floor.position.Y)))
                {
                    floor.Render(shader);
                }
            }
            // Update and render planets / 更新并渲染星球
            currentPlanet.Update((float)delta);
            lastPlanet.Update((float)delta);
            
            currentPlanet.Position = currentFloor.position;
            
            Vector2 offset = new Vector2(
                FloatMath.Cos(angle, true) * Floor.length * 2,
                FloatMath.Sin(angle, true) * Floor.length * 2
            );
            lastPlanet.Position = offset + currentFloor.position;
        
            bluePlanet.Render(shader, camera2D);
            redPlanet.Render(shader, camera2D);
        }
        Title = $"SharpFAI Player - FPS: {1 / (float)delta:F2} - 状态: {state} - Floor: {currentIndex}/{floors?.Count-1}";
        SwapBuffers();
    }

    public void MoveToNextFloor(Floor next)
    {
        if (currentIndex >= floors.Count) {
            return;
        }
        
        // Mark previous floor as hit (hide it) / 标记前一个地板为已命中（隐藏它）
        if (currentIndex > 0 && currentIndex < playerFloors.Count)
        {
            playerFloors[currentIndex - 1].isHit = true;
        }
        
        currentIndex = next.index;
        if (next.isMidspin) {
            // Hide the midspin floor itself / 隐藏中旋地板本身
            if (next.index < playerFloors.Count)
            {
                playerFloors[next.index].isHit = true;
            }
            currentIndex++;
        }
        currentFloor = floors[currentIndex];
        
        if (!currentFloor.lastFloor.isMidspin)
        {
            (currentPlanet, lastPlanet) = (lastPlanet, currentPlanet);
        }
        
        angle = (currentFloor.lastFloor.angle + 180).Fmod(360);
        
        cameraFromPos = camera2D.Position;
        cameraToPos = currentFloor.position;
        cameraTimer = 0f;
    }

    public void StartPlay()
    {
        _ = StartPlayAsync();
    }

    private async Task StartPlayAsync()
    {
        int offset = level.GetSetting<int>("offset");
        
        // Start both audio tracks simultaneously with precise timing
        // 同时启动两个音轨，使用精确计时
        if (offset > 0)
        {
            // If there's an offset, start music first, then hitsound after delay
            // 如果有偏移，先启动音乐，然后延迟后启动命中音效
            music?.Play();
            
            // Use high-resolution timer for more accurate delay
            // 使用高精度计时器以获得更准确的延迟
            var startTime = DateTime.UtcNow;
            var targetDelay = TimeSpan.FromMilliseconds(offset);
            
            while (DateTime.UtcNow - startTime < targetDelay)
            {
                // Spin-wait for more precise timing (last 5ms)
                // 自旋等待以获得更精确的计时（最后5毫秒）
                if (targetDelay - (DateTime.UtcNow - startTime) < TimeSpan.FromMilliseconds(5))
                {
                    System.Threading.Thread.SpinWait(100);
                }
                else
                {
                    await Task.Delay(1);
                }
            }
            
            hitSound?.Play();
        }
        else
        {
            // No offset, start both simultaneously
            // 无偏移，同时启动
            hitSound?.Play();
            music?.Play();
        }
        
        isStarted = true;
    }

    public void StopPlay()
    {
        isStarted = false;
        ResetPlayer();
    }

    public void PausePlay()
    {
        isStarted = false;
    }

    public void ResumePlay()
    {
        isStarted = true;
    }

    public void ResetPlayer()
    {
        currentFloor = floors[0];
        angle = 0;
        isStarted = false;
        currentIndex = 0;
        currentTime = 0;
        music?.Stop();
        hitSound?.Stop();
        
        // Reset all floors to visible / 重置所有地板为可见
        foreach (var floor in playerFloors)
        {
            floor.isHit = false;
        }
    }

    public void DestroyPlayer()
    {
        isStarted = false;
    }
}