namespace SengokuScroll.Strategy.Data.Models;

/// <summary>剧本元数据：玩家势力与当主位置（M3-b）。</summary>
public sealed class StrategyScenarioMeta
{
    public int PlayerForceId { get; init; } = 1;

    public string LordName { get; init; } = "当主";

    /// <summary>当主所在军事单位 Id；优先于 <see cref="LordStrongholdId"/>。</summary>
    public int? LordUnitId { get; init; }

    /// <summary>当主驻留据点 Id（当主不在部队中时使用）。</summary>
    public int? LordStrongholdId { get; init; }

    /// <summary>各势力当主角色 Id（ForceId → Character.Id）。</summary>
    public IReadOnlyDictionary<int, int> ForceLordCharacterIds { get; init; }
        = new Dictionary<int, int>();

    /// <summary>实体情报展示文本（来自剧本 JSON）。</summary>
    public StrategyScenarioIntelCatalog Intel { get; init; } = StrategyScenarioIntelCatalog.Empty;
}
