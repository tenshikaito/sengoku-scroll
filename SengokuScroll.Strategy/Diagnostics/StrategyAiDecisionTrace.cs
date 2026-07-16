using System.Collections.Concurrent;
using SengokuScroll.Common.Types;
using SengokuScroll.Localization;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>策略 AI 决策诊断环形缓冲（供 WebApi debug 端点）。</summary>
public sealed class StrategyAiDecisionTrace
{
    private const int MaxEntries = 400;
    private readonly ConcurrentQueue<StrategyAiDecisionTraceEntry> entries = new();
    private readonly IStrategyDayDebugLog dayDebugLog;
    private int sequence;

    public StrategyAiDecisionTrace(IStrategyDayDebugLog dayDebugLog)
        => this.dayDebugLog = dayDebugLog;

    public void Clear() => entries.Clear();

    public IReadOnlyList<StrategyAiDecisionTraceEntry> Snapshot()
        => [.. entries];

    public void LogDirective(int unitId, string unitName, int forceId, StrategyAiDirectiveDecision decision)
    {
        Enqueue(new StrategyAiDecisionTraceEntry(
            Interlocked.Increment(ref sequence),
            DateTimeOffset.UtcNow,
            "Directive",
            decision.Code,
            decision.Message,
            unitId,
            unitName,
            forceId,
            decision.Changed,
            decision.FromDirective,
            decision.ToDirective,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            decision.Steps));

        dayDebugLog.LogLocalized(
            "AI",
            LocalizationKeys.Debug.AiDirective,
            forceId,
            unitId,
            unitName,
            decision.FromDirective ?? "-",
            decision.ToDirective ?? "-",
            decision.Message);
    }

    public void LogAction(int unitId, string unitName, int forceId, string directive, StrategyAiDecision decision)
    {
        Enqueue(new StrategyAiDecisionTraceEntry(
            Interlocked.Increment(ref sequence),
            DateTimeOffset.UtcNow,
            "Action",
            decision.Code,
            decision.Message,
            unitId,
            unitName,
            forceId,
            decision.IsSuccess,
            null,
            null,
            directive,
            decision.TargetUnitId,
            decision.TargetPoint,
            decision.TargetStrongholdId,
            decision.Stance,
            decision.SiegeMode,
            decision.UnitStatus,
            decision.Steps));

        dayDebugLog.LogLocalized(
            "AI",
            LocalizationKeys.Debug.AiAction,
            forceId,
            unitId,
            unitName,
            directive,
            decision.Code,
            decision.Message);

        if (decision.Steps.Count > 0)
            dayDebugLog.LogLine("AI", $"  steps: {string.Join(" | ", decision.Steps)}");
    }

    public void LogSkip(int unitId, string unitName, int forceId, string reason)
    {
        Enqueue(new StrategyAiDecisionTraceEntry(
            Interlocked.Increment(ref sequence),
            DateTimeOffset.UtcNow,
            "Skip",
            "Skip",
            reason,
            unitId,
            unitName,
            forceId,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            [reason]));

        dayDebugLog.LogLocalized(
            "AI",
            LocalizationKeys.Debug.AiSkip,
            forceId,
            unitId,
            unitName,
            reason);
    }

    private void Enqueue(StrategyAiDecisionTraceEntry entry)
    {
        entries.Enqueue(entry);
        while (entries.Count > MaxEntries && entries.TryDequeue(out _))
        {
        }
    }
}

public sealed record StrategyAiDecisionTraceEntry(
    int Sequence,
    DateTimeOffset At,
    string Phase,
    string Code,
    string Message,
    int UnitId,
    string UnitName,
    int ForceId,
    bool ActedOrChanged,
    string? FromDirective,
    string? ToDirective,
    string? CurrentDirective,
    int? TargetUnitId,
    Point2? TargetPoint,
    int? TargetStrongholdId,
    string? Stance,
    string? SiegeMode,
    string? UnitStatus,
    IReadOnlyList<string> Steps);
