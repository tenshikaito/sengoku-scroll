using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>玩家/即时成交后刷新指定商品的 AI 挂单并撮合（避免砸单后盘口真空）。</summary>
public static class MarketAiRefreshHelper
{
    public static void RefreshAfterTrade(
        Stronghold stronghold,
        GameData gameData,
        MarketCommodityType commodity,
        MerchantTaxLedger taxLedger)
    {
        if (!MarketRules.CanTrade(stronghold, gameData))
            return;

        MarketActions.RemoveDeprecatedCommodityOrders(stronghold);
        EvaluateAiOrders(stronghold, commodity);

        var fallback = stronghold.Market.ResolveLastClose(commodity);
        if (fallback <= 0)
            fallback = CommodityInventoryHelper.ResolveDefaultPrice(null, commodity);

        var result = MarketCalculator.MatchOrdersForCommodity(
            stronghold,
            fallback,
            commodity,
            gameData.GameDate);
        MarketActions.ApplyMatchResult(stronghold, result, taxLedger);
    }

    private static void EvaluateAiOrders(Stronghold stronghold, MarketCommodityType commodity)
    {
        switch (commodity)
        {
            case MarketCommodityType.Food:
                CivilianMarketAiHelper.EvaluateAndPlaceBuyOrders(stronghold);
                GovernmentMarketAiHelper.EvaluateAndPlaceBuyOrders(stronghold);
                GovernmentMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold);
                MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);
                break;
            case MarketCommodityType.Horse:
                HorseMarketAiHelper.EvaluateAndPlaceOrders(stronghold);
                MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Horse);
                break;
        }
    }
}
