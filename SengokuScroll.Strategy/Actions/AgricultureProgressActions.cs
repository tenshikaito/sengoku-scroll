using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>农业分季进度：农忙期按劳力比日更。</summary>
public static class AgricultureProgressActions
{
    public static void AdvanceDailyProgress(
        Stronghold stronghold,
        GameData gameData,
        GameDate date,
        IReadOnlyDictionary<int, RegionHarvestProfile> regionProfiles,
        int regionId)
    {
        stronghold.Agriculture ??= new StrongholdAgricultureState();

        var regionPattern = AgricultureCropRules.ResolveRegionCropPattern(regionProfiles, regionId);
        var effectivePattern = AgricultureCropRules.ResolveEffectiveCropPattern(stronghold, regionPattern);
        var activePhase = AgricultureCropRules.ResolveActivePhase(date, effectivePattern);
        if (activePhase is null)
            return;

        var laborRatioBp = AgricultureLaborRules.CalculateLaborRatioBp(stronghold, gameData);
        var daily = AgricultureConstants.BaseDailyProgressBp
                    * activePhase.LaborWeightBp
                    / AgricultureConstants.ProgressBasisPoints
                    * laborRatioBp
                    / AgricultureConstants.ProgressBasisPoints;

        if (daily <= 0)
            return;

        var cycleIndex = activePhase.CycleIndex;
        var current = stronghold.Agriculture.GetProgressBp(cycleIndex);
        var cap = stronghold.Agriculture.GetProgressCapBp(cycleIndex);
        var next = Math.Min(cap, current + daily);
        stronghold.Agriculture.SetProgressBp(cycleIndex, next);

        if (activePhase.LaborWeightBp >= AgricultureConstants.CriticalLaborWeightBp
            && laborRatioBp < AgricultureConstants.ProgressBasisPoints)
        {
            var penalty = AgricultureConstants.CriticalLaborMissCapPenaltyBp
                          * (AgricultureConstants.ProgressBasisPoints - laborRatioBp)
                          / AgricultureConstants.ProgressBasisPoints;
            var newCap = Math.Max(next, cap - penalty);
            stronghold.Agriculture.SetProgressCapBp(cycleIndex, newCap);
        }
    }
}
