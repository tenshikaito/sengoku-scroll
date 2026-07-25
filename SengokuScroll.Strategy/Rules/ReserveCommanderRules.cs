using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>备队大将（LeaderId=0）与自动组 Unit 时的将领优先级。</summary>
public static class ReserveCommanderRules
{
    public const string ReserveCommanderDisplayName = "备队大将";

    public static bool IsReserveCommander(Unit unit) => unit.LeaderId == 0;

    public static string ResolveCommanderDisplayName(Unit unit, GameData gameData)
    {
        if (unit.LeaderId > 0
            && gameData.Characters.TryGetValue(unit.LeaderId, out var commander)
            && !commander.IsDead)
        {
            return commander.Name;
        }

        return ReserveCommanderDisplayName;
    }

    /// <summary>城主方自动组 Unit 时按优先级选取将领；无可用者返回 0（备队大将）。</summary>
    public static int PickAutoComposeCommanderId(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var candidates = CollectIdleCommanderCandidates(stronghold, gameData, meta);
        if (candidates.Count == 0)
            return 0;

        return candidates
            .OrderByDescending(c => ResolveCommanderPriority(c, stronghold, meta, gameData))
            .ThenBy(c => c.Id)
            .First()
            .Id;
    }

    /// <summary>备队大将占位时，更高优先级将领入城后自动替换。</summary>
    public static bool TryUpgradeReserveCommander(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!IsReserveCommander(unit))
            return false;

        var commanderId = PickAutoComposeCommanderId(stronghold, gameData, meta);
        if (commanderId <= 0)
            return false;

        if (!gameData.Characters.TryGetValue(commanderId, out var commander))
            return false;

        unit.LeaderId = commanderId;
        UnitCommanderHelper.AttachToUnit(commander, unit);
        return true;
    }

    /// <summary>备队大将或低阶将领在位时，若城内有更高优先级待命将领则替换。</summary>
    public static bool TryUpgradeCommanderIfBetter(
        Unit unit,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var bestId = PickAutoComposeCommanderId(stronghold, gameData, meta);
        if (bestId <= 0)
            return false;

        var currentPriority = 0;
        if (unit.LeaderId > 0
            && gameData.Characters.TryGetValue(unit.LeaderId, out var current)
            && !current.IsDead)
        {
            currentPriority = ResolveCommanderPriority(current, stronghold, meta, gameData);
        }

        var bestPriority = gameData.Characters.TryGetValue(bestId, out var best)
            ? ResolveCommanderPriority(best, stronghold, meta, gameData)
            : 0;

        if (bestPriority <= currentPriority)
            return false;

        if (unit.LeaderId > 0
            && gameData.Characters.TryGetValue(unit.LeaderId, out var previous)
            && previous.ForceStatus == CharacterForceStatus.UnitAction)
        {
            UnitCommanderHelper.DetachFromStronghold(previous, stronghold.Id);
        }

        if (!gameData.Characters.TryGetValue(bestId, out var commander))
            return false;

        unit.LeaderId = bestId;
        UnitCommanderHelper.AttachToUnit(commander, unit);
        return true;
    }

    private static int ResolveCommanderPriority(
        Character character,
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (character.Id == stronghold.LordId && stronghold.LordId > 0)
            return 280;

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            stronghold.ForceId, meta, gameData);
        if (character.Id == forceLordId)
            return 300;

        if (character.Id == stronghold.LeaderId && stronghold.LeaderId > 0)
            return 250;

        if (character.ForceStatus == CharacterForceStatus.Idle)
            return 100;

        return 50;
    }

    private static List<Character> CollectIdleCommanderCandidates(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var result = new List<Character>();
        foreach (var character in gameData.Characters.Values)
        {
            if (character.IsDead || character.ForceId != stronghold.ForceId)
                continue;

            if (character.LocationType != CharacterLocationType.Stronghold
                || character.StrongholdId != stronghold.Id)
            {
                continue;
            }

            if (character.ForceStatus is CharacterForceStatus.Prisoner or CharacterForceStatus.UnitAction)
                continue;

            result.Add(character);
        }

        return result;
    }
}
