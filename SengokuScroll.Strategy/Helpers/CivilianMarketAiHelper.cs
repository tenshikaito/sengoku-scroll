using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市民聚合买盘：仓位/缺粮/捡漏；开战囤积时近价抢筹。</summary>
public static class CivilianMarketAiHelper
{
    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
        => EvaluateAndPlaceBuyOrders(stronghold, signals: default, context: null);

    public static void EvaluateAndPlaceBuyOrders(
        Stronghold stronghold,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        var commodity = MarketCommodityType.Food;
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, commodity);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, commodity);
        var qty = MarketCalculator.CalculateCivilianBuyQuantityGo(stronghold);
        if (qty <= 0)
            qty = MarketCalculator.CalculateBargainBuyQuantityGo(stronghold, reference, bestAsk);

        // 业务：开战囤积 — 即使口粮尚可，仍按仓位补买
        if (qty <= 0 && signals.HoardBiasBp > 0 && context is not null)
        {
            var target = MarketPositionAiHelper.ResolveTargetMoneyShareBp(signals);
            qty = MarketPositionAiHelper.CalculateBuyQuantityToBalance(
                stronghold.CivilianActor,
                reference,
                target,
                commodity);
        }

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

        if (signals.DumpBiasBp > 0)
            qty = Math.Max(MarketConstants.GovernmentMinSellQuantityGo, qty / 2);

        var nearReference = MarketMakerAiHelper.PreferNearReferenceBids(stronghold)
                            || signals.HoardBiasBp > 0
                            || signals.PriceCrashObserved;
        var skew = nearReference
            ? MarketMakerAiHelper.BookSkew.NearReference
            : MarketMakerAiHelper.BookSkew.FarReference;

        var bidReference = reference;
        if (context is not null)
        {
            var importCeiling = MarketRegionalArbitrageHelper.ResolveImportBidCeiling(
                stronghold,
                context.GameData,
                commodity);
            if (importCeiling > 0)
                bidReference = Math.Max(1, Math.Min(reference, importCeiling));
        }

        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            bidReference,
            stronghold.CivilianActor.Id,
            qty,
            MarketConstants.CivilianMaxBuyFoodGoPerLevel,
            MarketConstants.GovernmentMinSellQuantityGo,
            skew,
            asksAboveReference: false,
            bestAsk,
            crossAtReference: nearReference);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            stronghold.CivilianActor.Id,
            MarketRules.BuySide,
            commodity,
            taxExempt: true,
            allocations);
    }
}
