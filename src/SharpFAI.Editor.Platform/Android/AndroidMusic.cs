using Android.Media;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Platform.Android;

/// <summary>
/// Android 平台音乐实现，使用 Android MediaPlayer
/// Android platform music implementation using Android MediaPlayer
/// </summary>
public class AndroidMusic : IMusic, IDisposable
{
    private MediaPlayer _mediaPlayer;
    private bool _disposed;
    private string _currentFile;
    private float _volume = 1.0f;
    private bool _isLooping = false;

    /// <summary>
    /// Get current playback position in seconds / 获取当前播放位置（秒）
    /// </summary>
    public double Position
    {
        get
        {
            if (_mediaPlayer == null)
                return 0;
            return _mediaPlayer.CurrentPosition / 1000.0; // Convert ms to seconds
        }
    }

    /// <summary>
    /// Get total duration in seconds / 获取总时长（秒）
    /// </summary>
    public double Duration
    {
        get
        {
            if (_mediaPlayer == null)
                return 0;
            return _mediaPlayer.Duration / 1000.0; // Convert ms to seconds
        }
    }

    /// <summary>
    /// Music volume (0.0 - 1.0) / 音乐音量（0.0 - 1.0）
    /// </summary>
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetVolume(_volume, _volume);
            }
        }
    }

    /// <summary>
    /// Music pitch multiplier / 音乐音调倍数
    /// </summary>
    public float Pitch { get; set; } = 1.0f;

    /// <summary>
    /// Whether the music is playing / 音乐是否正在播放
    /// </summary>
    public bool IsPlaying
    {
        get => _mediaPlayer?.IsPlaying ?? false;
    }

    /// <summary>
    /// Whether the music is paused / 音乐是否暂停
    /// </summary>
    public bool IsPaused
    {
        get => _mediaPlayer != null && !_mediaPlayer.IsPlaying && _mediaPlayer.CurrentPosition > 0;
    }

    /// <summary>
    /// Whether the music is looping / 音乐是否循环
    /// </summary>
    public bool IsLooping
    {
        get => _isLooping;
        set
        {
            _isLooping = value;
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Looping = value;
            }
        }
    }

    /// <summary>
    /// Create a new AndroidMusic instance / 创建新的音乐实例
    /// </summary>
    public AndroidMusic()
    {
        _mediaPlayer = new MediaPlayer();
    }

    /// <summary>
    /// Create a new AndroidMusic instance with file / 使用文件创建新的音乐实例
    /// </summary>
    public AndroidMusic(string path) : this()
    {
        Load(path);
    }

    /// <summary>
    /// Load audio file / 加载音频文件
    /// </summary>
    public void Load(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Audio file not found: {path}");

        try
        {
            _currentFile = path;
            _mediaPlayer.SetDataSource(path);
            _mediaPlayer.Prepare();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load audio file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Preload audio to reduce playback latency / 预加载音频以减少播放延迟
    /// </summary>
    public void Preload()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        // Android MediaPlayer is already prepared in Load()
        // No additional preload needed
    }

    /// <summary>
    /// Play the music / 播放音乐
    /// </summary>
    public void Play()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        if (_mediaPlayer == null)
            throw new InvalidOperationException("No audio file loaded. Call Load() first.");

        if (!_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Start();
        }
    }

    /// <summary>
    /// Pause the music / 暂停音乐
    /// </summary>
    public void Pause()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        if (_mediaPlayer?.IsPlaying ?? false)
        {
            _mediaPlayer.Pause();
        }
    }

    /// <summary>
    /// Stop the music / 停止音乐
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();
            _mediaPlayer.Prepare();
        }
    }

    /// <summary>
    /// Resume the music / 恢复音乐
    /// </summary>
    public void Resume()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        if (_mediaPlayer != null && !_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Start();
        }
    }

    /// <summary>
    /// Seek to a specific position / 跳转到指定位置
    /// </summary>
    public void Seek(double position)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidMusic));

        if (_mediaPlayer == null)
            return;

        int clampedPosition = (int)Math.Clamp(position * 1000, 0, _mediaPlayer.Duration);
        _mediaPlayer.SeekTo(clampedPosition);
    }

    /// <summary>
    /// Update music state / 更新音乐状态
    /// </summary>
    public void Update()
    {
        // Android MediaPlayer handles playback asynchronously
        // This method can be used for future extensions
    }

    /// <summary>
    /// Dispose music resources / 释放音乐资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_mediaPlayer != null)
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
            }
            _mediaPlayer.Release();
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~AndroidMusic()
    {
        Dispose();
    }
}