using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市场窗口快照：10 档深度 + 日 K（M4 UI）。</summary>
public static class MarketSnapshotHelper
{
    public static StrategyMarketSnapshotDto BuildSnapshot(
        Stronghold stronghold,
        GameData gameData,
        MarketCommodityType commodity,
        int depthLevels = MarketBootstrapHelper.DemoDepthLevels,
        int playerForceId = 0)
    {
        var market = stronghold.Market;
        var fallback = market.ResolveLastClose(commodity);
        if (fallback <= 0)
            fallback = CommodityInventoryHelper.ResolveDefaultPrice(null, commodity);

        var bookQuote = MarketDepthDisplayHelper.ResolveBookQuote(market, commodity, fallback);
        var sessionPrice = bookQuote.QuotePriceMoneyPerGo;
        var closeLevelQuantityGo = MarketDepthDisplayHelper.ResolveCloseLevelQuantityGo(
            market,
            commodity,
            sessionPrice);
        var isOpen = MarketRules.IsMarketOpen(stronghold, gameData);
        var playerOpenOrders = BuildPlayerOpenOrders(stronghold, commodity, playerForceId, gameData.GameDate);

        return new StrategyMarketSnapshotDto
        {
            StrongholdId = stronghold.Id,
            StrongholdName = stronghold.Name,
            Commodity = commodity.ToString(),
            IsOpen = isOpen,
            LastClosePriceMoneyPerGo = fallback,
            SessionPriceMoneyPerGo = sessionPrice,
            BookQuoteSide = bookQuote.Side.ToString(),
            BestBidPriceMoneyPerGo = bookQuote.BestBidPriceMoneyPerGo,
            BestAskPriceMoneyPerGo = bookQuote.BestAskPriceMoneyPerGo,
            CloseLevelQuantityGo = closeLevelQuantityGo,
            BidLevels = MarketDepthDisplayHelper.BuildBidDisplayLevels(
                market,
                commodity,
                depthLevels,
                sessionPrice,
                closeLevelQuantityGo),
            AskLevels = MarketDepthDisplayHelper.BuildAskDisplayLevels(
                market,
                commodity,
                depthLevels,
                sessionPrice,
                closeLevelQuantityGo),
            DailyBars = BuildDailyBars(market, commodity, fallback),
            PlayerOpenOrders = playerOpenOrders,
        };
    }

    private static IReadOnlyList<StrategyMarketOpenOrderDto> BuildPlayerOpenOrders(
        Stronghold stronghold,
        MarketCommodityType commodity,
        int playerForceId,
        GameDate gameDate)
    {
        if (playerForceId <= 0 || stronghold.ForceId != playerForceId)
            return [];

        return stronghold.Market.Orders
            .Where(o => o.Commodity == commodity
                        && o.ActorId == stronghold.ForceActor.Id
                        && (o.QuantityGo > 0
                            || MarketActions.IsFilledOrderVisibleToday(stronghold, o, gameDate))
                        && (o.MoneyCommitted
                            || o.InventoryCommitted
                            || o.CreatedYear > 0))
            .OrderByDescending(o => o.Id)
            .Select(MapOpenOrder)
            .ToList();
    }

    internal static StrategyMarketOpenOrderDto MapOpenOrder(MarketOrder order)
    {
        var original = order.OriginalQuantityGo > 0 ? order.OriginalQuantityGo : order.QuantityGo;
        var filled = Math.Max(0, original - order.QuantityGo);
        var fillStatus = order.QuantityGo <= 0 && original > 0
            ? "Filled"
            : filled > 0 ? "Partial" : "Open";

        return new StrategyMarketOpenOrderDto
        {
            Id = order.Id,
            Side = order.Side,
            PriceMoneyPerGo = order.PriceMoneyPerGo,
            QuantityGo = order.QuantityGo,
            OriginalQuantityGo = original,
            FilledQuantityGo = filled,
            FillStatus = fillStatus,
            CreatedYear = order.CreatedYear,
            CreatedMonth = order.CreatedMonth,
            CreatedDay = order.CreatedDay,
        };
    }

    private static IReadOnlyList<StrategyMarketDailyBarDto> BuildDailyBars(
        StrongholdMarket market,
        MarketCommodityType commodity,
        int fallbackClose)
    {
        var history = market.ResolvePriceHistory(commodity);
        if (history.Count == 0)
            return [];

        return history
            .TakeLast(MarketBootstrapHelper.DemoHistoryDays)
            .Select(bar => new StrategyMarketDailyBarDto
            {
                Year = bar.Date.Year,
                Month = bar.Date.Month,
                Day = bar.Date.Day,
                Open = bar.Open,
                High = bar.High,
                Low = bar.Low,
                Close = bar.Close > 0 ? bar.Close : fallbackClose,
                VolumeGo = bar.VolumeGo,
                TurnoverMoney = bar.TurnoverMoney,
            })
            .ToList();
    }
}
