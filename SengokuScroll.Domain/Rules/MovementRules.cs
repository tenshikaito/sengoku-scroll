using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Rules;

public class MovementRules(IGameContext context)
{
    public int GetTileMovementApCost(IMovable movable, Point2 location)
    {
        var terrain = context.GameWorldContext.GetTerrainOrDefault(location);

        if (terrain is null)
            return -1;

        var cost = terrain.MovementCost;

        var tileMap = context.GameWorldContext.GameMapMasterData.TileMap;
        var roadId = tileMap.GetRegion(location);
        var roads = context.GameWorldContext.GameMapMasterData.Roads;
        if (roadId > 0 && roads.TryGetValue(roadId, out var road))
        {
            cost = road.MovementCostOverride ?? Math.Max(1, cost - road.SpeedBonus);
        }

        if (RequiresStrongholdEntryAp(movable, location))
            cost += context.GameRuleConfig.EnterStrongholdAp;

        return cost;
    }

    /// <summary>途经己方据点仅计地形成本；进入他势力/敌城额外计 <see cref="GameRuleConfig.EnterStrongholdAp"/>。</summary>
    public bool RequiresStrongholdEntryAp(IMovable movable, Point2 location)
    {
        var stronghold = context.GameWorldContext.GetStrongholdOrDefault(location);

        if (stronghold is null)
            return false;

        var myForce = context.GameWorldContext.GetForce(movable);
        var holderForce = context.GameWorldContext.GetForce(stronghold);

        return !DiplomacyRules.IsSelf(myForce, holderForce).IsSuccess;
    }

    public GameResult CheckMoveToTileAp(IMovable movable, Point3 location)
    {
        var totalCost = GetTileMovementApCost(movable, location);

        if (totalCost < 0)
            return OutOfMapBoundError;

        if (movable.Ap < totalCost)
            return ApNotEnough;

        return GameResult.Ok();
    }

    public static GameResult CheckMoveAp(IMovable movable, TerrainDefinition terrain)
    {
        // 如果行动力不够
        if (movable.Ap < terrain.MovementCost)
            return ApNotEnough;

        return GameResult.Ok();
    }

    public static GameResult CheckAdjacent(IHasLocation hasLocation, Point3 location)
    {
        if (!hasLocation.Location.IsAdjacent(location))
            return TargetLocationNotAdjacent;

        return GameResult.Ok();
    }

    public GameResult CheckMoveToStronghold(IMovable movable, Point2 location)
    {
        if (context.GameWorldContext.GetStrongholdOrDefault(location) is null)
            return GameResult.Ok();

        if (movable.Ap < context.GameRuleConfig.EnterStrongholdAp)
            return ApNotEnough;

        return GameResult.Ok();
    }

    public GameResult CheckMoveToUnit(IMovable movable, Point3 location)
    {
        var t = context.GameWorldContext.GetUnitOrDefault(location);

        if (t is null)
            return GameResult.Ok();

        var targetUnit = t!;

        // 如果目的地有单位
        // 如果双方是军队
        if (movable.IsMilitary && targetUnit.IsMilitary)
        {
            // 如果互相没有向对方移动
            if (!(movable.IsFaceToFace(targetUnit) && movable.IsReadyToMove && targetUnit.IsReadyToMove))
                return MovementError.UnitAlreadyExistsInTile;
        }

        // 非军事单位（如运输队）可与友军同格；仅敌军队列阻挡
        if (!movable.IsMilitary && targetUnit.IsMilitary)
        {
            var sf = context.GameWorldContext.GetForce(movable);
            var tf = context.GameWorldContext.GetForce(targetUnit);

            if (DiplomacyRules.IsEnemy(sf, tf))
                return DiplomacyError.EnemyForce;
        }

        // 如果是单位且对方是单位
        if (movable.IsUnit && targetUnit.IsUnit)
        {
            var sf = context.GameWorldContext.GetForce(movable);
            var tf = context.GameWorldContext.GetForce(t);

            // 是敌人
            if (DiplomacyRules.IsEnemy(sf, tf))
                return DiplomacyError.EnemyForce;
        }

        return GameResult.Ok();
    }
}
