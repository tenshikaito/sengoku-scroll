using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Actions;

/// <summary>收粮日农业税与产出划分（M4-b）。</summary>
public static class HarvestEconomyActions
{
    /// <summary>收粮结算结果：总产量、税粮、市民余粮、贡赋义务。</summary>
    public sealed record HarvestSettlementResult(
        int GrossHarvestGo,
        int TaxFoodGo,
        int CivilianFoodGo,
        int TributeObligationGo);

    /// <summary>收粮：税入府库，余粮入市民；返回贡赋义务（合）。</summary>
    public static HarvestSettlementResult ApplyHarvestSettlement(
        Stronghold stronghold,
        HarvestEventDefinition harvestEvent,
        int tributeFoodBasisPoints)
    {
        var gross = HarvestCalculator.CalculateGrossHarvestGo(stronghold, harvestEvent);
        if (gross <= 0)
            return new HarvestSettlementResult(0, 0, 0, 0);

        var tax = HarvestCalculator.CalculateHarvestTaxFoodGo(stronghold, gross);
        tax = Math.Min(tax, gross);
        var civilian = gross - tax;

        stronghold.ForceActor.Food += tax;
        stronghold.CivilianActor.Food += civilian;

        // 业务：未配置贡赋比例时使用势力内部默认万分比
        var tributeBp = tributeFoodBasisPoints > 0
            ? tributeFoodBasisPoints
            : HarvestConstants.DefaultInternalTributeFoodBp;
        var obligation = HarvestCalculator.CalculateTributeFoodObligationGo(gross, tributeBp);

        return new HarvestSettlementResult(gross, tax, civilian, obligation);
    }
}
