using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.World;

namespace SengokuScroll.Domain.Rules;

/// <summary>通用地图规则：边界校验等跨系统共用检查。</summary>
public class CommonRules(IGameContext context)
{
    /// <summary>校验坐标是否在当前剧本地图范围内。</summary>
    public GameResult CheckOutOfBounds(Point3 p)
    {
        var tileMap = context.GameWorldContext.GameMapMasterData.TileMap;

        return CheckOutOfBounds(tileMap, p);
    }

    /// <summary>校验坐标是否在指定 TileMap 范围内。</summary>
    public static GameResult CheckOutOfBounds(TileMap tileMap, Point3 p)
    {
        if (tileMap.IsOutOfBounds(p))
            return GameResult.Fail(GameError.OutOfMapBoundError);

        return GameResult.Ok();
    }
}
