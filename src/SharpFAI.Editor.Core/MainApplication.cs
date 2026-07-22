using System;
using OpenTK.Windowing.Desktop;
using SharpFAI.Editor.Core.Application;
using SharpFAI.Editor.Core.Platform.Audio;
using SharpFAI.Editor.Core.Platform.Graphics;

namespace SharpFAI.Editor.Core
{
    /// <summary>
    /// SharpFAI 应用程序基类，集成 ImGUI 支持
    /// 管理图形上下文、音频和主循环，包含完整的 ImGUI 渲染
    /// </summary>
    public class MainApplication : IDisposable
    {
        protected IGameWindow? _gameWindow;
        protected (IGraphicsContext, IAudioProvider) _platform;
        protected bool _initialized = false;

    public MainApplication(IAudioProvider audioProvider, IGraphicsContext graphicsContext, string levelPath)
    {
        ArgumentNullException.ThrowIfNull(audioProvider);
        ArgumentNullException.ThrowIfNull(graphicsContext);
        // 只创建编辑器窗口
        _gameWindow = new LevelEditor(levelPath);
        graphicsContext.Load += _gameWindow.LoadWindow;
        graphicsContext.UpdateFrame += _gameWindow.UpdateFrame;
        graphicsContext.RenderFrame += _gameWindow.RenderFrame;
        graphicsContext.Unload += _gameWindow.CloseWindow;
        graphicsContext.Resize += _gameWindow.ResizeWindow;
        graphicsContext.Minimized += _gameWindow.Minimized;
        graphicsContext.KeyInput += _gameWindow.OnKeyEvent;
        graphicsContext.MouseInput += _gameWindow.OnMouseEvent;
        graphicsContext.MouseWheel += _gameWindow.OnMouseWheel;
        _gameWindow.AudioProvider = audioProvider;

        // Set GameWindow for desktop platform
        if (graphicsContext is GameWindow gameWindow)
        {
            _gameWindow.GraphicsContext = graphicsContext;
            Console.WriteLine($"[MainApplication] Set GameWindow: {gameWindow.Size.X}x{gameWindow.Size.Y}");
        }
        else
        {
            Console.WriteLine($"[MainApplication] WARNING: Could not set GameWindow. graphicsContext type: {graphicsContext.GetType().Name}, _gameWindow type: {_gameWindow.GetType().Name}");
        }

        _platform = (graphicsContext, audioProvider);
        _initialized = true;
        Console.WriteLine("[MainApplication] Initialization complete");
    }

        public void Start() => _platform.Item1.Start();

        public void Dispose()
        {
            _gameWindow?.Dispose();
            _platform.Item1.Dispose();
            _platform.Item2.Dispose();
        }
    }
}
