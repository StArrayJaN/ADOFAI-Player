using SharpFAI.Editor.Core.Framework.Audio;
using SharpFAI.Editor.Core.Platform.Audio;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Platform.Desktop
{
    /// <summary>
    /// Desktop 平台音频提供者（Windows/Linux/macOS）
    /// </summary>
    public class DesktopAudioProvider : IAudioProvider
    {
        public IMusic LoadAudio(string filePath)
        {
            var music = new Music(filePath);
            return music;
        }

        public void Dispose() { }
    }
}
