using System;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Platform.Audio
{
    /// <summary>
    /// 平台特定的音频提供者接口
    /// Platform-specific audio provider interface
    /// </summary>
    public interface IAudioProvider : IDisposable
    {
        /// <summary>
        /// 加载音频文件
        /// Load audio file
        /// </summary>
        IMusic LoadAudio(string filePath);
    }
}