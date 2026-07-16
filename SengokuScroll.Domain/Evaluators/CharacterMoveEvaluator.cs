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
    /// <summary>评估角色单步移动是否合法；途经敌军单位格时须为敌对关系。</summary>
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

            // 业务：友军单位格不阻挡角色，交由 MovementRules 处理同格细则
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
