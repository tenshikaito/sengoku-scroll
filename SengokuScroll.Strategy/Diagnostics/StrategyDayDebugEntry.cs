namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>日推进 debug 日志条目。</summary>
public sealed record StrategyDayDebugEntry(
    int Sequence,
    DateTimeOffset At,
    int? GameYear,
    int? GameMonth,
    int? GameDay,
    string Category,
    string Message);
