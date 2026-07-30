using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests.Fixtures;

/// <summary>测试用挂单：下单前补足 Actor 资金/库存以通过统一锁资校验。</summary>
public static class MarketTestOrderSeedHelper
{
    public static void PlaceBuy(
        Stronghold stronghold,
        int actorId,
        int priceMoneyPerGo,
        int quantityGo,
        MarketCommodityType commodity = MarketCommodityType.Food,
        bool taxExempt = true)
    {
        EnsureBuyFunds(stronghold, actorId, priceMoneyPerGo, quantityGo);
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.BuySide,
            actorId,
            priceMoneyPerGo,
            quantityGo,
            commodity,
            taxExempt);
    }

    public static void PlaceSell(
        Stronghold stronghold,
        int actorId,
        int priceMoneyPerGo,
        int quantityGo,
        MarketCommodityType commodity = MarketCommodityType.Food,
        bool taxExempt = true)
    {
        EnsureSellStock(stronghold, actorId, quantityGo, commodity);
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            actorId,
            priceMoneyPerGo,
            quantityGo,
            commodity,
            taxExempt);
    }

    public static void EnsureBuyFunds(
        Stronghold stronghold,
        int actorId,
        int priceMoneyPerGo,
        int quantityGo)
    {
        if (!MarketActions.TryGetActorPublic(stronghold, actorId, out var actor))
            return;

        actor.Money = Math.Max(actor.Money, priceMoneyPerGo * quantityGo);
    }

    public static void EnsureSellStock(
        Stronghold stronghold,
        int actorId,
        int quantityGo,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        if (!MarketActions.TryGetActorPublic(stronghold, actorId, out var actor))
            return;

        if (commodity == MarketCommodityType.Horse)
        {
            var reserve = actorId == stronghold.ForceActor.Id ? 0 : MarketConstants.MerchantHorseReserve;
            actor.Horse = Math.Max(actor.Horse, quantityGo + reserve);
            return;
        }

        var foodReserve = actorId == stronghold.ForceActor.Id
            ? MarketConstants.GovernmentFoodReserveGo
            : MarketConstants.MerchantFoodReserveGo;
        actor.Food = Math.Max(actor.Food, quantityGo + foodReserve);
    }
}
