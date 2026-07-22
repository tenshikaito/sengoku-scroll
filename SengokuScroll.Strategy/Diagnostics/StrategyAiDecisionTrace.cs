using System.Collections.Concurrent;
using SengokuScroll.Common.Types;
using Microsoft.Extensions.Options;
using SengokuScroll.Localization;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>策略 AI 决策诊断环形缓冲（供 WebApi debug 端点）。</summary>
public sealed class StrategyAiDecisionTrace
{
    private readonly ConcurrentQueue<StrategyAiDecisionTraceEntry> entries = new();
    private readonly IStrategyDayDebugLog dayDebugLog;
    private readonly int maxEntries;
    private int sequence;

    public StrategyAiDecisionTrace(
        IStrategyDayDebugLog dayDebugLog,
        IOptions<StrategyAiTraceOptions>? traceOptions = null)
    {
        this.dayDebugLog = dayDebugLog;
        maxEntries = traceOptions?.Value.MaxEntries ?? 400;
    }

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

        LogThoughtSteps(decision.Steps);
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
            LogThoughtSteps(decision.Steps);
    }

    private void LogThoughtSteps(IReadOnlyList<string> steps)
    {
        foreach (var step in steps)
            dayDebugLog.LogLine("AI", $"  thought: {step}");
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

        LogThoughtSteps([reason]);
    }

    private void Enqueue(StrategyAiDecisionTraceEntry entry)
    {
        entries.Enqueue(entry);
        if (maxEntries == int.MaxValue)
            return;

        while (entries.Count > maxEntries && entries.TryDequeue(out _))
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
