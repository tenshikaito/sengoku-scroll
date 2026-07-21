using SengokuScroll.Common.Types;
using SengokuScroll.Domain;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>解析格点所在地图区域 Id（<see cref="Domain.World.TileMap"/> region 层）。</summary>
public static class RegionLocationHelper
{
    public static int ResolveRegionId(GameWorld world, Point3 location)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        if (tileMap.IsOutOfBounds(location))
            return 0;

        return tileMap.GetRegion(location);
    }
}
