using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Audio;

/// <summary>
/// Improved audio conductor with better synchronization
/// 改进的音频指挥，具有更好的同步机制
/// </summary>
public class ImprovedAudioConductor
{
    private IMusic? _music;
    private double _dspTime; // Current DSP time
    private double _lastDspTime; // Last frame's DSP time
    private double _bpm;
    private double _offset; // Audio offset in seconds
    private float _pitch = 1.0f;

    // Beat tracking
    private double _crotchetTime; // Time for one beat (60 / BPM)
    private double _nextBeatTime;
    private int _beatNumber;
    private int _barNumber;
    private int _crotchetsPerBar = 8;

    // Synchronization
    private double _musicStartDspTime = -1; // DSP time when music actually started
    private double _musicStartPosition = 0; // Music position when started
    private bool _isInitialized = false;

    // Events
    public event Action<int>? OnBeat;
    public event Action<int>? OnBar;

    /// <summary>
    /// Get current DSP time
    /// 获取当前 DSP 时间
    /// </summary>
    public double DspTime => _dspTime;

    /// <summary>
    /// Get song position in seconds
    /// 获取歌曲位置（秒）
    /// </summary>
    public double SongPosition
    {
        get
        {
            if (_musicStartDspTime < 0 || _music == null)
                return 0;

            // Calculate position based on actual music playback
            return _music.Position;
        }
    }

    /// <summary>
    /// Get current beat number
    /// 获取当前拍号
    /// </summary>
    public int BeatNumber => _beatNumber;

    /// <summary>
    /// Get current bar number
    /// 获取当前小节号
    /// </summary>
    public int BarNumber => _barNumber;

    /// <summary>
    /// Create improved audio conductor
    /// 创建改进的音频指挥
    /// </summary>
    public ImprovedAudioConductor(IMusic music, double bpm, double offset = 0, float pitch = 1.0f)
    {
        _music = music;
        _bpm = Math.Max(1, bpm);
        _offset = offset;
        _pitch = Math.Max(0.1f, pitch);
        _crotchetTime = 60.0 / _bpm;
        _dspTime = 0;
        _nextBeatTime = 0;
        _beatNumber = 0;
        _barNumber = 0;
    }

    /// <summary>
    /// Initialize conductor
    /// 初始化指挥
    /// </summary>
    public void Initialize()
    {
        _dspTime = AudioSettings.dspTime;
        _lastDspTime = _dspTime;
        _isInitialized = true;
    }

    /// <summary>
    /// Start music playback and synchronize
    /// 开始音乐播放并同步
    /// </summary>
    public void StartMusic()
    {
        if (_music == null) return;

        _music.Play();

        // Record the DSP time and music position when playback starts
        _musicStartDspTime = AudioSettings.dspTime;
        _musicStartPosition = _music.Position;
    }

    /// <summary>
    /// Update conductor (call once per frame)
    /// 更新指挥（每帧调用一次）
    /// </summary>
    public void Update(double currentDspTime)
    {
        _lastDspTime = _dspTime;
        _dspTime = currentDspTime;

        if (_music == null || !_music.IsPlaying)
            return;

        // Get current song position directly from music source
        double songPosition = _music.Position;

        // Check for beats
        if (songPosition > _nextBeatTime)
        {
            OnBeat?.Invoke(_beatNumber);
            _beatNumber++;

            if (_beatNumber % _crotchetsPerBar == 0)
            {
                OnBar?.Invoke(_barNumber);
                _barNumber++;
            }

            _nextBeatTime += _crotchetTime;
        }
    }

    /// <summary>
    /// Seek to specific song position
    /// 跳转到特定歌曲位置
    /// </summary>
    public void Seek(double songPosition)
    {
        if (_music == null) return;

        _music.Seek(songPosition);

        // Recalculate beat position
        _beatNumber = (int)(songPosition / _crotchetTime);
        _barNumber = _beatNumber / _crotchetsPerBar;
        _nextBeatTime = (_beatNumber + 1) * _crotchetTime;

        // Update sync point
        _musicStartDspTime = AudioSettings.dspTime;
        _musicStartPosition = songPosition;
    }

    /// <summary>
    /// Stop playback
    /// 停止播放
    /// </summary>
    public void Stop()
    {
        if (_music == null) return;

        _music.Stop();
        _musicStartDspTime = -1;
        _beatNumber = 0;
        _barNumber = 0;
        _nextBeatTime = 0;
    }

    /// <summary>
    /// Get conductor statistics for debugging
    /// 获取指挥统计信息（用于调试）
    /// </summary>
    public (double dspTime, double songPosition, int beat, int bar) GetStats()
    {
        return (_dspTime, SongPosition, _beatNumber, _barNumber);
    }

    /// <summary>
    /// Check if music is synchronized
    /// 检查音乐是否同步
    /// </summary>
    public bool IsSynchronized(double threshold = 0.05)
    {
        if (_music == null) return false;

        double musicPos = _music.Position;
        double expectedPos = SongPosition;
        double error = Math.Abs(musicPos - expectedPos);

        return error < threshold;
    }
}
