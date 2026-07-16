using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>收粮与农业税纯计算（M4-b）。</summary>
public static class HarvestCalculator
{
    /// <summary>当次收粮总量（合）。</summary>
    public static int CalculateGrossHarvestGo(Stronghold stronghold, HarvestEventDefinition harvestEvent)
        => EconomyCalculator.ApplyBasisPointsShare(
            stronghold.CivilianActor.AgricultureProduction,
            harvestEvent.ShareBasisPoints);

    /// <summary>农业税入库 = 毛收粮 × 税率 × 征收效率。</summary>
    public static int CalculateHarvestTaxFoodGo(Stronghold stronghold, int grossHarvestGo)
    {
        // 业务：税率以百分比 ×100 转万分比
        var rateBp = stronghold.AgricultureTaxRate * 100;
        var grossTax = EconomyCalculator.ApplyBasisPointsTax(grossHarvestGo, rateBp);
        var efficiency = EconomyCalculator.CalculateCollectionEfficiencyBp(stronghold);
        return EconomyCalculator.ApplyBasisPointsTax(grossTax, efficiency);
    }

    /// <summary>贡赋义务（合）= 产出 × 比例。</summary>
    public static int CalculateTributeFoodObligationGo(int grossHarvestGo, int tributeBasisPoints)
        => EconomyCalculator.ApplyBasisPointsShare(grossHarvestGo, tributeBasisPoints);
}
