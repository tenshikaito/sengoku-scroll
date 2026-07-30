using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>官府挂单：储备调节 + 开战囤积 / 丰收抛压 + 邻城套利锚价。</summary>
public static class GovernmentMarketAiHelper
{
    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
        => EvaluateAndPlaceSellOrders(stronghold, signals: default, context: null);

    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
        => EvaluateAndPlaceBuyOrders(stronghold, signals: default, context: null);

    public static void EvaluateAndPlaceSellOrders(
        Stronghold stronghold,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        // 业务：开战囤积时仍允许维护/补充卖盘（不再清空既有挂单）
        if (signals.HoardBiasBp > 0 && signals.DumpBiasBp <= 0)
        {
            // 跳过官府增卖逻辑，保留簿面
        }
        else
        {
            PlaceGovernmentSellBook(stronghold, signals, context);
        }
    }

    private static void PlaceGovernmentSellBook(
        Stronghold stronghold,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        var surplus = MarketCalculator.CalculateGovernmentSellQuantityGo(stronghold);
        if (signals.DumpBiasBp > 0 && surplus < MarketConstants.GovernmentMinSellQuantityGo)
        {
            surplus = Math.Min(
                Math.Max(0, stronghold.ForceActor.Food - MarketConstants.GovernmentFoodReserveGo / 2),
                MarketConstants.GovernmentMaxSellQuantityGo);
        }

        if (surplus <= 0)
        {
            MarketMakerAiHelper.SyncBookSide(
                stronghold,
                stronghold.ForceActor.Id,
                MarketRules.SellSide,
                MarketCommodityType.Food,
                taxExempt: true,
                []);
            return;
        }

        var sellBudget = Math.Min(surplus, MarketConstants.GovernmentMaxSellQuantityGo);
        if (signals.DumpBiasBp > 0)
            sellBudget = Math.Min(MarketConstants.GovernmentMaxSellQuantityGo, sellBudget * 2);

        PlaceMultiLevelSells(stronghold, stronghold.ForceActor.Id, sellBudget, taxExempt: true, signals, context);
    }

    public static void EvaluateAndPlaceBuyOrders(
        Stronghold stronghold,
        StrongholdMarketSignals signals,
        MarketAiDayContext? context)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        if (signals.DumpBiasBp > 0 && signals.HoardBiasBp <= 0)
        {
            MarketMakerAiHelper.SyncBookSide(
                stronghold,
                stronghold.ForceActor.Id,
                MarketRules.BuySide,
                MarketCommodityType.Food,
                taxExempt: true,
                []);
            return;
        }

