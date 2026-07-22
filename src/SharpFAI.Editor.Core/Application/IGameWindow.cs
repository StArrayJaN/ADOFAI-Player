
using OpenTK.Windowing.Common;
using SharpFAI.Editor.Core.Platform.Audio;

namespace SharpFAI.Editor.Core.Application;

public interface IGameWindow : IDisposable
{
    void RenderFrame(double delta);
    void UpdateFrame(double delta);
    void LoadWindow();
    void CloseWindow();
    void ResizeWindow(int width, int height);
    void OnKeyEvent(KeyboardKeyEventArgs e);
    void OnMouseEvent(MouseButtonEventArgs e);
    void OnMouseWheel(MouseWheelEventArgs e);
    void Minimized();

    SharpFAI.Editor.Core.Platform.Graphics.IGraphicsContext GraphicsContext { get; set; }
    IAudioProvider AudioProvider { get; set; }
}