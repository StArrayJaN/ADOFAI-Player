using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Framework.Audio;

/// <summary>
/// Advanced audio conductor system based on ADOFAI's scrConductor
/// 基于 ADOFAI scrConductor 的高级音频指挥系统
///
/// Key concepts from ADOFAI:
/// 1. dspTime: Unity's AudioSettings.dspTime - precise audio playback time
/// 2. dspTimeSong: Scheduled start time of the song in DSP time
/// 3. songposition_minusi: Current song position (in seconds) without input offset
/// 4. Scheduled playback: Use AudioSource.PlayScheduled() for frame-perfect timing
/// 5. Lookahead scheduling: Schedule sounds 5 seconds in advance
/// </summary>
public class AdvancedAudioConductor
{
    private IMusic? _music;
    private double _dspTime; // Current DSP time (from audio source)
    private double _dspTimeSong; // Scheduled start time of song
    private double _songPosition; // Current song position (seconds)
    private double _lastDspTime; // Last frame's DSP time
    private double _bpm;
    private double _offset; // Audio offset in seconds
    private float _pitch = 1.0f;

    // Lookahead buffer for scheduling sounds
    private const double LOOKAHEAD_TIME = 5.0; // Schedule sounds 5 seconds in advance

    // Beat tracking
    private double _crotchetTime; // Time for one beat (60 / BPM)
    private double _nextBeatTime;
    private int _beatNumber;
    private int _barNumber;
    private int _crotchetsPerBar = 8;

    // Events
    public event Action<int>? OnBeat;
    public event Action<int>? OnBar;

    /// <summary>
    /// Get current DSP time (precise audio playback time)
    /// 获取当前 DSP 时间（精准音频播放时间）
    /// </summary>
    public double DspTime => _dspTime;

    /// <summary>
    /// Get song position in seconds (without offset)
    /// 获取歌曲位置（秒，不含偏移）
    /// </summary>
    public double SongPosition => _songPosition;

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
    /// Create advanced audio conductor
    /// 创建高级音频指挥
    /// </summary>
    public AdvancedAudioConductor(IMusic music, double bpm, double offset = 0, float pitch = 1.0f)
    {
        _music = music;
        _bpm = Math.Max(1, bpm);
        _offset = offset;
        _pitch = Math.Max(0.1f, pitch);
        _crotchetTime = 60.0 / _bpm;
        _dspTime = 0;
        _dspTimeSong = 0;
        _songPosition = 0;
        _nextBeatTime = 0;
        _beatNumber = 0;
        _barNumber = 0;
    }

    /// <summary>
    /// Initialize conductor with music start time
    /// 使用音乐开始时间初始化指挥
    /// </summary>
    public void Initialize(double currentDspTime)
    {
        _dspTime = currentDspTime;
        _lastDspTime = currentDspTime;
        // dspTimeSong will be set by SetSongStartTime
    }

    /// <summary>
    /// Update conductor (call once per frame)
    /// 更新指挥（每帧调用一次）
    /// </summary>
    public void Update(double currentDspTime)
    {
        _lastDspTime = _dspTime;
        _dspTime = currentDspTime;

        // Calculate song position: (dspTime - dspTimeSong - offset) * pitch
        // offset is already in seconds, no need to divide by 1000
        double rawPosition = (_dspTime - _dspTimeSong - _offset) * _pitch;
        _songPosition = Math.Max(0, rawPosition);

        // Check for beats
        if (_songPosition > _nextBeatTime)
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
    /// Schedule a sound to play at specific DSP time
    /// 安排声音在特定 DSP 时间播放
    /// </summary>
    public void ScheduleSound(Action<double> playCallback, double scheduledTime)
    {
        // Only schedule if within lookahead window
        if (scheduledTime > _dspTime && scheduledTime <= _dspTime + LOOKAHEAD_TIME)
        {
            playCallback(scheduledTime);
        }
    }

    /// <summary>
    /// Calculate DSP time for a song position
    /// 计算歌曲位置对应的 DSP 时间
    /// </summary>
    public double GetDspTimeForSongPosition(double songPosition)
    {
        // dspTime = dspTimeSong + (songPosition + offset) / pitch
        return _dspTimeSong + (songPosition + _offset) / _pitch;
    }

    /// <summary>
    /// Calculate song position for a DSP time
    /// 计算 DSP 时间对应的歌曲位置
    /// </summary>
    public double GetSongPositionForDspTime(double dspTime)
    {
        // songPosition = (dspTime - dspTimeSong - offset) * pitch
        return (dspTime - _dspTimeSong - _offset) * _pitch;
    }

    /// <summary>
    /// Set song start time (called when music starts playing)
    /// 设置歌曲开始时间（音乐开始播放时调用）
    /// </summary>
    public void SetSongStartTime(double dspTime)
    {
        _dspTimeSong = dspTime;
        _nextBeatTime = 0;
        _beatNumber = 0;
        _barNumber = 0;
    }

    /// <summary>
    /// Seek to specific song position
    /// 跳转到特定歌曲位置
    /// </summary>
    public void Seek(double songPosition)
    {
        if (_music == null) return;

        _music.Seek(songPosition);
        _songPosition = songPosition;

        // Recalculate beat position
        _beatNumber = (int)(_songPosition / _crotchetTime);
        _barNumber = _beatNumber / _crotchetsPerBar;
        _nextBeatTime = (_beatNumber + 1) * _crotchetTime;

        // Reset DSP time tracking
        _dspTime = AudioSettings.dspTime;
        _lastDspTime = _dspTime;
        // Recalculate dspTimeSong based on current position
        _dspTimeSong = _dspTime - (songPosition + _offset) / _pitch;
    }

    /// <summary>
    /// Get lookahead time for scheduling
    /// 获取用于调度的前瞻时间
    /// </summary>
    public double GetLookaheadTime() => LOOKAHEAD_TIME;

    /// <summary>
    /// Check if a scheduled time is within lookahead window
    /// 检查计划时间是否在前瞻窗口内
    /// </summary>
    public bool IsWithinLookahead(double scheduledTime)
    {
        return scheduledTime > _dspTime && scheduledTime <= _dspTime + LOOKAHEAD_TIME;
    }

    /// <summary>
    /// Get conductor statistics for debugging
    /// 获取指挥统计信息（用于调试）
    /// </summary>
    public (double dspTime, double songPosition, int beat, int bar) GetStats()
    {
        return (_dspTime, _songPosition, _beatNumber, _barNumber);
    }
}
