using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>农业收穫与进度计算。</summary>
public static class AgricultureCalculator
{
    public static int CalculateGrossHarvestGo(
        Stronghold stronghold,
        HarvestEventDefinition harvestEvent,
        int cycleProgressBp)
    {
        var potential = stronghold.CivilianActor.AgricultureProduction;
        var share = harvestEvent.ShareBasisPoints;
        var progress = Math.Clamp(cycleProgressBp, 0, AgricultureConstants.ProgressBasisPoints);

        return (int)((long)potential * share / AgricultureConstants.ProgressBasisPoints
                     * progress / AgricultureConstants.ProgressBasisPoints);
    }
}
