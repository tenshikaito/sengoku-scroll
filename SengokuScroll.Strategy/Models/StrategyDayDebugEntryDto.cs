namespace SengokuScroll.Strategy.Models;

/// <summary>日推进 debug 日志条目 DTO。</summary>
public sealed record StrategyDayDebugEntryDto
{
    public required int Sequence { get; init; }

    public required string At { get; init; }

    public int? GameYear { get; init; }

    public int? GameMonth { get; init; }

    public int? GameDay { get; init; }

    public required string Category { get; init; }

    public required string Message { get; init; }
}
