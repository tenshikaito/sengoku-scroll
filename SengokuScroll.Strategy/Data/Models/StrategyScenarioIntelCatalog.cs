namespace SengokuScroll.Strategy.Data.Models;

/// <summary>剧本 JSON 中实体展示用文本（指挥官/领主/文化等），供 API DTO 映射。</summary>
public sealed class StrategyScenarioIntelCatalog
{
    public static StrategyScenarioIntelCatalog Empty { get; } = new();

    public Dictionary<int, StrategyUnitIntelOverlay> Units { get; init; } = new();

    public Dictionary<int, StrategyStrongholdIntelOverlay> Strongholds { get; init; } = new();
}

public sealed class StrategyUnitIntelOverlay
{
    public string? CommanderName { get; init; }

    public string CultureName { get; init; } = "日本";

    public string ReligionName { get; init; } = "神道";
}

public sealed class StrategyStrongholdIntelOverlay
{
    public string? LordName { get; init; }

    public string? MayorName { get; init; }

    public string CultureName { get; init; } = "日本";

    public string ReligionName { get; init; } = "神道";
}
