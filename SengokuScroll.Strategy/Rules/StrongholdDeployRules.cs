using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Rules;

/// <summary>居城出征校验。</summary>
public static class StrongholdDeployRules
{
    public static GameResult ValidateDeploy(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData,
        int playerForceId,
        int commanderId,
        IReadOnlyList<StrategyDeployCompositionEntry> composition)
    {
        if (stronghold.ForceId != playerForceId)
            return GameError.DiplomacyError.NotSelfForce;

        if (!StrategyStrongholdLordHelper.IsForceLordResidence(stronghold, meta, gameData))
            return GameError.StrongholdError.StrongholdNotFound;

        if (composition.Count == 0)
            return GameError.DataNotFound;

        var total = composition.Sum(c => c.Soldiers);
        if (total <= 0 || total > StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold))
            return GameError.DataNotFound;

        if (gameData.Units.Values.Any(u =>
                u.IsMilitary && u.Soldier > 0 && u.Location.IsSameTile(stronghold.Location)))
            return GameError.MovementError.UnitAlreadyExistsInTile;

        if (!gameData.Characters.TryGetValue(commanderId, out var commander))
            return GameError.CharacterError.CharacterNotFound;

        if (commander.ForceId != playerForceId
            || !UnitCommanderHelper.IsAvailableForDeployment(commander, stronghold.Id))
            return GameError.CharacterError.CharacterNotFound;

        return GameResult.Ok();
    }
}

/// <summary>出征编组条目（API/Host 共用）。</summary>
public sealed class StrategyDeployCompositionEntry
{
    public required int TypeId { get; init; }

    public string? TypeName { get; init; }

    public required int Soldiers { get; init; }

    public int? CommanderId { get; init; }
}
