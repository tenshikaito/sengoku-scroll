using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点维持费：据点类型基础费 + 城防设施维持费总和。</summary>
public static class StrongholdMaintenanceHelper
{
    public static int CalculateTypeMaintenanceMoney(Stronghold stronghold, GameMasterData masterData)
    {
        if (stronghold.TypeId != 0
            && masterData.StrongholdTypes.TryGetValue(stronghold.TypeId, out var typeDef))
        {
            return Math.Max(0, typeDef.Maintenance);
        }

        return EconomyCalculator.CalculateStrongholdMonthlyMaintenanceMoney(stronghold);
    }

    public static int CalculateDefenseFacilitiesMaintenanceMoney(Stronghold stronghold, GameMasterData masterData)
    {
        var total = 0;
        foreach (var typeId in stronghold.DefenseFacilityIds)
        {
            if (masterData.DefenseFacilityTypes.TryGetValue(typeId, out var facilityType))
                total += Math.Max(0, facilityType.Maintenance);
        }

        return total;
    }

    public static int CalculateTotalMaintenanceMoney(Stronghold stronghold, GameMasterData masterData)
        => CalculateTypeMaintenanceMoney(stronghold, masterData)
           + CalculateDefenseFacilitiesMaintenanceMoney(stronghold, masterData);

    /// <summary>写回 <see cref="Stronghold.Maintenance"/> 并同步城防值。</summary>
    public static void Sync(Stronghold stronghold, GameMasterData masterData)
    {
        stronghold.Maintenance = CalculateTotalMaintenanceMoney(stronghold, masterData);
        StrongholdDefenseRules.SyncDefenseValue(stronghold, masterData);
    }
}
