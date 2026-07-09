using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Rules;

namespace SengokuScroll.Domain.Evaluators;

/// <summary>角色移动合法性评估（含敌军单位格拦截）。</summary>
public class CharacterMoveEvaluator(
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

        GameResult CheckMoveToUnit()
        {
            var unit = context.GameWorldContext.GetUnitOrDefault(location);

            if (unit is null)
                return GameResult.Ok();

            var myForce = context.GameWorldContext.GetForce(movable);
            var targetForce = context.GameWorldContext.GetForce(unit);

            var r = DiplomacyRules.IsEnemy(myForce, targetForce);

            if (!r)
                return r;

            return movementRules.CheckMoveToUnit(movable, location);
        }

        return Evaluate(
        [
            CheckOutOfBounds,
            CheckMovementTileAp,
            CheckAdjacent,
            CheckMoveToUnit
        ]);
    }
}
