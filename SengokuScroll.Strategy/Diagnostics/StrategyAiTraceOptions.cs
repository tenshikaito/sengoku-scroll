namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>AI 决策 trace 缓冲选项。</summary>
public sealed class StrategyAiTraceOptions
{
    /// <summary>内存环形缓冲最大条目数；仿真模式可设为 <see cref="int.MaxValue"/>。</summary>
    public int MaxEntries { get; set; } = 400;
}
