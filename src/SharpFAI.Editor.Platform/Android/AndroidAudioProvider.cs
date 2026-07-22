using SharpFAI.Editor.Core.Platform.Audio;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Platform.Android;

public class AndroidAudioProvider : IAudioProvider
{
    public IMusic LoadAudio(string filePath)
    {
        var music = new AndroidMusic(filePath);
        return music;
    }

    public void Dispose() { }
}