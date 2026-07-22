using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFAI.Editor.Core.Platform.Graphics;

namespace SharpFAI.Editor.Core.Platform.Desktop
{
    /// <summary>
    /// Desktop 平台的图形上下文实现 (Windows/Linux/macOS)
    /// 使用 OpenTK GameWindow 作为底层
    /// </summary>
    public class DesktopGraphicsContext : GameWindow, Graphics.IGraphicsContext
    {
        public new event Action<double>? RenderFrame;
        public new event Action? Load;
        public new event Action? Unload;
        public new event Action<int, int>? Resize;
        public event Action<KeyboardKeyEventArgs>? KeyInput;
        public event Action<MouseButtonEventArgs>? MouseInput;
        public event Action<MouseWheelEventArgs>? MouseWheel;
        public new event Action? Minimized;
        public new event Action<double>? UpdateFrame;

        public DesktopGraphicsContext(string title = "SharpFAI", int width = 1280, int height = 720)
            : base(GameWindowSettings.Default, new NativeWindowSettings
            {
                Title = title,
                ClientSize = (width, height),
                WindowBorder = WindowBorder.Fixed,
                Vsync = VSyncMode.On,
            })
        {
        }

        #region 属性实现

        bool Graphics.IGraphicsContext.IsClosing => base.IsExiting;

        int Graphics.IGraphicsContext.Width => Size.X;
        int Graphics.IGraphicsContext.Height => Size.Y;

        string Graphics.IGraphicsContext.Title
        {
            get => Title;
            set => Title = value;
        }

        VSyncMode Graphics.IGraphicsContext.VSync
        {
            get => VSync;
            set => VSync = value;
        }

        bool Graphics.IGraphicsContext.IsVisible
        {
            get => IsVisible;
            set => IsVisible = value;
        }

        bool Graphics.IGraphicsContext.IsFullscreen
        {
            get => WindowState == WindowState.Fullscreen;
            set => WindowState = value ? WindowState.Fullscreen : WindowState.Normal;
        }

        int Graphics.IGraphicsContext.X
        {
            get => Location.X;
            set => Location = (value, Location.Y);
        }

        int Graphics.IGraphicsContext.Y
        {
            get => Location.Y;
            set => Location = (Location.X, value);
        }

        bool Graphics.IGraphicsContext.IsFocused => IsFocused;

        IKeyboardState Graphics.IGraphicsContext.KeyboardState => new DesktopKeyboardState(base.KeyboardState);

        IMouseState Graphics.IGraphicsContext.MouseState => new DesktopMouseState(base.MouseState);

        #endregion

        #region 核心操作

        void Graphics.IGraphicsContext.MakeCurrent() => base.MakeCurrent();
        void Graphics.IGraphicsContext.SwapBuffers() => base.SwapBuffers();
        void Graphics.IGraphicsContext.ProcessEvents() => base.ProcessEvents(0);
        void Graphics.IGraphicsContext.Start() => base.Run();
        #endregion
        
        #region 窗口操作

        void Graphics.IGraphicsContext.SetSize(int width, int height) => Size = (width, height);
        void Graphics.IGraphicsContext.SetPosition(int x, int y) => Location = (x, y);
        void Graphics.IGraphicsContext.Show() => IsVisible = true;
        void Graphics.IGraphicsContext.Hide() => IsVisible = false;
        void Graphics.IGraphicsContext.Close() => Close();

        #endregion

        #region GameWindow 事件处理

        protected override void OnLoad()
        {
            base.OnLoad();
            Load?.Invoke();
        }

        protected override void OnUnload()
        {
            Unload?.Invoke();
            base.OnUnload();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            UpdateFrame?.Invoke(args.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            RenderFrame?.Invoke(args.Time);
        }

        protected override void OnResize(ResizeEventArgs args)
        {
            base.OnResize(args);
            Resize?.Invoke(args.Size.X, args.Size.Y);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            KeyInput?.Invoke(e);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);
            KeyInput?.Invoke(e);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            MouseInput?.Invoke(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            MouseInput?.Invoke(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            MouseWheel?.Invoke(e);
        }

        #endregion
    }
}