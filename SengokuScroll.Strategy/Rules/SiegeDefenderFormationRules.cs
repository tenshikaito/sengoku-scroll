using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>攻城开战时城主方自动组建 InStronghold 守军（同盟不自动组）。</summary>
public static class SiegeDefenderFormationRules
{
    public static Unit? TryEnsureOwnerDefenderUnit(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta? meta = null)
    {
        var existing = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData);
        if (existing is not null)
            return existing;

        if (gameData.Units.Values.Any(u => UnitStrongholdPresenceRules.IsOwnerMapDefenderOnTile(u, stronghold)))
            return null;

        if (!HasComposeableOwnerTroops(stronghold, gameData))
            return null;

        return AutoComposeOwnerInStrongholdUnit(context, stronghold, gameData, meta);
    }

    public static bool HasComposeableOwnerTroops(Stronghold stronghold, GameData gameData)
        => StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold) > 0
           || stronghold.ForceActor.SubUnitIds.Any(id =>
               gameData.SubUnits.TryGetValue(id, out var sub) && sub.UnitId == 0 && sub.Soldier > 0);

    private static Unit AutoComposeOwnerInStrongholdUnit(
        IGameWorldContext context,
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta? meta)
    {
        var composition = BuildFullOwnerComposition(stronghold, gameData);
        var commanderId = meta is null
            ? 0
            : ReserveCommanderRules.PickAutoComposeCommanderId(stronghold, gameData, meta);

        var result = UnitComposeActions.ComposeFromStronghold(
            context,
            stronghold,
            meta,
            gameData,
            stronghold.ForceId,
            unitName: $"{stronghold.Name}守军",
            commanderId,
            composition,
            deployToMap: false,
            requireLordResidence: false);

        return result.IsSuccess ? result.Value! : null!;
    }

    private static List<StrategyDeployCompositionEntry> BuildFullOwnerComposition(
        Stronghold stronghold,
        GameData gameData)
    {
        var pools = StrongholdMilitaryBootstrapHelper.ListGarrisonTroopPools(stronghold, gameData);
        return pools
            .Where(p => p.Soldiers > 0)
            .Select(p => new StrategyDeployCompositionEntry
            {
                TypeId = p.TypeId,
                TypeName = p.TypeName,
                Soldiers = p.Soldiers
            })
            .ToList();
    }
}
