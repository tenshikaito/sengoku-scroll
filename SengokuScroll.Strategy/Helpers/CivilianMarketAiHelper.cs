using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市民聚合买盘：多档限价求购；低价卖盘触发捡漏采购。</summary>
public static class CivilianMarketAiHelper
{
    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        var commodity = MarketCommodityType.Food;
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, commodity);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, commodity);
        var qty = MarketCalculator.CalculateCivilianBuyQuantityGo(stronghold);
        if (qty <= 0)
            qty = MarketCalculator.CalculateBargainBuyQuantityGo(stronghold, reference, bestAsk);

        if (qty <= 0)
        {
            MarketMakerAiHelper.SyncBookSide(
                stronghold,
                stronghold.CivilianActor.Id,
                MarketRules.BuySide,
                commodity,
                taxExempt: true,
                []);
            return;
        }

        var nearReference = MarketMakerAiHelper.PreferNearReferenceBids(stronghold);
        var skew = nearReference
            ? MarketMakerAiHelper.BookSkew.NearReference
            : MarketMakerAiHelper.BookSkew.FarReference;
        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            stronghold.CivilianActor.Id,
            qty,
            MarketConstants.CivilianMaxBuyFoodGoPerLevel,
            MarketConstants.GovernmentMinSellQuantityGo,
            skew,
            asksAboveReference: false,
            bestAsk);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            stronghold.CivilianActor.Id,
            MarketRules.BuySide,
            commodity,
            taxExempt: true,
            allocations);
    }
}
