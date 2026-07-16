using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>商户在本据点市场挂卖单（M4-d）。</summary>
public static class MerchantMarketAiHelper
{
    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        foreach (var merchant in stronghold.MerchantActors)
            EvaluateMerchant(stronghold, merchant);
    }

    private static void EvaluateMerchant(Stronghold stronghold, StrongholdActor merchant)
    {
        PlaceFoodSell(stronghold, merchant);
        PlaceLuxurySell(stronghold, merchant);
    }

    private static void PlaceFoodSell(Stronghold stronghold, StrongholdActor merchant)
    {
        var surplus = merchant.Food - MarketConstants.MerchantFoodReserveGo;
        if (surplus < MarketConstants.GovernmentMinSellQuantityGo)
        {
            RemoveSellOrders(stronghold, merchant.Id, MarketCommodityType.Food);
            return;
        }

        var qty = Math.Min(surplus, MarketConstants.MerchantMaxSellFoodGo);
        var price = MarketCalculator.CalculateGovernmentSellPrice(stronghold);

        MarketActions.UpsertSellOrder(
            stronghold,
            merchant.Id,
            price,
            qty,
            MarketCommodityType.Food,
            taxExempt: false);
    }

    private static void PlaceLuxurySell(Stronghold stronghold, StrongholdActor merchant)
    {
        if (merchant.LuxuryGoods <= 0)
        {
            RemoveSellOrders(stronghold, merchant.Id, MarketCommodityType.Luxury);
            return;
        }

        var qty = Math.Min(merchant.LuxuryGoods, MarketConstants.MerchantMaxSellFoodGo);
        var basePrice = MarketCalculator.CalculateGovernmentSellPrice(stronghold);
        var price = basePrice * MarketConstants.LuxuryPriceMultiplierBp
                    / EconomyConstants.BasisPointsPer100Percent;

        MarketActions.UpsertSellOrder(
            stronghold,
            merchant.Id,
            price,
            qty,
            MarketCommodityType.Luxury,
            taxExempt: false);
    }

    private static void RemoveSellOrders(
        Stronghold stronghold,
        int actorId,
        MarketCommodityType commodity)
    {
        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsSellOrder(o)
            && o.ActorId == actorId
            && o.Commodity == commodity);
    }
}
