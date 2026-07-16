using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>官办奢侈品工坊卖单（M4-d）。</summary>
public static class GovernmentLuxuryMarketAiHelper
{
    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        if (!EconomyFacilityRules.HasLuxuryWorkshop(stronghold))
            return;

        var qty = Math.Min(
            stronghold.ForceActor.LuxuryGoods,
            MarketConstants.GovernmentMaxLuxurySellQuantity);

        if (qty <= 0)
        {
            RemoveLuxurySellOrders(stronghold);
            return;
        }

        var basePrice = MarketCalculator.CalculateGovernmentSellPrice(stronghold);
        var price = basePrice * MarketConstants.LuxuryPriceMultiplierBp
                    / EconomyConstants.BasisPointsPer100Percent;

        MarketActions.UpsertSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            price,
            qty,
            MarketCommodityType.Luxury,
            taxExempt: true);
    }

    private static void RemoveLuxurySellOrders(Stronghold stronghold)
    {
        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsSellOrder(o)
            && o.ActorId == stronghold.ForceActor.Id
            && o.Commodity == MarketCommodityType.Luxury);
    }
}
