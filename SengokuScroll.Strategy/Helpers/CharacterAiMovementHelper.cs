using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>角色 AI 移动：自据点出城或地图格寻路至目标据点。</summary>
public static class CharacterAiMovementHelper
{
    /// <summary>
    /// 任命赴任等指令：只写入 Task 计划与目标据点，角色仍留在城内；
    /// 出城与寻路由 <see cref="StrategyCharacterAISystem"/> 在日推进时评估后执行。
    /// </summary>
    public static void ScheduleGovernanceTravelTask(Character character, Stronghold target)
    {
        character.StrongholdId = target.Id;
        character.ActionPlan = CharacterActionPlan.Task;
        character.ForceStatus = CharacterForceStatus.Task;
        character.ActionTarget.StrongholdId = target.Id;
        character.ActionTarget.RoutePoints.Clear();
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.LastAiCheckDate = default;
    }

    /// <summary>在据点内开始休息。</summary>
    public static bool TryBeginRest(Character character)
    {
        if (character.LocationType != CharacterLocationType.Stronghold)
            return false;

        character.ActionStatus = CharacterActionStatus.Resting;
        character.ActionTarget.RoutePoints.Clear();
        return true;
    }

    /// <summary>向目标据点移动（Visit / TaskRun）。</summary>
    public static bool TryRouteToStronghold(
        IGameWorldContext worldContext,
        Character character,
        Stronghold target,
        StrategyScenarioMeta meta,
        IPathfindingService pathfinding)
    {
        if (CharacterAiRules.IsAtStronghold(character, target))
        {
            if (character.LocationType == CharacterLocationType.Map)
            {
                UnitCommanderEscapeHelper.EnterStronghold(
                    character,
                    target,
                    meta,
                    worldContext.GameWorld.GameData);
            }

            character.ActionStatus = CharacterActionStatus.Waiting;
            return true;
        }

        var start = ResolveRouteStart(character, worldContext.GameWorld.GameData);
        var path = pathfinding.CalculatePathFrom((Point2)start, (Point2)target.Location, character);
        if (path is null || path.Count <= 1)
            return false;

        character.ActionTarget.StrongholdId = target.Id;
        character.ActionTarget.RoutePoints.Clear();

        foreach (var node in path.Skip(1))
            character.ActionTarget.RoutePoints.Enqueue(node.Location);

        if (character.LocationType == CharacterLocationType.Stronghold)
        {
            var strongholdLocation = start;
            character.LocationType = CharacterLocationType.Map;
            character.LocationStrongholdId = 0;
            MapLocationActions.SetCharacterLocation(worldContext, character, strongholdLocation);
        }

        character.ActionStatus = CharacterActionStatus.Moving;
        character.Ap = Math.Max(character.Ap, 4);
        return true;
    }

    private static Point3 ResolveRouteStart(Character character, GameData gameData)
    {
        if (character.LocationType == CharacterLocationType.Stronghold
            && character.LocationStrongholdId > 0
            && gameData.Strongholds.TryGetValue(character.LocationStrongholdId, out var current))
        {
            return current.Location;
        }

        return character.Location;
    }
}
