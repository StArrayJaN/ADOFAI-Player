using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Audio;

/// <summary>
/// High-precision audio synchronization manager (similar to Unity AudioSettings.dspTime)
/// 高精度音频同步管理器（类似 Unity AudioSettings.dspTime）
///
/// Features:
/// - Precise time tracking from audio source
/// - Automatic drift correction
/// - Configurable sync threshold
/// - Frame-based time accumulation with audio source verification
/// </summary>
public class AudioSyncManager
{
    private IMusic? _music;
    private double _dspTime; // Precise playback time
    private double _lastMusicPosition; // Last known music position
    private double _syncThreshold; // Threshold for resync (in seconds)
    private bool _isPlaying;
    private double _pausedTime; // Time when paused

    /// <summary>
    /// Get current DSP time (precise playback time)
    /// 获取当前 DSP 时间（精准播放时间）
    /// </summary>
    public double DspTime => _dspTime;

    /// <summary>
    /// Get current music position from audio source
    /// 获取当前音乐位置（从音频源）
    /// </summary>
    public double MusicPosition => _music?.Position ?? 0;

    /// <summary>
    /// Get sync error (difference between DSP time and music position)
    /// 获取同步误差（DSP 时间与音乐位置的差异）
    /// </summary>
    public double SyncError => Math.Abs(_dspTime - MusicPosition);

    /// <summary>
    /// Whether audio is currently playing
    /// 音频是否正在播放
    /// </summary>
    public bool IsPlaying => _isPlaying && (_music?.IsPlaying ?? false);

    /// <summary>
    /// Create audio sync manager
    /// 创建音频同步管理器
    /// </summary>
    /// <param name="music">Music instance to sync with / 要同步的音乐实例</param>
    /// <param name="syncThreshold">Sync threshold in seconds (default 0.05s = 50ms) / 同步阈值（秒）</param>
    public AudioSyncManager(IMusic music, double syncThreshold = 0.05)
    {
        _music = music;
        _syncThreshold = Math.Max(0.01, syncThreshold); // Minimum 10ms
        _dspTime = 0;
        _lastMusicPosition = 0;
        _isPlaying = false;
        _pausedTime = 0;
    }

    /// <summary>
    /// Start playback and initialize DSP time
    /// 开始播放并初始化 DSP 时间
    /// </summary>
    public void Play()
    {
        if (_music == null) return;

        _music.Play();
        _dspTime = _music.Position;
        _lastMusicPosition = _music.Position;
        _isPlaying = true;
    }

    /// <summary>
    /// Pause playback
    /// 暂停播放
    /// </summary>
    public void Pause()
    {
        if (_music == null) return;

        _music.Pause();
        _pausedTime = _dspTime;
        _isPlaying = false;
    }

    /// <summary>
    /// Resume playback
    /// 恢复播放
    /// </summary>
    public void Resume()
    {
        if (_music == null) return;

        _music.Resume();
        _isPlaying = true;
    }

    /// <summary>
    /// Stop playback and reset time
    /// 停止播放并重置时间
    /// </summary>
    public void Stop()
    {
        if (_music == null) return;

        _music.Stop();
        _dspTime = 0;
        _lastMusicPosition = 0;
        _isPlaying = false;
        _pausedTime = 0;
    }

    /// <summary>
    /// Seek to specific position
    /// 跳转到指定位置
    /// </summary>
    public void Seek(double position)
    {
        if (_music == null) return;

        _music.Seek(position);
        _dspTime = position;
        _lastMusicPosition = position;
    }

    /// <summary>
    /// Update DSP time (call once per frame)
    /// 更新 DSP 时间（每帧调用一次）
    /// </summary>
    /// <param name="deltaTime">Frame delta time in seconds / 帧增量时间（秒）</param>
    public void Update(double deltaTime)
    {
        if (_music == null || !_isPlaying)
            return;

        double musicPosition = _music.Position;

        // Detect if music position jumped (seek, restart, or buffer underrun)
        double positionDelta = musicPosition - _lastMusicPosition;

        if (Math.Abs(positionDelta) > _syncThreshold)
        {
            // Position changed significantly, resync to audio source
            _dspTime = musicPosition;
        }
        else if (positionDelta < 0)
        {
            // Position went backwards (shouldn't happen in normal playback)
            _dspTime = musicPosition;
        }
        else
        {
            // Normal playback, accumulate time
            _dspTime += deltaTime;
        }

        _lastMusicPosition = musicPosition;

        // Clamp to music duration
        if (_music.Duration > 0 && _dspTime > _music.Duration)
        {
            _dspTime = _music.Duration;
        }
    }

    /// <summary>
    /// Force resync to audio source (useful for debugging or recovery)
    /// 强制重新同步到音频源（用于调试或恢复）
    /// </summary>
    public void ForceResync()
    {
        if (_music == null) return;

        _dspTime = _music.Position;
        _lastMusicPosition = _music.Position;
    }

    /// <summary>
    /// Get sync statistics for debugging
    /// 获取同步统计信息（用于调试）
    /// </summary>
    public (double dspTime, double musicPosition, double error, bool isPlaying) GetSyncStats()
    {
        return (_dspTime, MusicPosition, SyncError, IsPlaying);
    }
}
