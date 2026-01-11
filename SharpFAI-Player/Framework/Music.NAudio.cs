using NAudio.Vorbis;
using NAudio.Wave;
using SharpFAI.Framework;

namespace SharpFAI_Player.Framework;

/// <summary>
/// Music implementation using NAudio with full OGG support (BACKUP)
/// 使用NAudio的音乐实现，完整支持OGG（备份）
/// </summary>
public class MusicNAudio : IMusic
{
    private AudioFileReader _audioFileReader;
    private VorbisWaveReader _vorbisReader;
    private WaveOutEvent _outputDevice;
    private bool _disposed;
    private string _currentFile;
    private bool _isOgg;
    private bool _isPreloaded;

    /// <summary>
    /// Get current playback position in seconds / 获取当前播放位置（秒）
    /// </summary>
    public double Position
    {
        get
        {
            if (_isOgg && _vorbisReader != null)
            {
                return _vorbisReader.CurrentTime.TotalSeconds;
            }
            return _audioFileReader?.CurrentTime.TotalSeconds ?? 0;
        }
    }

    /// <summary>
    /// Get total duration in seconds / 获取总时长（秒）
    /// </summary>
    public double Duration
    {
        get
        {
            if (_isOgg && _vorbisReader != null)
            {
                return _vorbisReader.TotalTime.TotalSeconds;
            }
            return _audioFileReader?.TotalTime.TotalSeconds ?? 0;
        }
    }

    /// <summary>
    /// Music volume (0.0 - 1.0) / 音乐音量（0.0 - 1.0）
    /// </summary>
    public float Volume
    {
        get => _outputDevice?.Volume ?? 1.0f;
        set
        {
            if (_outputDevice != null)
            {
                _outputDevice.Volume = Math.Clamp(value, 0.0f, 1.0f);
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
        get => _outputDevice?.PlaybackState == PlaybackState.Playing;
    }

    /// <summary>
    /// Whether the music is paused / 音乐是否暂停
    /// </summary>
    public bool IsPaused
    {
        get => _outputDevice?.PlaybackState == PlaybackState.Paused;
    }

    /// <summary>
    /// Whether the music is looping / 音乐是否循环
    /// </summary>
    public bool IsLooping { get; set; } = false;

    /// <summary>
    /// Create a new Music instance / 创建新的音乐实例
    /// </summary>
    public MusicNAudio()
    {
    }

    /// <summary>
    /// Create a new Music instance with file / 使用文件创建新的音乐实例
    /// </summary>
    public MusicNAudio(string path)
    {
        Load(path);
    }

    /// <summary>
    /// Load audio file / 加载音频文件
    /// </summary>
    public void Load(string path)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Audio file not found: {path}");

        // Dispose existing resources
        CleanupResources();

        try
        {
            _currentFile = path;
            _isOgg = path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase);
            _outputDevice = new WaveOutEvent
            {
                // Reduce latency by using smaller buffer
                // 通过使用更小的缓冲区来减少延迟
                DesiredLatency = 100 // 100ms latency (default is 300ms)
            };

            if (_isOgg)
            {
                _vorbisReader = new VorbisWaveReader(path);
                _outputDevice.Init(_vorbisReader);
            }
            else
            {
                _audioFileReader = new AudioFileReader(path);
                _outputDevice.Init(_audioFileReader);
            }

            // Setup looping
            _outputDevice.PlaybackStopped += OnPlaybackStopped;
            _isPreloaded = false;
        }
        catch (Exception ex)
        {
            CleanupResources();
            throw new InvalidOperationException($"Failed to load audio file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Preload audio to reduce playback latency / 预加载音频以减少播放延迟
    /// </summary>
    public void Preload()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        if (_outputDevice == null)
            throw new InvalidOperationException("No audio file loaded. Call Load() first.");

        if (_isPreloaded)
            return;

        try
        {
            // Initialize the output device buffer by playing and immediately pausing
            // This ensures the audio pipeline is ready
            // 通过播放并立即暂停来初始化输出设备缓冲区
            // 这确保音频管道已准备就绪
            _outputDevice.Play();
            System.Threading.Thread.Sleep(10); // Allow buffer to fill
            _outputDevice.Pause();
            
            // Reset position
            if (_isOgg && _vorbisReader != null)
            {
                _vorbisReader.CurrentTime = TimeSpan.Zero;
            }
            else if (_audioFileReader != null)
            {
                _audioFileReader.Position = 0;
            }

            _isPreloaded = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Preload warning: {ex.Message}");
            // Non-critical error, continue anyway
        }
    }

    /// <summary>
    /// Play the music / 播放音乐
    /// </summary>
    public void Play()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        if (_outputDevice == null)
            throw new InvalidOperationException("No audio file loaded. Call Load() first.");

        // If not preloaded, do a quick preload
        // 如果未预加载，进行快速预加载
        if (!_isPreloaded)
        {
            Preload();
        }

        _outputDevice.Play();
    }

    /// <summary>
    /// Pause the music / 暂停音乐
    /// </summary>
    public void Pause()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        _outputDevice?.Pause();
    }

    /// <summary>
    /// Stop the music / 停止音乐
    /// </summary>
    public void Stop()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        _outputDevice?.Stop();

        // Reset position for both formats
        if (_isOgg && _vorbisReader != null)
        {
            _vorbisReader.CurrentTime = TimeSpan.Zero;
        }
        else if (_audioFileReader != null)
        {
            _audioFileReader.Position = 0;
        }
    }

    /// <summary>
    /// Resume the music / 恢复音乐
    /// </summary>
    public void Resume()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        if (_outputDevice?.PlaybackState == PlaybackState.Paused)
        {
            _outputDevice.Play();
        }
    }

    /// <summary>
    /// Seek to a specific position / 跳转到指定位置
    /// </summary>
    public void Seek(double position)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MusicNAudio));

        double clampedPosition = Math.Clamp(position, 0, Duration);

        if (_isOgg && _vorbisReader != null)
        {
            _vorbisReader.CurrentTime = TimeSpan.FromSeconds(clampedPosition);
        }
        else if (_audioFileReader != null)
        {
            _audioFileReader.CurrentTime = TimeSpan.FromSeconds(clampedPosition);
        }
    }

    /// <summary>
    /// Update music state / 更新音乐状态
    /// </summary>
    public void Update()
    {
        // This method can be used for future extensions
        // Currently, NAudio handles playback asynchronously
    }

    /// <summary>
    /// Handle playback stopped event / 处理播放停止事件
    /// </summary>
    private void OnPlaybackStopped(object sender, StoppedEventArgs e)
    {
        if (IsLooping && !_disposed)
        {
            // Reset position based on file type
            if (_isOgg && _vorbisReader != null)
            {
                _vorbisReader.CurrentTime = TimeSpan.Zero;
            }
            else if (_audioFileReader != null)
            {
                _audioFileReader.Position = 0;
            }

            _outputDevice?.Play();
        }
    }

    /// <summary>
    /// Cleanup audio resources / 清理音频资源
    /// </summary>
    private void CleanupResources()
    {
        if (_outputDevice != null)
        {
            _outputDevice.PlaybackStopped -= OnPlaybackStopped;
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }

        if (_audioFileReader != null)
        {
            _audioFileReader.Dispose();
            _audioFileReader = null;
        }

        if (_vorbisReader != null)
        {
            _vorbisReader.Dispose();
            _vorbisReader = null;
        }

        _isOgg = false;
        _isPreloaded = false;
    }

    /// <summary>
    /// Dispose music resources / 释放音乐资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        CleanupResources();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~MusicNAudio()
    {
        Dispose();
    }
}
