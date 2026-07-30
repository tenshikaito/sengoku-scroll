using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>商户做市：仓位低买高卖 + 邻城套利锚价 + 无买盘抛售；开战囤积时不抛。</summary>
public static class MerchantMarketAiHelper
{
    public static void EvaluateAndPlaceOrders(
        Stronghold stronghold,
        MarketCommodityType commodity = MarketCommodityType.Food)
        => EvaluateAndPlaceOrders(stronghold, commodity, signals: default, context: null);

    public static void EvaluateAndPlaceOrders(
        Stronghold stronghold,
        MarketCommodityType commodity,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        foreach (var merchant in stronghold.MerchantActors)
            EvaluateMerchant(stronghold, merchant, commodity, signals, context);
    }

    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
        => EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);

    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
        => EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);

    private static void EvaluateMerchant(
        Stronghold stronghold,
        StrongholdActor merchant,
        MarketCommodityType commodity,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        PlaceBids(stronghold, merchant, commodity, signals, context);
        PlaceAsks(stronghold, merchant, commodity, signals, context);
    }

    private static void PlaceBids(
        Stronghold stronghold,
        StrongholdActor merchant,
        MarketCommodityType commodity,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        if (signals.DumpBiasBp > 0 && signals.HoardBiasBp <= 0 && commodity == MarketCommodityType.Food)
        {
            ClearSide(stronghold, merchant.Id, MarketRules.BuySide, commodity);
            return;
        }

        var moneyBudget = merchant.Money - MarketConstants.MerchantMoneyOperatingReserve;
        moneyBudget = Math.Min(moneyBudget, MarketConstants.MerchantMaxTotalBuyMoney);
        if (signals.HoardBiasBp > 0)
            moneyBudget = Math.Min(merchant.Money, moneyBudget + moneyBudget / 2);

        if (moneyBudget < MarketConstants.GovernmentMinSellQuantityGo)
            return;

        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, commodity);
        if (context is not null && commodity == MarketCommodityType.Food)
        {
            var importCeiling = MarketRegionalArbitrageHelper.ResolveImportBidCeiling(
                stronghold,
                context.GameData,
                commodity);
            if (importCeiling > 0)
                reference = Math.Max(1, Math.Min(reference, importCeiling));
        }

        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, commodity);
        var maxPerLevel = commodity == MarketCommodityType.Horse
            ? MarketConstants.GovernmentMaxHorseBuyQuantity
            : MarketConstants.MerchantMaxBuyFoodGoPerLevel;

        var skew = signals.HoardBiasBp > 0 || signals.PriceCrashObserved
            ? MarketMakerAiHelper.BookSkew.NearReference
            : MarketMakerAiHelper.BookSkew.FarReference;

        var allocations = MarketMakerAiHelper.BuildMoneyBidAllocations(
            reference,
            merchant.Id,
            moneyBudget,
            maxPerLevel,
            MarketConstants.GovernmentMinSellQuantityGo,
            skew,
            bestAsk,
            crossAtReference: signals.HoardBiasBp > 0 || signals.PriceCrashObserved);

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
        MarketCommodityType commodity,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
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
        if (signals.DumpBiasBp > 0)
            sellBudget = Math.Min(maxTotal, Math.Max(sellBudget, (stock - reserve) * 2 / 3));

        if (sellBudget < MarketConstants.GovernmentMinSellQuantityGo)
            return;

        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, commodity);
        if (context is not null && commodity == MarketCommodityType.Food)
        {
            var exportFloor = MarketRegionalArbitrageHelper.ResolveExportAskFloor(
                stronghold,
                context.GameData,
                commodity);
            if (exportFloor > reference)
                reference = exportFloor;
        }

        var externalBestBid = MarketMakerAiHelper.ResolveBestBid(stronghold, commodity, merchant.Id);
        // 业务：囤积时不做低价抛售；丰收/无买盘时才 undercut 抢成交
        var undercutForFill = signals.HoardBiasBp <= 0
                              && (signals.DumpBiasBp > 0
                                  || externalBestBid <= 0
                                  || externalBestBid < reference);
        if (undercutForFill)
            ClearSide(stronghold, merchant.Id, MarketRules.BuySide, commodity);

        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            merchant.Id,
            sellBudget,
            maxPerLevel,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.NearReference,
            asksAboveReference: !undercutForFill,
            bestAsk: 0,
            crossAtReference: !undercutForFill,
            bestBid: externalBestBid,
            undercutAsks: undercutForFill);

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
        => MarketActions.PruneAiRestingOrders(
            stronghold,
            actorId,
            side,
            commodity,
            keepPrices: new HashSet<int>());
}
