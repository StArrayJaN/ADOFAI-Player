using SharpFAI.Editor.Core.Framework.Audio;
using SharpFAI.Framework;

namespace SharpFAI.Editor.Core.Util;

/// <summary>
/// Audio timing debugger for diagnosing synchronization issues
/// 音频时序调试器，用于诊断同步问题
/// </summary>
public class AudioTimingDebugger
{
    private dynamic? _conductor; // Can be AdvancedAudioConductor or ImprovedAudioConductor
    private IMusic? _music;
    private double _lastLogTime;
    private double _logInterval = 1.0; // Log every 1 second

    public AudioTimingDebugger(dynamic conductor, IMusic music)
    {
        _conductor = conductor;
        _music = music;
        _lastLogTime = AudioSettings.dspTime;
    }

    /// <summary>
    /// Log timing information for debugging
    /// 记录时序信息用于调试
    /// </summary>
    public void LogTimingInfo()
    {
        if (_conductor == null || _music == null) return;

        double currentDspTime = AudioSettings.dspTime;
        if (currentDspTime - _lastLogTime < _logInterval) return;

        _lastLogTime = currentDspTime;

        var stats = _conductor.GetStats();
        double dspTime = stats.Item1;
        double songPos = stats.Item2;
        int beat = stats.Item3;
        int bar = stats.Item4;

        double musicPos = _music.Position;
        double syncError = Math.Abs(songPos - musicPos);

        Console.WriteLine($"[Audio Timing Debug]");
        Console.WriteLine($"  DSP Time: {dspTime:F3}s");
        Console.WriteLine($"  Song Position (Conductor): {songPos:F3}s");
        Console.WriteLine($"  Music Position (Source): {musicPos:F3}s");
        Console.WriteLine($"  Sync Error: {syncError * 1000:F1}ms");
        Console.WriteLine($"  Beat: {beat}, Bar: {bar}");
        Console.WriteLine($"  Music Duration: {_music.Duration:F3}s");
        Console.WriteLine($"  Music Playing: {_music.IsPlaying}");
    }

    /// <summary>
    /// Get detailed timing statistics
    /// 获取详细的时序统计
    /// </summary>
    public (double dspTime, double conductorPos, double musicPos, double syncError, int beat, int bar) GetStats()
    {
        if (_conductor == null || _music == null)
            return (0, 0, 0, 0, 0, 0);

        var stats = _conductor.GetStats();
        double dspTime = stats.Item1;
        double songPos = stats.Item2;
        int beat = stats.Item3;
        int bar = stats.Item4;

        double musicPos = _music.Position;
        double syncError = Math.Abs(songPos - musicPos);

        return (dspTime, songPos, musicPos, syncError, beat, bar);
    }

    /// <summary>
    /// Check if timing is synchronized
    /// 检查时序是否同步
    /// </summary>
    public bool IsSynchronized(double threshold = 0.05)
    {
        var (_, _, _, syncError, _, _) = GetStats();
        return syncError < threshold;
    }
}
