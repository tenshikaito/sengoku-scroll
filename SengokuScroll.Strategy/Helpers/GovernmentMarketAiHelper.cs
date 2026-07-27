using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>官府在本据点市场多档挂单：余粮释出（卖高于中枢）、储备不足收粮（买低于中枢）。</summary>
public static class GovernmentMarketAiHelper
{
    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        var surplus = MarketCalculator.CalculateGovernmentSellQuantityGo(stronghold);
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
        PlaceMultiLevelSells(stronghold, stronghold.ForceActor.Id, sellBudget, taxExempt: true);
    }

    /// <summary>粮库低于储备时，官府在中枢下方多档挂买单补库。</summary>
    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        var reserve = MarketConstants.GovernmentFoodReserveGo;
        var deficit = reserve - stronghold.ForceActor.Food;
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, MarketCommodityType.Food);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, MarketCommodityType.Food);
        if (deficit < MarketConstants.GovernmentMinSellQuantityGo
            && MarketCalculator.CalculateBargainBuyQuantityGo(stronghold, reference, bestAsk) > 0)
        {
            deficit = MarketConstants.GovernmentMinSellQuantityGo;
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
            taxExempt: true);
    }

    internal static void PlaceMultiLevelSells(
        Stronghold stronghold,
        int actorId,
        int totalQuantityGo,
        bool taxExempt)
    {
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold);
        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            actorId,
            totalQuantityGo,
            MarketConstants.GovernmentMaxSellQuantityGo,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.NearReference,
            asksAboveReference: true);

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
        bool taxExempt)
    {
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, MarketCommodityType.Food);
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, MarketCommodityType.Food);
        var skew = nearReferencePreferred
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
            bestAsk);

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
