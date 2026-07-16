using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>官府/官办企业在本地市场挂卖单（M4-c 贸易收入）。</summary>
public static class GovernmentMarketAiHelper
{
    public static void EvaluateAndPlaceSellOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        var qty = MarketCalculator.CalculateGovernmentSellQuantityGo(stronghold);
        if (qty <= 0)
        {
            RemoveGovernmentSellOrders(stronghold);
            return;
        }

        var price = MarketCalculator.CalculateGovernmentSellPrice(stronghold);
        MarketActions.UpsertGovernmentSellOrder(stronghold, price, qty);
    }

    private static void RemoveGovernmentSellOrders(Stronghold stronghold)
    {
        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsSellOrder(o)
            && o.ActorId == stronghold.ForceActor.Id);
    }
}
