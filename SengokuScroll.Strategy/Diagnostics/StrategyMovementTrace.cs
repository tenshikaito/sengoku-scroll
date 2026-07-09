using System.Collections.Concurrent;
using SengokuScroll.Common.Types;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>策略单位移动诊断环形缓冲（供 WebApi 日志与 debug 端点）。</summary>
public sealed class StrategyMovementTrace
{
    private const int MaxEntries = 200;
    private readonly ConcurrentQueue<StrategyMovementTraceEntry> entries = new();
    private int sequence;

    public void Clear() => entries.Clear();

    public IReadOnlyList<StrategyMovementTraceEntry> Snapshot()
        => entries.ToArray();

    public void Log(string phase, string message, int? unitId = null, Point2? from = null, Point2? to = null,
        string? detail = null)
    {
        var entry = new StrategyMovementTraceEntry(
            Interlocked.Increment(ref sequence),
            DateTimeOffset.UtcNow,
            phase,
            message,
            unitId,
            from,
            to,
            detail);

        entries.Enqueue(entry);

        while (entries.Count > MaxEntries && entries.TryDequeue(out _))
        {
        }
    }
}

public sealed record StrategyMovementTraceEntry(
    int Sequence,
    DateTimeOffset At,
    string Phase,
    string Message,
    int? UnitId,
    Point2? From,
    Point2? To,
    string? Detail);
