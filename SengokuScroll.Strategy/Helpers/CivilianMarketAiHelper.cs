using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市民缺粮时向本地市场挂买单（M4-b）。</summary>
public static class CivilianMarketAiHelper
{
    public static void EvaluateAndPlaceBuyOrders(Stronghold stronghold)
    {
        if (!MarketRules.CanTrade(stronghold))
            return;

        var qty = MarketCalculator.CalculateCivilianBuyQuantityGo(stronghold);
        if (qty <= 0)
            return;

        var limit = MarketCalculator.CalculateCivilianBuyLimitPrice(stronghold);
        MarketActions.UpsertCivilianBuyOrder(stronghold, limit, qty);
    }
}
