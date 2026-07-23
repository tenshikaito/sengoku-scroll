using SengokuScroll.Domain;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>收粮日农业税与产出划分（M4-b）。</summary>
public static class HarvestEconomyActions
{
    /// <summary>收粮结算结果：总产量、税粮、市民余粮、贡赋义务。</summary>
    public sealed record HarvestSettlementResult(
        int GrossHarvestGo,
        int TaxFoodGo,
        int CivilianFoodGo,
        int TributeObligationGo,
        int CycleProgressBp);

    /// <summary>收粮：税入府库，余粮入市民；返回贡赋义务（合）。</summary>
    public static HarvestSettlementResult ApplyHarvestSettlement(
        Stronghold stronghold,
        HarvestEventDefinition harvestEvent,
        int tributeFoodBasisPoints,
        GameData gameData,
        IReadOnlyDictionary<int, RegionHarvestProfile> regionProfiles,
        int regionId)
    {
        stronghold.Agriculture ??= new StrongholdAgricultureState();

        var events = regionId > 0 && regionProfiles.TryGetValue(regionId, out var profile)
            ? profile.Events
            : [harvestEvent];
        var cycleIndex = AgricultureCropRules.ResolveCycleIndex(events, harvestEvent);
        var progressBp = stronghold.Agriculture.GetProgressBp(cycleIndex);

        var gross = AgricultureCalculator.CalculateGrossHarvestGo(stronghold, harvestEvent, progressBp);
        if (gross <= 0)
        {
            stronghold.Agriculture.ResetCycleProgress(cycleIndex);
            return new HarvestSettlementResult(0, 0, 0, 0, progressBp);
        }

        var tax = HarvestCalculator.CalculateHarvestTaxFoodGo(stronghold, gross);
        tax = Math.Min(tax, gross);
        var civilian = gross - tax;

        stronghold.ForceActor.Food += tax;
        stronghold.CivilianActor.Food += civilian;

        var tributeBp = tributeFoodBasisPoints > 0
            ? tributeFoodBasisPoints
            : HarvestConstants.DefaultInternalTributeFoodBp;
        var obligation = HarvestCalculator.CalculateTributeFoodObligationGo(gross, tributeBp);

        stronghold.Agriculture.ResetCycleProgress(cycleIndex);

        return new HarvestSettlementResult(gross, tax, civilian, obligation, progressBp);
    }

    /// <summary>兼容旧测试：满进度收粮。</summary>
    public static HarvestSettlementResult ApplyHarvestSettlement(
        Stronghold stronghold,
        HarvestEventDefinition harvestEvent,
        int tributeFoodBasisPoints)
    {
        stronghold.Agriculture ??= new StrongholdAgricultureState();
        stronghold.Agriculture.SetProgressBp(0, AgricultureConstants.ProgressBasisPoints);

        var gross = AgricultureCalculator.CalculateGrossHarvestGo(
            stronghold,
            harvestEvent,
            AgricultureConstants.ProgressBasisPoints);
        if (gross <= 0)
            return new HarvestSettlementResult(0, 0, 0, 0, 0);

        var tax = HarvestCalculator.CalculateHarvestTaxFoodGo(stronghold, gross);
        tax = Math.Min(tax, gross);
        var civilian = gross - tax;

        stronghold.ForceActor.Food += tax;
        stronghold.CivilianActor.Food += civilian;

        var tributeBp = tributeFoodBasisPoints > 0
            ? tributeFoodBasisPoints
            : HarvestConstants.DefaultInternalTributeFoodBp;
        var obligation = HarvestCalculator.CalculateTributeFoodObligationGo(gross, tributeBp);

        return new HarvestSettlementResult(gross, tax, civilian, obligation, AgricultureConstants.ProgressBasisPoints);
    }
}
