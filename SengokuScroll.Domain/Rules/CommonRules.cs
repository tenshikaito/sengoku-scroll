using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.World;

namespace SengokuScroll.Domain.Rules;

public class CommonRules(IGameContext context)
{
    public GameResult CheckOutOfBounds(Point3 p)
    {
        var tileMap = context.GameWorldContext.GameMapMasterData.TileMap;

        return CheckOutOfBounds(tileMap, p);
    }

    public static GameResult CheckOutOfBounds(TileMap tileMap, Point3 p)
    {
        if (tileMap.IsOutOfBounds(p))
            return GameResult.Fail(GameError.OutOfMapBoundError);

        return GameResult.Ok();
    }
}
