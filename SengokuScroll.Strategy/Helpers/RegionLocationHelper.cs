using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>解析据点所在政治 Region Id。</summary>
public static class RegionLocationHelper
{
    public static int ResolvePoliticalRegionId(GameWorld world, Point3 location)
    {
        var master = world.GameMapMasterData;
        if (master.PoliticalRegionGrid.Length == 0)
            return 0;

        if (master.TileMap.IsOutOfBounds(location))
            return 0;

        var index = master.TileMap.GetIndex(location);
        if (index < 0 || index >= master.PoliticalRegionGrid.Length)
            return 0;

        return master.PoliticalRegionGrid[index];
    }
}
