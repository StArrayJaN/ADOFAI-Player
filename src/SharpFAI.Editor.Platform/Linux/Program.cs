using System;
using SharpFAI.Editor.Core;
using SharpFAI.Editor.Core.Application;
using SharpFAI.Editor.Core.Framework.Assets;
using SharpFAI.Editor.Core.Platform.Desktop;
using SharpFAI.Editor.Core.Platform.System;

namespace SharpFAI.Editor.Platform.Linux
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"SharpFAI - Linux");
            Console.WriteLine($"Mode: Editor with Player\n");

            // Initialize asset manager
            AssetManager.Initialize(new DesktopAssetManager());

            // Linux 平台特定的实现
            var graphicsContext = new DesktopGraphicsContext($"SharpFAI - {Environment.OSVersion.GetSystemName()}");
            var audioProvider = new DesktopAudioProvider();

            using (var app = new MainApplication(audioProvider, graphicsContext, null))
            {
                app.Start();
            }
        }
    }
}
