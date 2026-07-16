namespace SengokuScroll.Strategy.Models;

/// <summary>日推进 debug 日志快照 DTO。</summary>
public sealed record StrategyDayDebugLogSnapshotDto
{
    public required bool Enabled { get; init; }

    public string? LastWrittenFilePath { get; init; }

    public required IReadOnlyList<StrategyDayDebugEntryDto> Entries { get; init; }
}
