using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Rules;

/// <summary>从居城 SubUnit 池组建 Unit 的校验。</summary>
public static class UnitComposeRules
{
    public static GameResult ValidateCompose(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData,
        int playerForceId,
        int commanderId,
        IReadOnlyList<StrategyDeployCompositionEntry> composition,
        bool deployToMap,
        bool requireLordResidence = true)
    {
        if (stronghold.ForceId != playerForceId)
            return GameError.DiplomacyError.NotSelfForce;

        if (requireLordResidence
            && !StrategyStrongholdLordHelper.IsForceLordResidence(stronghold, meta, gameData))
        {
            return GameError.StrongholdError.StrongholdNotFound;
        }

        if (composition.Count == 0)
            return GameError.DataNotFound;

        var total = composition.Sum(c => c.Soldiers);
        if (total <= 0)
            return GameError.DataNotFound;

        var pools = StrongholdMilitaryBootstrapHelper.ListGarrisonTroopPools(stronghold, gameData)
            .ToDictionary(p => p.TypeId, p => p.Soldiers);

        foreach (var entry in composition)
        {
            if (entry.Soldiers <= 0)
                continue;

            var available = pools.GetValueOrDefault(entry.TypeId);
            if (entry.Soldiers > available)
                return GameError.StrongholdError.InsufficientGarrisonTroops;
        }

        if (deployToMap
            && gameData.Units.Values.Any(u =>
                u.IsMilitary
                && u.Soldier > 0
                && !u.InStronghold
                && u.Location.IsSameTile(stronghold.Location)))
        {
            return GameError.MovementError.UnitAlreadyExistsInTile;
        }

        if (commanderId > 0)
        {
            if (!gameData.Characters.TryGetValue(commanderId, out var commander))
                return GameError.CharacterError.CharacterNotFound;

            if (commander.ForceId != playerForceId
                || !UnitCommanderHelper.IsAvailableForDeployment(commander, stronghold.Id))
            {
                return GameError.CharacterError.CharacterNotFound;
            }
        }

        return GameResult.Ok();
    }
}