        var reserve = MarketConstants.GovernmentFoodReserveGo;
        var deficit = reserve - stronghold.ForceActor.Food;
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, MarketCommodityType.Food);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, MarketCommodityType.Food);
        if (deficit < MarketConstants.GovernmentMinSellQuantityGo
            && MarketCalculator.CalculateBargainBuyQuantityGo(stronghold, reference, bestAsk) > 0)
        {
            deficit = MarketConstants.GovernmentMinSellQuantityGo;
        }

        if (deficit < MarketConstants.GovernmentMinSellQuantityGo && signals.HoardBiasBp > 0)
        {
            var target = MarketPositionAiHelper.ResolveTargetMoneyShareBp(signals);
            deficit = MarketPositionAiHelper.CalculateBuyQuantityToBalance(
                stronghold.ForceActor,
                reference,
                target,
                MarketCommodityType.Food);
        }

        if (deficit < MarketConstants.GovernmentMinSellQuantityGo)
        {
            MarketMakerAiHelper.SyncBookSide(
                stronghold,
                stronghold.ForceActor.Id,
                MarketRules.BuySide,
                MarketCommodityType.Food,
                taxExempt: true,
                []);
            return;
        }

        var buyBudgetGo = Math.Min(deficit, MarketConstants.GovernmentMaxBuyQuantityGo);
        if (signals.HoardBiasBp > 0)
            buyBudgetGo = Math.Min(MarketConstants.GovernmentMaxBuyQuantityGo * 2, buyBudgetGo * 2);

        var bidAnchor = bestAsk > 0 ? Math.Min(reference, bestAsk) : reference;
        var affordableGo = bidAnchor > 0
            ? stronghold.ForceActor.Money / bidAnchor
            : 0;
        buyBudgetGo = Math.Min(buyBudgetGo, affordableGo);

        if (buyBudgetGo < MarketConstants.GovernmentMinSellQuantityGo)
        {
            MarketMakerAiHelper.SyncBookSide(
                stronghold,
                stronghold.ForceActor.Id,
                MarketRules.BuySide,
                MarketCommodityType.Food,
                taxExempt: true,
                []);
            return;
        }

        PlaceMultiLevelBuys(
            stronghold,
            stronghold.ForceActor.Id,
            buyBudgetGo,
            nearReferencePreferred: true,
            taxExempt: true,
            signals,
            context);
    }

    internal static void PlaceMultiLevelSells(
        Stronghold stronghold,
        int actorId,
        int totalQuantityGo,
        bool taxExempt,
        StrongholdMarketSignals signals = default,
        MarketAiDayContext? context = null)
    {
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold);
        if (context is not null)
        {
            var exportFloor = MarketRegionalArbitrageHelper.ResolveExportAskFloor(
                stronghold,
                context.GameData);
            if (exportFloor > reference)
                reference = exportFloor;
        }

        var undercut = signals.DumpBiasBp > 0;
        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            actorId,
            totalQuantityGo,
            MarketConstants.GovernmentMaxSellQuantityGo,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.NearReference,
            asksAboveReference: !undercut,
            bestAsk: 0,
            crossAtReference: !undercut,
            bestBid: MarketMakerAiHelper.ResolveBestBid(stronghold, MarketCommodityType.Food, actorId),
            undercutAsks: undercut);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            actorId,
            MarketRules.SellSide,
            MarketCommodityType.Food,
            taxExempt,
            allocations);
    }

    internal static void PlaceMultiLevelBuys(
        Stronghold stronghold,
        int actorId,
        int totalQuantityGo,
        bool nearReferencePreferred,
        bool taxExempt,
        StrongholdMarketSignals signals = default,
        MarketAiDayContext? context = null)
    {
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, MarketCommodityType.Food);
        if (context is not null)
        {
            var importCeiling = MarketRegionalArbitrageHelper.ResolveImportBidCeiling(
                stronghold,
                context.GameData);
            if (importCeiling > 0)
                reference = Math.Max(1, Math.Min(reference, importCeiling));
        }

        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, MarketCommodityType.Food);
        var skew = nearReferencePreferred || signals.HoardBiasBp > 0
            ? MarketMakerAiHelper.BookSkew.NearReference
            : MarketMakerAiHelper.BookSkew.FarReference;

        var raw = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            actorId,
            totalQuantityGo,
            MarketConstants.GovernmentMaxBuyQuantityGo,
            MarketConstants.GovernmentMinSellQuantityGo,
            skew,
            asksAboveReference: false,
            bestAsk,
            crossAtReference: signals.HoardBiasBp > 0);

        var money = stronghold.ForceActor.Money;
        var capped = new List<MarketMakerAiHelper.LevelAllocation>();
        foreach (var level in raw)
        {
            var qty = MarketMakerAiHelper.QuantityAffordableByMoney(
                money,
                level.PriceMoneyPerGo,
                level.QuantityGo);
            if (qty <= 0)
                continue;

            capped.Add(new MarketMakerAiHelper.LevelAllocation(level.PriceMoneyPerGo, qty));
            money -= level.PriceMoneyPerGo * qty;
        }

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            actorId,
            MarketRules.BuySide,
            MarketCommodityType.Food,
            taxExempt,
            capped);
    }
}
