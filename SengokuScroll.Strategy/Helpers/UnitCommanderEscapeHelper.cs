using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>部队溃灭后将领脱离部队、在地图格现身并寻路回居城。</summary>
public static class UnitCommanderEscapeHelper
{
    /// <summary>收集部队编制内所有将领 Id（含子队）。</summary>
    public static IEnumerable<int> CollectCommanderIds(Unit unit, GameData gameData)
    {
        var ids = new HashSet<int>();
        CollectCommanderIdsRecursive(unit, gameData, ids);
        return ids;
    }

    /// <summary>将领脱离部队，在当前格以 Map 状态出现并自动寻路回所属居城。</summary>
    public static void ReleaseToMapAndRouteHome(
        IGameWorldContext context,
        Character commander,
        Point3 releaseLocation,
        GameData gameData,
        StrategyScenarioMeta meta,
        IPathfindingService pathfinding)
    {
        if (commander.IsDead || commander.ForceStatus == CharacterForceStatus.Prisoner)
            return;

        var home = ResolveHomeStronghold(commander, gameData, meta);
        if (home is null)
            return;

        commander.ForceStatus = CharacterForceStatus.Idle;
        commander.LocationType = CharacterLocationType.Map;
        commander.LocationStrongholdId = 0;
        commander.ActionTarget.StrongholdId = home.Id;
        commander.ActionStatus = CharacterActionStatus.Moving;
        commander.ActionTarget.RoutePoints.Clear();
        // 业务：溃逃将领按军事移动力起步，配合 CharacterMoveAction 单日格数上限逐日回城
        commander.Ap = Math.Max(commander.Ap, 5);

        MapLocationActions.SetCharacterLocation(context, commander, releaseLocation);

        var path = pathfinding.CalculatePathFrom((Point2)releaseLocation, (Point2)home.Location, commander);
        if (path is not null && path.Count > 1)
        {
            // 业务：路径入队后由 CharacterMoveAction 逐日推进，前端 mapCharacters 可见回城过程
            foreach (var node in path.Skip(1))
                commander.ActionTarget.RoutePoints.Enqueue(node.Location);
            return;
        }

        // 业务：溃灭格与居城同格时可直接入城
        if (path is not null
            && path.Count == 1
            && releaseLocation.X == home.Location.X
            && releaseLocation.Y == home.Location.Y)
        {
            EnterStronghold(commander, home, meta, gameData);
            return;
        }

        // 业务：寻路失败时仍留在溃灭格，避免瞬移回居城
        commander.ActionStatus = CharacterActionStatus.Waiting;
    }

    /// <summary>将领抵达目标据点后进入在城状态（含当主居城判定）。</summary>
    public static void EnterStronghold(
        Character commander,
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        commander.ActionStatus = CharacterActionStatus.Waiting;
        commander.ActionTarget.RoutePoints.Clear();
        commander.ForceStatus = CharacterForceStatus.Idle;
        commander.LocationType = CharacterLocationType.Stronghold;
        commander.LocationStrongholdId = stronghold.Id;
        commander.Location = stronghold.Location;

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            stronghold.ForceId,
            meta,
            gameData);
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            stronghold.ForceId,
            gameData,
            meta);

        if (commander.Id == forceLordId)
        {
            // 业务：当主出访不改写名义居城 StrongholdId；回居城时再同步
            if (stronghold.Id == residenceId)
                StrategyStrongholdLordHelper.EnsureLordResidence(stronghold, commander);
        }
        else
        {
            commander.StrongholdId = stronghold.Id;
        }
    }

    private static Stronghold? ResolveHomeStronghold(
        Character commander,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            commander.ForceId,
            meta,
            gameData);
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            commander.ForceId,
            gameData,
            meta);

        if (commander.Id == forceLordId
            && residenceId > 0
            && gameData.Strongholds.TryGetValue(residenceId, out var lordResidence))
        {
            return lordResidence;
        }

        // 业务：非当主将领优先回所属据点（StrongholdId），其次当主居城，最后势力最大城
        if (commander.StrongholdId > 0
            && gameData.Strongholds.TryGetValue(commander.StrongholdId, out var affiliated)
            && affiliated.ForceId == commander.ForceId)
        {
            return affiliated;
        }

        if (residenceId > 0 && gameData.Strongholds.TryGetValue(residenceId, out var residence))
            return residence;

        return gameData.Strongholds.Values
            .Where(s => s.ForceId == commander.ForceId)
            .OrderByDescending(s => s.Population)
            .FirstOrDefault();
    }

    private static void CollectCommanderIdsRecursive(Unit unit, GameData gameData, HashSet<int> ids)
    {
        if (unit.LeaderId > 0)
            ids.Add(unit.LeaderId);

        foreach (var subUnitId in unit.SubUnitIds)
        {
            if (!gameData.Units.TryGetValue(subUnitId, out var subUnit))
                continue;

            CollectCommanderIdsRecursive(subUnit, gameData, ids);
        }
    }
}
