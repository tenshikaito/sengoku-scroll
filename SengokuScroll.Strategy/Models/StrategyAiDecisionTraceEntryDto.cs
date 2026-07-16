namespace SengokuScroll.Strategy.Models;

/// <summary>AI 决策诊断条目 API 响应。</summary>
public sealed record StrategyAiDecisionTraceEntryDto
{
    public required int Sequence { get; init; }

    public required string At { get; init; }

    /// <summary>Directive | Action | Skip</summary>
    public required string Phase { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }

    public required int UnitId { get; init; }

    public required string UnitName { get; init; }

    public required int ForceId { get; init; }

    /// <summary>是否改写方针 / 是否采取行动。</summary>
    public required bool ActedOrChanged { get; init; }

    public string? FromDirective { get; init; }

    public string? ToDirective { get; init; }

    public string? CurrentDirective { get; init; }

    public int? TargetUnitId { get; init; }

    public int? TargetStrongholdId { get; init; }

    public int? TargetX { get; init; }

    public int? TargetY { get; init; }

    public string? Stance { get; init; }

    public string? SiegeMode { get; init; }

    public string? UnitStatus { get; init; }

    /// <summary>思维链步骤。</summary>
    public required IReadOnlyList<string> Steps { get; init; }
}
