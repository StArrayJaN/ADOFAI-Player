using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace SharpFAI_Player;

public class Program
{
    public static void Main(string[] args)
    {
        NativeWindowSettings gameWindowSettings = new NativeWindowSettings
        {
            Title = "SharpFAI Main Player",
            Flags = ContextFlags.Default,
            APIVersion = new Version(3, 3),
            Vsync = VSyncMode.Off
        };
        ImGuiGameWindow window = new ImGuiGameWindow(GameWindowSettings.Default,gameWindowSettings);
        window.Run();
        //MainPlayer.Init(args);
    }
}