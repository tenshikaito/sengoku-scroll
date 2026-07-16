using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// AI 决策结果（对齐 <see cref="Domain.GameResult"/>：IsSuccess + 失败码；
/// 额外携带思维链 Steps，便于 debug 优化）。
/// </summary>
public readonly struct StrategyAiDecision
{
    public bool IsSuccess { get; }

    /// <summary>决策码，如 EngageAdjacent / MarchRetreat / Hold / Skip。</summary>
    public string Code { get; }

    /// <summary>一句话结论。</summary>
    public string Message { get; }

    /// <summary>思维链步骤（评估过程）。</summary>
    public IReadOnlyList<string> Steps { get; }

    public int? TargetUnitId { get; }

    public Point2? TargetPoint { get; }

    public int? TargetStrongholdId { get; }

    public string? Stance { get; }

    public string? SiegeMode { get; }

    public string? UnitStatus { get; }

    private StrategyAiDecision(
        bool isSuccess,
        string code,
        string message,
        IReadOnlyList<string> steps,
        int? targetUnitId,
        Point2? targetPoint,
        int? targetStrongholdId,
        string? stance,
        string? siegeMode,
        string? unitStatus)
    {
        IsSuccess = isSuccess;
        Code = code;
        Message = message;
        Steps = steps;
        TargetUnitId = targetUnitId;
        TargetPoint = targetPoint;
        TargetStrongholdId = targetStrongholdId;
        Stance = stance;
        SiegeMode = siegeMode;
        UnitStatus = unitStatus;
    }

    /// <summary>已采取行动（接敌/行军等）。</summary>
    public static StrategyAiDecision Ok(
        string code,
        string message,
        StrategyAiThought thought,
        int? targetUnitId = null,
        Point2? targetPoint = null,
        int? targetStrongholdId = null,
        string? stance = null,
        string? siegeMode = null,
        string? unitStatus = null)
        => new(true, code, message, thought.Snapshot(), targetUnitId, targetPoint,
            targetStrongholdId, stance, siegeMode, unitStatus);

    /// <summary>未采取行动（待机/跳过/无目标）。</summary>
    public static StrategyAiDecision Fail(
        string code,
        string message,
        StrategyAiThought thought,
        int? targetStrongholdId = null,
        string? stance = null,
        string? siegeMode = null,
        string? unitStatus = null)
        => new(false, code, message, thought.Snapshot(), null, null,
            targetStrongholdId, stance, siegeMode, unitStatus);

    /// <summary>附加单位当前方针/姿态/攻城/目标，便于 debug 分析。</summary>
    public static StrategyAiDecision WithUnitContext(StrategyAiDecision decision, Unit unit)
        => new(
            decision.IsSuccess,
            decision.Code,
            decision.Message,
            decision.Steps,
            decision.TargetUnitId ?? (unit.ActionTarget.UnitId > 0 ? unit.ActionTarget.UnitId : null),
            decision.TargetPoint,
            decision.TargetStrongholdId
                ?? (unit.ActionTarget.StrongholdId > 0 ? unit.ActionTarget.StrongholdId : (int?)null)
                ?? (unit.DirectiveTargetId > 0 ? unit.DirectiveTargetId : null),
            decision.Stance ?? unit.Stance.ToString(),
            decision.SiegeMode ?? unit.SiegeMode.ToString(),
            decision.UnitStatus ?? unit.Status.ToString());

    public static implicit operator bool(StrategyAiDecision d) => d.IsSuccess;

    public override string ToString()
        => $"{(IsSuccess ? "OK" : "IDLE")} {Code}: {Message} [{Steps.Count} steps]";
}

/// <summary>可变思维链缓冲，决策过程中逐步追加。</summary>
public sealed class StrategyAiThought
{
    private readonly List<string> steps = [];

    public StrategyAiThought Add(string step)
    {
        if (!string.IsNullOrWhiteSpace(step))
            steps.Add(step.Trim());
        return this;
    }

    public StrategyAiThought Add(string format, params object?[] args)
        => Add(string.Format(format, args));

    public IReadOnlyList<string> Snapshot() => [.. steps];
}

/// <summary>方针评估结果（可能改写 Directive）。</summary>
public readonly struct StrategyAiDirectiveDecision
{
    public bool Changed { get; }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyList<string> Steps { get; }

    public string? FromDirective { get; }

    public string? ToDirective { get; }

    private StrategyAiDirectiveDecision(
        bool changed,
        string code,
        string message,
        IReadOnlyList<string> steps,
        string? fromDirective,
        string? toDirective)
    {
        Changed = changed;
        Code = code;
        Message = message;
        Steps = steps;
        FromDirective = fromDirective;
        ToDirective = toDirective;
    }

    public static StrategyAiDirectiveDecision Unchanged(string code, string message, StrategyAiThought thought)
        => new(false, code, message, thought.Snapshot(), null, null);

    public static StrategyAiDirectiveDecision ChangedTo(
        string code,
        string message,
        StrategyAiThought thought,
        string fromDirective,
        string toDirective)
        => new(true, code, message, thought.Snapshot(), fromDirective, toDirective);
}
