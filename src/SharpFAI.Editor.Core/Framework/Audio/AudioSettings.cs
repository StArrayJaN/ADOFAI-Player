using System.Diagnostics;

namespace SharpFAI.Editor.Core.Framework.Audio;

/// <summary>
/// Platform-independent audio settings wrapper
/// 平台无关的音频设置包装器
/// </summary>
public static class AudioSettings
{
    private static Stopwatch _stopwatch = Stopwatch.StartNew();
    private static double _dspTimeOffset = 0;

    /// <summary>
    /// Get current DSP time (Digital Signal Processing time)
    /// 获取当前 DSP 时间（数字信号处理时间）
    ///
    /// On desktop platforms with OpenTK, this uses high-resolution timer
    /// 在带有 OpenTK 的桌面平台上，这使用高分辨率计时器
    /// </summary>
    public static double dspTime
    {
        get
        {
            // Use high-resolution stopwatch for precise timing
            // 使用高分辨率秒表进行精准计时
            return _stopwatch.Elapsed.TotalSeconds + _dspTimeOffset;
        }
    }

    /// <summary>
    /// Reset DSP time (useful for testing or synchronization)
    /// 重置 DSP 时间（用于测试或同步）
    /// </summary>
    public static void ResetDspTime()
    {
        _stopwatch.Restart();
        _dspTimeOffset = 0;
    }

    /// <summary>
    /// Set DSP time offset (for synchronization with external sources)
    /// 设置 DSP 时间偏移（用于与外部源同步）
    /// </summary>
    public static void SetDspTimeOffset(double offset)
    {
        _dspTimeOffset = offset;
    }
}
