namespace SengokuScroll.Strategy.Models;

/// <summary>移动诊断条目 API 响应。</summary>
public sealed record StrategyMovementTraceEntryDto
{
    public required int Sequence { get; init; }

    public required string At { get; init; }

    public required string Phase { get; init; }

    public required string Message { get; init; }

    public int? UnitId { get; init; }

    public int? FromX { get; init; }

    public int? FromY { get; init; }

    public int? ToX { get; init; }

    public int? ToY { get; init; }

    public string? Detail { get; init; }
}
