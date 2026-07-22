using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>玩家操控角色（当主）在大战略模式下的出城、移动与入城。</summary>
public static class CharacterPlayerActions
{
    /// <summary>当主自当前据点出城，现身于据点格。</summary>
    public static GameResult TryLeaveStronghold(
        IGameWorldContext worldContext,
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        int gateApCost,
        bool forceUnderBlockade,
        int simulationSeed,
        out string? riskMessage)
    {
        riskMessage = null;
        if (!IsPlayerLord(character, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (character.LocationType != CharacterLocationType.Stronghold)
            return GameError.MovementError.CannotMoveToTile;

        var strongholdId = character.LocationStrongholdId > 0
            ? character.LocationStrongholdId
            : character.StrongholdId;
        if (strongholdId <= 0
            || !gameData.Strongholds.TryGetValue(strongholdId, out var stronghold))
        {
            return GameError.StrongholdError.StrongholdNotFound;
        }

        var gate = ValidateAndPayGate(character, stronghold, gameData, gateApCost, forceUnderBlockade);
        if (!gate.IsSuccess)
            return gate.Error!;

        if (gameData.Units.Values.Any(u =>
                u.IsMilitary && u.Soldier > 0 && u.Location.IsSameTile(stronghold.Location)))
        {
            return GameError.MovementError.UnitAlreadyExistsInTile;
        }

        if (CharacterStrongholdGateRules.IsGateBlocked(stronghold, gameData))
            riskMessage = CharacterStrongholdGateRules.ApplyForcedGateRisk(
                character, stronghold, gameData, simulationSeed);

        character.LocationType = CharacterLocationType.Map;
        character.LocationStrongholdId = 0;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.ActionTarget.RoutePoints.Clear();
        character.ActionTarget.StrongholdId = 0;
        MapLocationActions.SetCharacterLocation(worldContext, character, stronghold.Location);
        return GameResult.Ok();
    }

    /// <summary>为角色寻路并进入移动状态；在城内则先出城再移动。</summary>
    public static GameResult TryOrderMove(
        IGameWorldContext worldContext,
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        IPathfindingService pathfinding,
        Point2 target,
        IReadOnlyList<Point2>? via = null)
    {
        if (!IsPlayerLord(character, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (character.LocationType == CharacterLocationType.Unit)
            return GameError.MovementError.CannotMoveToTile;

        if (character.LocationType == CharacterLocationType.Stronghold)
            return GameError.MovementError.CannotMoveToTile;

        var start = ResolveMoveStart(character, gameData);
        var stops = BuildStopList((Point2)start, target, via);
        var path = BuildPathThroughInternal(pathfinding, character, stops, (Point2)start);
        if (!path.IsSuccess)
            return path.Error!;

        character.ActionTarget.StrongholdId = ResolveTargetStrongholdId(gameData, target);
        character.ActionTarget.RoutePoints.Clear();
        foreach (var node in path.Value!.Skip(1))
            character.ActionTarget.RoutePoints.Enqueue(node.Location);

        character.ActionStatus = CharacterActionStatus.Moving;
        return GameResult.Ok();
    }

    /// <summary>当主在同格据点入城。</summary>
    public static GameResult TryEnterStronghold(
        Character character,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        int gateApCost,
        bool forceUnderBlockade,
        int simulationSeed,
        out string? riskMessage)
    {
        riskMessage = null;
        if (!IsPlayerLord(character, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (character.LocationType != CharacterLocationType.Map)
            return GameError.MovementError.CannotMoveToTile;

        if (character.Location.X != stronghold.Location.X
            || character.Location.Y != stronghold.Location.Y)
        {
            return GameError.MovementError.CannotMoveToTile;
        }

        var gate = ValidateAndPayGate(character, stronghold, gameData, gateApCost, forceUnderBlockade);
        if (!gate.IsSuccess)
            return gate.Error!;

        if (gameData.Units.Values.Any(u =>
                u.IsMilitary && u.Soldier > 0 && u.Location.IsSameTile(stronghold.Location)))
        {
            return GameError.MovementError.UnitAlreadyExistsInTile;
        }

        if (CharacterStrongholdGateRules.IsGateBlocked(stronghold, gameData))
            riskMessage = CharacterStrongholdGateRules.ApplyForcedGateRisk(
                character, stronghold, gameData, simulationSeed);

        UnitCommanderEscapeHelper.EnterStronghold(character, stronghold, meta, gameData);
        return GameResult.Ok();
    }

    private static GameResult ValidateAndPayGate(
        Character character,
        Stronghold stronghold,
        GameData gameData,
        int gateApCost,
        bool forceUnderBlockade)
    {
        if (CharacterStrongholdGateRules.IsGateBlocked(stronghold, gameData) && !forceUnderBlockade)
            return GameError.MovementError.StrongholdBlockaded;

        return CharacterStrongholdGateRules.TryPayGateAp(character, gateApCost);
    }

    private static bool IsPlayerLord(Character character, StrategyScenarioMeta meta, GameData gameData)
    {
        if (character.ForceId != meta.PlayerForceId || character.IsDead)
            return false;

        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        return lordId > 0 && character.Id == lordId;
    }

    public static Point2 ResolveMoveStartPoint(Character character, GameData gameData)
        => (Point2)ResolveMoveStart(character, gameData);

    public static GameResult<List<PathNode>> BuildPathThrough(
        IPathfindingService pathfinding,
        Character character,
        IReadOnlyList<Point2> stops,
        Point2 pathStart)
        => BuildPathThroughInternal(pathfinding, character, stops, pathStart);

    private static Point3 ResolveMoveStart(Character character, GameData gameData)
    {
        if (character.LocationType == CharacterLocationType.Stronghold
            && character.LocationStrongholdId > 0
            && gameData.Strongholds.TryGetValue(character.LocationStrongholdId, out var current))
        {
            return current.Location;
        }

        return character.Location;
    }

    private static int ResolveTargetStrongholdId(GameData gameData, Point2 target)
    {
        var at = gameData.Strongholds.Values.FirstOrDefault(s =>
            s.Location.X == target.X && s.Location.Y == target.Y);
        return at?.Id ?? 0;
    }

    private static List<Point2> BuildStopList(Point2 start, Point2 target, IReadOnlyList<Point2>? via)
    {
        var stops = new List<Point2>();
        if (via is not null)
            stops.AddRange(via);

        if (stops.Count == 0 || stops[^1] != target)
            stops.Add(target);

        if (stops.Count > 0 && stops[0] == start)
            stops.RemoveAt(0);

        return stops;
    }

    private static GameResult<List<PathNode>> BuildPathThroughInternal(
        IPathfindingService pathfinding,
        Character character,
        IReadOnlyList<Point2> stops,
        Point2 pathStart)
    {
        if (stops.Count == 0)
            return GameError.MovementError.CannotMoveToTile;

        var merged = new List<PathNode>();
        var segmentStart = pathStart;

        foreach (var stop in stops)
        {
            var segment = pathfinding.CalculatePathFrom(segmentStart, stop, character);
            if (segment is null || segment.Count <= 1)
                return GameError.MovementError.CannotMoveToTile;

            if (merged.Count == 0)
                merged.AddRange(segment);
            else
                merged.AddRange(segment.Skip(1));

            segmentStart = stop;
        }

        return merged;
    }
}
