namespace SengokuScroll.Strategy.Models;

/// <summary>
/// 地图静态主数据（地形/区域/道路/地标）；前端启动时加载一次，不随日推进重复下发。
/// </summary>
public sealed record StrategyMapMasterDto
{
    public required string ScenarioId { get; init; }

    public required string Name { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required IReadOnlyList<StrategyTerrainDefDto> Terrains { get; init; }

    public required IReadOnlyList<StrategyRegionDefDto> Regions { get; init; }

    public required IReadOnlyList<StrategyRoadTypeDefDto> RoadTypes { get; init; }

    /// <summary>行优先地形 Id（长度 = Width × Height）。</summary>
    public required IReadOnlyList<int> TerrainIds { get; init; }

    /// <summary>行优先区域 Id（0 = 无区域）。</summary>
    public required IReadOnlyList<int> RegionIds { get; init; }

    public required IReadOnlyList<StrategyRoadCellDto> RoadCells { get; init; }

    public required IReadOnlyList<StrategyMapLandmarkDto> Landmarks { get; init; }
}

/// <summary>地形类型定义（对应 <see cref="Definitions.TerrainDefinition"/>）。</summary>
public sealed record StrategyTerrainDefDto
{
    public required int Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public required int MovementCost { get; init; }
}

/// <summary>地图区域定义（对应 <see cref="Definitions.RegionDefinition"/>）。</summary>
public sealed record StrategyRegionDefDto
{
    public required int Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }
}

/// <summary>道路类型定义（对应 <see cref="Definitions.RoadDefinition"/>）。</summary>
public sealed record StrategyRoadTypeDefDto
{
    public required int Id { get; init; }

    public required string Key { get; init; }

    public required string Name { get; init; }

    public required int SpeedBonus { get; init; }

    public int? MovementCost { get; init; }
}
