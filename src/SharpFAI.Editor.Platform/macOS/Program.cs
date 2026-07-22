using System;
using SharpFAI.Editor.Core;
using SharpFAI.Editor.Core.Application;
using SharpFAI.Editor.Core.Framework.Assets;
using SharpFAI.Editor.Core.Platform.Desktop;
using SharpFAI.Editor.Core.Platform.System;

namespace SharpFAI.Editor.Platform.macOS
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"SharpFAI - macOS");
            Console.WriteLine($"Mode: Editor with Player\n");

            // Initialize asset manager
            AssetManager.Initialize(new DesktopAssetManager());

            // macOS 平台特定的实现
            var graphicsContext = new DesktopGraphicsContext($"SharpFAI - {Environment.OSVersion.GetSystemName()}");
            var audioProvider = new DesktopAudioProvider();

            using (var app = new MainApplication(audioProvider, graphicsContext, null))
            {
                // 使用平台特定的实现初始化应用程序
                app.Start();
            }
        }
    }
}
