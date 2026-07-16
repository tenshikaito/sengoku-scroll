using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Definitions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Domain.Rules;

/// <summary>移动相关规则：地形/道路行动力消耗、据点入城费、同格占用与邻格校验。</summary>
public class MovementRules(IGameContext context)
{
    /// <summary>计算移动到目标格所需行动力（含道路加成与入城附加费）。</summary>
    public int GetTileMovementApCost(IMovable movable, Point2 location)
    {
        var terrain = context.GameWorldContext.GetTerrainOrDefault(location);

        // 业务：无地形数据表示越界或无效格，调用方以 -1 判定失败
        if (terrain is null)
            return -1;

        var cost = terrain.MovementCost;

        var tileMap = context.GameWorldContext.GameMapMasterData.TileMap;
        var roadId = tileMap.GetRegion(location);
        var roads = context.GameWorldContext.GameMapMasterData.Roads;
        // 业务：道路可覆盖地形成本或按 SpeedBonus 减免，最低为 1
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

    /// <summary>校验目标格是否在地图内且行动力足以支付移动（含入城费）。</summary>
    public GameResult CheckMoveToTileAp(IMovable movable, Point3 location)
    {
        var totalCost = GetTileMovementApCost(movable, location);

        if (totalCost < 0)
            return OutOfMapBoundError;

        if (movable.Ap < totalCost)
            return ApNotEnough;

        return GameResult.Ok();
    }

    /// <summary>校验行动力是否足以通过指定地形（不含道路/据点附加费）。</summary>
    public static GameResult CheckMoveAp(IMovable movable, TerrainDefinition terrain)
    {
        if (movable.Ap < terrain.MovementCost)
            return ApNotEnough;

        return GameResult.Ok();
    }

    /// <summary>校验目标格是否与当前位置相邻（单步移动前提）。</summary>
    public static GameResult CheckAdjacent(IHasLocation hasLocation, Point3 location)
    {
        if (!hasLocation.Location.IsAdjacent(location))
            return TargetLocationNotAdjacent;

        return GameResult.Ok();
    }

    /// <summary>校验进入据点格时是否具备入城所需行动力。</summary>
    public GameResult CheckMoveToStronghold(IMovable movable, Point2 location)
    {
        if (context.GameWorldContext.GetStrongholdOrDefault(location) is null)
            return GameResult.Ok();

        if (movable.Ap < context.GameRuleConfig.EnterStrongholdAp)
            return ApNotEnough;

        return GameResult.Ok();
    }

    /// <summary>
    /// 校验目标格军事占用：本势力/同战共战方可叠；敌对可进入同格开战；
    /// 平时同盟与中立军事不可同格；交战格禁中立穿行。
    /// </summary>
    public GameResult CheckMoveToUnit(IMovable movable, Point3 location)
    {
        var occupants = context.GameWorldContext.GetUnitsAt(location);
        if (occupants.Count == 0)
            return GameResult.Ok();

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var myForce = context.GameWorldContext.GetForce(movable);
        if (myForce is null)
            return GameResult.Ok();

        // 业务：正在交战的格禁止非参战方进入
        if (HasActiveBattlefieldAt(location, gameData)
            && !IsParticipantOfTileBattlefields(myForce.Id, location, gameData))
            return MovementError.UnitAlreadyExistsInTile;

        foreach (var targetUnit in occupants)
        {
            if (movable is Unit self && targetUnit.Id == self.Id)
                continue;

            if (!targetUnit.IsMilitary)
                continue;

            var tf = context.GameWorldContext.GetForce(targetUnit);
            if (tf is null)
                continue;

            // 业务：军事与军事
            if (movable.IsMilitary)
            {
                if (WarRules.CanMilitaryStack(myForce.Id, tf.Id, gameData))
                    continue;

                // 业务：援防行军不得进入敌军格（即便进攻方针允许同格开战）
                if (movable is Unit supportUnit
                    && supportUnit.Directive == UnitDirective.Support
                    && AreHostileForces(myForce.Id, tf.Id, gameData))
                    return MovementError.CannotMoveToTile;

                // 业务：敌对（战争对立或外交敌对）允许进入同格以创建战场
                if (AreHostileForces(myForce.Id, tf.Id, gameData))
                    continue;

                return MovementError.UnitAlreadyExistsInTile;
            }

            // 业务：非军事遇敌军 → 会触发遭遇，移动仍可“进入”由拦截系统处理；此处挡死强制绕路更安全
            if (AreHostileForces(myForce.Id, tf.Id, gameData))
                return DiplomacyError.EnemyForce;
        }

        return GameResult.Ok();
    }

    /// <summary>寻路途经格：敌对/不可叠军事挡路（目的格敌对由 CheckMoveToUnit 放行开战）。</summary>
    public bool IsPathTileBlockedByMilitary(IMovable movable, Point2 location)
    {
        if (!movable.IsMilitary)
            return false;

        var occupants = context.GameWorldContext.GetUnitsAt(location);
        if (occupants.Count == 0)
            return false;

        var gameData = context.GameWorldContext.GameWorld.GameData;
        var myForce = context.GameWorldContext.GetForce(movable);
        if (myForce is null)
            return false;

        if (HasActiveBattlefieldAt(location, gameData)
            && !IsParticipantOfTileBattlefields(myForce.Id, location, gameData))
            return true;

        foreach (var occupant in occupants)
        {
            if (movable is Unit self && occupant.Id == self.Id)
                continue;

            if (!occupant.IsMilitary)
                continue;

            var tf = context.GameWorldContext.GetForce(occupant);
            if (tf is null)
                continue;

            if (WarRules.CanMilitaryStack(myForce.Id, tf.Id, gameData))
                continue;

            // 业务：寻路不穿越敌军格（开战只允许在最终进入步）
            return true;
        }

        return false;
    }

    private static bool AreHostileForces(int forceIdA, int forceIdB, GameData gameData)
    {
        if (WarRules.AreWarEnemies(forceIdA, forceIdB, gameData))
            return true;

        if (!gameData.Forces.TryGetValue(forceIdA, out var a)
            || !gameData.Forces.TryGetValue(forceIdB, out var b))
            return false;

        return DiplomacyRules.IsEnemy(a, b).IsSuccess;
    }

    private static bool HasActiveBattlefieldAt(Point2 location, GameData gameData)
        => gameData.Battlefields.Values.Any(b =>
            !b.IsClosed && b.Location.X == location.X && b.Location.Y == location.Y);

    private static bool IsParticipantOfTileBattlefields(int forceId, Point2 location, GameData gameData)
    {
        foreach (var bf in gameData.Battlefields.Values)
        {
            if (bf.IsClosed || bf.Location.X != location.X || bf.Location.Y != location.Y)
                continue;

            if (!gameData.Wars.TryGetValue(bf.WarId, out var war) || war.IsEnded)
                continue;

            if (WarRules.IsParticipant(war, forceId))
                return true;
        }

        return false;
    }
}
