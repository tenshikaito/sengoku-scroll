namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>策略日推进 debug 日志选项。</summary>
public sealed class StrategyDayDebugOptions
{
    /// <summary>是否启用日推进 debug 日志（内存 + 可选写文件）。</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>写文件的目录（相对当前工作目录或绝对路径）。</summary>
    public string OutputDirectory { get; set; } = "Log/strategy-debug";

    /// <summary>是否在每次 <see cref="StrategyDayDebugLog.EndDay"/> 时追加写入文件。</summary>
    public bool WriteToFile { get; set; } = true;

    /// <summary>内存中保留的最大条目数（环形缓冲）。</summary>
    public int MaxInMemoryEntries { get; set; } = 4000;
}
