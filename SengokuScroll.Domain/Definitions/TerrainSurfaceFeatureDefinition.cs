using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Domain.Definitions;

/// <summary>
/// µØ±í¸²¸ÇÎï
/// </summary>
public class TerrainSurfaceFeatureDefinition
{
    public TerrainFeatureType Type { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }
}
