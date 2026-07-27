using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 市场盘口展示纯函数：现价=最后成交价；档位仅含 qty&gt;0，卖一向上/买一向下 N 档。
/// </summary>
public static class MarketDepthDisplayHelper
{
    /// <summary>最后成交价（K 线收盘优先）。</summary>
    public static int ResolveLastTradePrice(StrongholdMarket market, int fallbackClose)
    {
        if (market.PriceHistory.Count > 0)
        {
            var lastClose = market.PriceHistory[^1].Close;
            if (lastClose > 0)
                return lastClose;
        }

        if (fallbackClose > 0)
            return fallbackClose;

        return MarketConstants.DefaultPriceMoneyPerGo;
    }

    /// <summary>簿面状态（诊断用）；现价始终为最后成交价，不随卖一/买一改变。</summary>
    public static StrategyMarketBookQuote ResolveBookQuote(
        StrongholdMarket market,
        MarketCommodityType commodity,
        int fallbackClose)
    {
        var bids = GroupOrderQuantities(market, commodity, isBuy: true)
            .Where(x => x.Price > 0 && x.Quantity > 0)
            .OrderByDescending(x => x.Price)
            .ToList();
        var asks = GroupOrderQuantities(market, commodity, isBuy: false)
            .Where(x => x.Price > 0 && x.Quantity > 0)
            .OrderBy(x => x.Price)
            .ToList();

        var bestBid = bids.Count > 0 ? bids[0].Price : 0;
        var bestAsk = asks.Count > 0 ? asks[0].Price : 0;
        var lastTrade = ResolveLastTradePrice(market, fallbackClose);

        var side = (bestBid, bestAsk) switch
        {
            ( <= 0, <= 0) => StrategyMarketBookQuoteSide.Empty,
            ( <= 0, _) => StrategyMarketBookQuoteSide.Ask,
            (_, <= 0) => StrategyMarketBookQuoteSide.Bid,
            _ => StrategyMarketBookQuoteSide.Both,
        };

        return new StrategyMarketBookQuote(lastTrade, side, bestBid, bestAsk);
    }

    /// <summary>现价价位上的挂单总量（买卖合计；可为 0）。</summary>
    public static int ResolveCloseLevelQuantityGo(
        StrongholdMarket market,
        MarketCommodityType commodity,
        int lastTradePrice)
    {
        if (lastTradePrice <= 0)
            return 0;

        return market.Orders
            .Where(o => o.Commodity == commodity && o.PriceMoneyPerGo == lastTradePrice)
            .Sum(o => o.QuantityGo);
    }

    /// <summary>卖盘：自卖一（最低有量卖价）向上取 N 档；展示价高在上。</summary>
    public static IReadOnlyList<StrategyMarketDepthLevelDto> BuildAskDisplayLevels(
        StrongholdMarket market,
        MarketCommodityType commodity,
        int depthCount,
        int sessionPrice = 0,
        int closeLevelQuantityGo = 0)
    {
        var asks = ExcludeSessionPriceWhenEmpty(
                GroupOrderQuantities(market, commodity, isBuy: false)
                    .Where(x => x.Price > 0 && x.Quantity > 0),
                sessionPrice,
                closeLevelQuantityGo)
            .OrderBy(x => x.Price)
            .ToList();

        var selected = asks.Count <= depthCount
            ? asks
            : asks.Take(depthCount).ToList();

        var display = selected
            .OrderByDescending(x => x.Price)
            .Select(x => new StrategyMarketDepthLevelDto
            {
                PriceMoneyPerGo = x.Price,
                QuantityGo = x.Quantity,
            })
            .ToList();

        return PadDepthLevels(display, depthCount);
    }

    /// <summary>买盘：自买一（最高有量买价）向下取 N 档；展示价高在上。</summary>
    public static IReadOnlyList<StrategyMarketDepthLevelDto> BuildBidDisplayLevels(
        StrongholdMarket market,
        MarketCommodityType commodity,
        int depthCount,
        int sessionPrice = 0,
        int closeLevelQuantityGo = 0)
    {
        var selected = ExcludeSessionPriceWhenEmpty(
                GroupOrderQuantities(market, commodity, isBuy: true)
                    .Where(x => x.Price > 0 && x.Quantity > 0),
                sessionPrice,
                closeLevelQuantityGo)
            .OrderByDescending(x => x.Price)
            .Take(depthCount)
            .Select(x => new StrategyMarketDepthLevelDto
            {
                PriceMoneyPerGo = x.Price,
                QuantityGo = x.Quantity,
            })
            .ToList();

        return PadDepthLevels(selected, depthCount);
    }

    /// <summary>现价无量时不进入买卖档位（仅 K 线/图表参考，不占 5 档）。</summary>
    private static IEnumerable<(int Price, int Quantity)> ExcludeSessionPriceWhenEmpty(
        IEnumerable<(int Price, int Quantity)> levels,
        int sessionPrice,
        int closeLevelQuantityGo)
    {
        if (closeLevelQuantityGo > 0 || sessionPrice <= 0)
            return levels;

        return levels.Where(x => x.Price != sessionPrice);
    }

    private static List<(int Price, int Quantity)> GroupOrderQuantities(
        StrongholdMarket market,
        MarketCommodityType commodity,
        bool isBuy)
    {
        return market.Orders
            .Where(o => o.Commodity == commodity
                        && (isBuy ? MarketRules.IsBuyOrder(o) : MarketRules.IsSellOrder(o)))
            .GroupBy(o => o.PriceMoneyPerGo)
            .Select(g => (Price: g.Key, Quantity: g.Sum(o => o.QuantityGo)))
            .Where(x => x.Quantity > 0)
            .ToList();
    }

    private static IReadOnlyList<StrategyMarketDepthLevelDto> PadDepthLevels(
        IReadOnlyList<StrategyMarketDepthLevelDto> levels,
        int depthCount)
    {
        var rows = levels.ToList();
        while (rows.Count < depthCount)
        {
            rows.Add(new StrategyMarketDepthLevelDto
            {
                PriceMoneyPerGo = 0,
                QuantityGo = 0,
            });
        }

        return rows.Take(depthCount).ToList();
    }
}
