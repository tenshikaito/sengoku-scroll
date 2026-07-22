using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Actions;

/// <summary>移民队抵达后的据点人口与民心/治安调整。</summary>
public static class MigrantConvoyActions
{
    public static void CompleteMigrantArrival(SupplyConvoy convoy, Stronghold destination)
    {
        if (convoy.CargoPopulation <= 0)
            return;

        destination.Population += convoy.CargoPopulation;
        destination.Stability = (byte)Math.Max(
            0,
            destination.Stability - MigrantConstants.DestinationStabilityPenalty);
        convoy.CargoPopulation = 0;
    }

    public static void ApplyOriginDepartureEffects(Stronghold origin, int migrants)
    {
        if (migrants <= 0)
            return;

        origin.Population = Math.Max(0, origin.Population - migrants);
        origin.CivilianActor.PopularFeelings = (byte)Math.Min(
            100,
            origin.CivilianActor.PopularFeelings + MigrantConstants.OriginPopularFeelingsRelief);
    }
}
