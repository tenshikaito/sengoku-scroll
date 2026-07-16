using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.World;

namespace SengokuScroll.Domain;

public class GameMapMasterData
{
    public required TileMap TileMap { get; set; }

    // 地图元信息（只读/配置）
    public required string Name { get; set; }

    public required string Version { get; set; }

    public required Dictionary<int, TerrainDefinition> Terrains { get; set; }

    public required Dictionary<int, TerrainVegetationFeatureDefinition> TerrainVegatationFeatures { get; set; }

    public required Dictionary<int, TerrainSurfaceFeatureDefinition> TerrainSurfaceFeatures { get; set; }

    public required Dictionary<int, ClimateDefinition> Climates { get; set; }

    public required Dictionary<int, RegionDefinition> Regions { get; set; }

    /// <summary>政治区域 Id 网格（行优先，与 <see cref="TileMap"/> 同尺寸；0=无）。与道路 region 层独立。</summary>
    public byte[] PoliticalRegionGrid { get; set; } = [];

    /// <summary>道路类型 Id → 定义（region 层存储道路类型 Id）。</summary>
    public required Dictionary<int, RoadDefinition> Roads { get; set; }

    public required Dictionary<int, Landmark> Landmarks { get; set; }
}
