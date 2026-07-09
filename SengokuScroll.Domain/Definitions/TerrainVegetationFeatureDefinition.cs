using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// 地表生命相关覆盖物
/// </summary>
public class TerrainVegetationFeatureDefinition
{
    public TerrainFeatureType Type { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

}
