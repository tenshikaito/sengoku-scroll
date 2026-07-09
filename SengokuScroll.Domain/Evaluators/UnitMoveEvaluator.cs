using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Rules;

namespace SengokuScroll.Domain.Evaluators;

/// <summary>军事单位移动合法性评估（不含同格敌军拦截，策略/ZOC 扩展见 StrategyUnitMoveEvaluator）。</summary>
public class UnitMoveEvaluator(
    IGameContext context,
    MovementRules movementRules) : EvaluatorBase
{
    public GameResult Evaluate(IMovable movable, Point2 location)
    {
        GameResult CheckOutOfBounds()
            => CommonRules.CheckOutOfBounds(context.GameWorldContext.GameMapMasterData.TileMap, location);

        GameResult CheckMovementTileAp()
            => movementRules.CheckMoveToTileAp(movable, location);

        GameResult CheckAdjacent()
            => MovementRules.CheckAdjacent(movable, location);

        return Evaluate(
        [
            CheckOutOfBounds,
            CheckMovementTileAp,
            CheckAdjacent
        ]);
    }
}
