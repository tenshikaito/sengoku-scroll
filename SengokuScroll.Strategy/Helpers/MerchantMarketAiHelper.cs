using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>商户做市：本城低买高卖；感知最低卖价（best ask）。</summary>
public static class MerchantMarketAiHelper
{
    public static void EvaluateAndPlaceOrders(
        Stronghold stronghold,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        foreach (var merchant in stronghold.MerchantActors)
            EvaluateMerchant(stronghold, merchant, commodity);
    }

    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
        => EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);

    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
        => EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);

    private static void EvaluateMerchant(
        Stronghold stronghold,
        StrongholdActor merchant,
        MarketCommodityType commodity)
    {
        PlaceBids(stronghold, merchant, commodity);
        PlaceAsks(stronghold, merchant, commodity);
    }

    private static void PlaceBids(
        Stronghold stronghold,
        StrongholdActor merchant,
        MarketCommodityType commodity)
    {
        var moneyBudget = merchant.Money - MarketConstants.MerchantMoneyOperatingReserve;
        moneyBudget = Math.Min(moneyBudget, MarketConstants.MerchantMaxTotalBuyMoney);

        if (moneyBudget < MarketConstants.GovernmentMinSellQuantityGo)
        {
            ClearSide(stronghold, merchant.Id, MarketRules.BuySide, commodity);
            return;
        }

        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, commodity);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, commodity);
        var maxPerLevel = commodity == MarketCommodityType.Horse
            ? MarketConstants.GovernmentMaxHorseBuyQuantity
            : MarketConstants.MerchantMaxBuyFoodGoPerLevel;

        var allocations = MarketMakerAiHelper.BuildMoneyBidAllocations(
            reference,
            merchant.Id,
            moneyBudget,
            maxPerLevel,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.FarReference,
            bestAsk);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            merchant.Id,
            MarketRules.BuySide,
            commodity,
            taxExempt: false,
            allocations);
    }

    private static void PlaceAsks(
        Stronghold stronghold,
        StrongholdActor merchant,
        MarketCommodityType commodity)
    {
        var stock = CommodityInventoryHelper.GetStock(merchant, commodity);
        var reserve = commodity == MarketCommodityType.Horse
            ? MarketConstants.MerchantHorseReserve
            : MarketConstants.MerchantFoodReserveGo;
        var maxTotal = commodity == MarketCommodityType.Horse
            ? MarketConstants.GovernmentMaxHorseSellQuantity
            : MarketConstants.MerchantMaxTotalSellFoodGo;
        var maxPerLevel = commodity == MarketCommodityType.Horse
            ? MarketConstants.GovernmentMaxHorseSellQuantity
            : MarketConstants.MerchantMaxSellFoodGoPerLevel;

        var sellBudget = Math.Min(stock - reserve, maxTotal);
        if (sellBudget < MarketConstants.GovernmentMinSellQuantityGo)
        {
            ClearSide(stronghold, merchant.Id, MarketRules.SellSide, commodity);
            return;
        }

        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, commodity);
        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            merchant.Id,
            sellBudget,
            maxPerLevel,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.NearReference,
            asksAboveReference: true);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            merchant.Id,
            MarketRules.SellSide,
            commodity,
            taxExempt: false,
            allocations);
    }

    private static void ClearSide(
        Stronghold stronghold,
        int actorId,
        string side,
        MarketCommodityType commodity)
        => MarketMakerAiHelper.SyncBookSide(stronghold, actorId, side, commodity, taxExempt: false, []);
}
