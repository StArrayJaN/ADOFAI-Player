using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SharpFAI.Editor.Core;
using SharpFAI.Editor.Core.Framework.Assets;
using SharpFAI.Editor.Core.Platform.Desktop;
using SharpFAI.Editor.Core.Platform.System;
using SharpFAI.Editor.Core.Player;

namespace SharpFAI.Editor.Platform.Windows
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine($"SharpFAI - Windows");
            Console.WriteLine($"Mode: Editor with Player\n");

            // Initialize asset manager
            AssetManager.Initialize(new DesktopAssetManager());

            
            if (true)
            {
                MainPlayer player = new MainPlayer(GameWindowSettings.Default, new NativeWindowSettings()
                {
                    Title = "SharpFAI - Player",
                    ClientSize = (1280, 720),
                    WindowBorder = WindowBorder.Resizable,
                    Vsync = VSyncMode.Off
                });
                player.Run();
            }
            /*var graphicsContext = new DesktopGraphicsContext($"SharpFAI - {Environment.OSVersion.GetSystemName()}");
            var audioProvider = new DesktopAudioProvider();
            using (var app = new MainApplication(audioProvider, graphicsContext, null))
            {
                // 使用平台特定的实现初始化应用程序
                app.Start();
            }*/
        }
    }
}
