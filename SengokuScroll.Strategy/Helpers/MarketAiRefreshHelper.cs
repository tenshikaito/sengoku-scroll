using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 市场 AI 仅在日推进时刷新（<see cref="Systems.StrategyMarketSystem"/>）。
/// 玩家/商队即时成交只更新簿面与当日 K 线，不触发 AI 补单或二次撮合，避免同日多次「AI 行动」与多根 K 线。
/// </summary>
public static class MarketAiRefreshHelper
{
    /// <summary>已废弃：AI 补单与撮合改由每日 <see cref="Systems.StrategyMarketSystem"/> 统一执行。</summary>
    [Obsolete("AI order refresh and matching run only on daily StrategyMarketSystem tick.")]
    public static void RefreshAfterTrade(
        Stronghold stronghold,
        GameData gameData,
        MarketCommodityType commodity,
        MerchantTaxLedger taxLedger,
        StrategyScenarioMeta? scenarioMeta = null,
        GameWorld? world = null)
    {
        _ = stronghold;
        _ = gameData;
        _ = commodity;
        _ = taxLedger;
        _ = scenarioMeta;
        _ = world;
    }
}
