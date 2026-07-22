namespace SengokuScroll.Strategy.Persistence;

/// <summary>磁盘存档位摘要（列表 UI）。</summary>
public sealed class StrategySaveSlotSummary
{
    public required int Slot { get; init; }

    public required bool Occupied { get; init; }

    public DateTime? SavedAtUtc { get; init; }

    public string? ScenarioId { get; init; }

    public string? LordName { get; init; }

    public string? DateLabel { get; init; }
}

/// <summary>单个存档位文件内容。</summary>
public sealed class StrategySaveSlotEnvelope
{
    public required DateTime SavedAtUtc { get; init; }

    public required string LordName { get; init; }

    public required StrategySaveDocument Save { get; init; }
}
