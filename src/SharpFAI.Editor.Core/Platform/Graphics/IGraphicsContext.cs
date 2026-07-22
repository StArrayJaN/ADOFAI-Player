using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI.Editor.Core.Platform.Graphics
{
    /// <summary>
    /// 可取消事件参数
    /// </summary>
    public class CancelEventArgs : EventArgs
    {
        /// <summary>
        /// 获取或设置是否取消操作
        /// </summary>
        public bool Cancel { get; set; }
    }
    /// <summary>
    /// 统一的图形上下文接口
    /// 负责处理平台特定的窗口和 OpenGL 上下文
    /// </summary>
    public interface IGraphicsContext : IDisposable
    {
        #region 核心操作

        /// <summary>
        /// 使该上下文成为当前线程的上下文
        /// </summary>
        void MakeCurrent();

        /// <summary>
        /// 交换前后缓冲区
        /// </summary>
        void SwapBuffers();

        /// <summary>
        /// 处理待处理的事件
        /// </summary>
        void ProcessEvents();

        /// <summary>
        /// 启动图形上下文
        /// </summary>
        void Start();
        #endregion

        #region 窗口状态属性

        /// <summary>
        /// 获取窗口是否正在关闭
        /// </summary>
        bool IsClosing { get; }

        /// <summary>
        /// 获取窗口宽度
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 获取窗口高度
        /// </summary>
        int Height { get; }

        /// <summary>
        /// 获取或设置窗口标题
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// 获取或设置垂直同步模式
        /// </summary>
        VSyncMode VSync { get; set; }

        /// <summary>
        /// 获取或设置窗口是否可见
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// 获取或设置窗口是否全屏
        /// </summary>
        bool IsFullscreen { get; set; }

        /// <summary>
        /// 获取或设置窗口位置 (X坐标)
        /// </summary>
        int X { get; set; }

        /// <summary>
        /// 获取或设置窗口位置 (Y坐标)
        /// </summary>
        int Y { get; set; }

        /// <summary>
        /// 获取窗口是否获得焦点
        /// </summary>
        bool IsFocused { get; }

        /// <summary>
        /// 获取键盘状态
        /// </summary>
        IKeyboardState KeyboardState { get; }

        /// <summary>
        /// 获取鼠标状态
        /// </summary>
        IMouseState MouseState { get; }

        #endregion

        #region 窗口操作

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        void SetSize(int width, int height);

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        void SetPosition(int x, int y);

        /// <summary>
        /// 显示窗口
        /// </summary>
        void Show();

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        void Hide();

        /// <summary>
        /// 关闭窗口
        /// </summary>
        void Close();

        #endregion

        #region 事件

        /// <summary>
        /// 渲染帧事件
        /// </summary>
        event Action<double> RenderFrame;

        /// <summary>
        /// 加载完成事件
        /// </summary>
        event Action Load;

        /// <summary>
        /// 卸载事件
        /// </summary>
        event Action Unload;

        /// <summary>
        /// 窗口大小改变事件
        /// </summary>
        event Action<int, int> Resize;

        /// <summary>
        /// 键盘输入事件
        /// </summary>
        event Action<KeyboardKeyEventArgs> KeyInput;
        
        /// <summary>
        /// 鼠标输入事件
        /// </summary>
        event Action<MouseButtonEventArgs> MouseInput;

        /// <summary>
        /// 鼠标滚轮事件
        /// </summary>
        event Action<MouseWheelEventArgs> MouseWheel;

        /// <summary>
        /// 窗口最小化事件
        /// </summary>
        event Action Minimized;

        /// <summary>
        /// 更新帧事件（在渲染前调用）
        /// </summary>
        event Action<double> UpdateFrame;

        #endregion
    }
}
