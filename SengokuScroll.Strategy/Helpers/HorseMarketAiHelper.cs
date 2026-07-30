using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>马匹大宗做市（官府/商户；库存 <see cref="Actor.Horse"/>）。</summary>
public static class HorseMarketAiHelper
{
    public static void EvaluateAndPlaceOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        PlaceGovernmentHorseAsks(stronghold);
        PlaceGovernmentHorseBids(stronghold);
    }

    private static void PlaceGovernmentHorseAsks(Stronghold stronghold)
    {
        var actor = stronghold.ForceActor;
        var surplus = actor.Horse;
        if (surplus < MarketConstants.GovernmentMinSellQuantityGo)
            return;

        var sellBudget = Math.Min(surplus, MarketConstants.GovernmentMaxHorseSellQuantity);
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, MarketCommodityType.Horse);
        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            reference,
            actor.Id,
            sellBudget,
            MarketConstants.GovernmentMaxHorseSellQuantity,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.NearReference,
            asksAboveReference: true);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            actor.Id,
            MarketRules.SellSide,
            MarketCommodityType.Horse,
            taxExempt: true,
            allocations);
    }

    private static void PlaceGovernmentHorseBids(Stronghold stronghold)
    {
        var actor = stronghold.ForceActor;
        var bestAsk = MarketMakerAiHelper.ResolveBestAsk(stronghold, MarketCommodityType.Horse);
        if (bestAsk <= 0)
            return;

        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold, MarketCommodityType.Horse);
        if (bestAsk > reference)
            return;

        var moneyBudget = Math.Min(actor.Money / 10, MarketConstants.GovernmentMaxHorseBuyMoney);
        if (moneyBudget < bestAsk)
            return;

        var allocations = MarketMakerAiHelper.BuildMoneyBidAllocations(
            reference,
            actor.Id,
            moneyBudget,
            MarketConstants.GovernmentMaxHorseBuyQuantity,
            MarketConstants.GovernmentMinSellQuantityGo,
            MarketMakerAiHelper.BookSkew.NearReference,
            bestAsk);

        MarketMakerAiHelper.SyncBookSide(
            stronghold,
            actor.Id,
            MarketRules.BuySide,
            MarketCommodityType.Horse,
            taxExempt: true,
            allocations);
    }

    private static void ClearSide(Stronghold stronghold, int actorId, string side)
        => MarketActions.PruneAiRestingOrders(
            stronghold,
            actorId,
            side,
            MarketCommodityType.Horse,
            keepPrices: new HashSet<int>());
}
