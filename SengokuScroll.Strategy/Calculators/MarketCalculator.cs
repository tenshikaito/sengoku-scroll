using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>订单簿连续撮合纯计算（M4-b/d）。</summary>
public static class MarketCalculator
{
    /// <summary>单笔成交记录：买卖单撮合结果。</summary>
    public sealed record TradeExecution(
        int BuyOrderId,
        int SellOrderId,
        int PriceMoneyPerGo,
        int QuantityGo,
        int BuyerActorId,
        int SellerActorId,
        bool SellerTaxExempt,
        MarketCommodityType Commodity);

    /// <summary>撮合会话结果：成交列表与 OHLC 行情。</summary>
    public sealed record MatchResult(
        IReadOnlyList<TradeExecution> Trades,
        int SessionOpen,
        int SessionHigh,
        int SessionLow,
        int SessionClose,
        int TotalVolumeGo);

    /// <summary>连续撮合：按商品种类分别撮合。</summary>
    public static MatchResult MatchOrders(Stronghold stronghold, int fallbackPrice)
    {
        var food = MatchOrdersForCommodity(stronghold, fallbackPrice, MarketCommodityType.Food);
        var luxury = MatchOrdersForCommodity(stronghold, fallbackPrice, MarketCommodityType.Luxury);

        var trades = food.Trades.Concat(luxury.Trades).ToList();
        if (trades.Count == 0)
            return food;

        var open = food.SessionOpen > 0 ? food.SessionOpen : luxury.SessionOpen;
        var high = Math.Max(food.SessionHigh, luxury.SessionHigh);
        var low = Math.Min(
            food.SessionLow == int.MaxValue ? luxury.SessionLow : food.SessionLow,
            luxury.SessionLow == int.MaxValue ? food.SessionLow : luxury.SessionLow);
        var close = luxury.SessionClose > 0 && luxury.TotalVolumeGo > 0
            ? luxury.SessionClose
            : food.SessionClose;
        var volume = food.TotalVolumeGo + luxury.TotalVolumeGo;

        return new MatchResult(trades, open, high, low == int.MaxValue ? close : low, close, volume);
    }

    private static MatchResult MatchOrdersForCommodity(
        Stronghold stronghold,
        int fallbackPrice,
        MarketCommodityType commodity)
    {
        var market = stronghold.Market;
        var trades = new List<TradeExecution>();
        var buys = market.Orders
            .Where(o => MarketRules.IsBuyOrder(o) && o.Commodity == commodity)
            .OrderByDescending(o => o.PriceMoneyPerGo)
            .ThenBy(o => o.Id)
            .ToList();
        var sells = market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.Commodity == commodity)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ThenBy(o => o.Id)
            .ToList();

        var open = 0;
        var high = 0;
        var low = int.MaxValue;
        var close = fallbackPrice;
        var volume = 0;

        while (buys.Count > 0 && sells.Count > 0)
        {
            var buy = buys[0];
            var sell = sells[0];

            // 业务：最高买价 < 最低卖价时无法成交，停止撮合
            if (buy.PriceMoneyPerGo < sell.PriceMoneyPerGo)
                break;

            // 业务：成交价取卖单挂价，数量取买卖双方较小挂单量
            var price = sell.PriceMoneyPerGo;
            var qty = Math.Min(buy.QuantityGo, sell.QuantityGo);
            if (qty <= 0)
                break;

            trades.Add(new TradeExecution(
                buy.Id,
                sell.Id,
                price,
                qty,
                buy.ActorId,
                sell.ActorId,
                sell.TaxExempt,
                commodity));

            buy.QuantityGo -= qty;
            sell.QuantityGo -= qty;
            volume += qty;

            if (open == 0)
                open = price;
            high = Math.Max(high, price);
            low = Math.Min(low, price);
            close = price;

            if (buy.QuantityGo <= 0)
                buys.RemoveAt(0);
            if (sell.QuantityGo <= 0)
                sells.RemoveAt(0);
        }

        market.Orders.RemoveAll(o => o.QuantityGo <= 0);

        if (trades.Count == 0)
        {
            var last = fallbackPrice > 0 ? fallbackPrice : MarketConstants.DefaultPriceMoneyPerGo;
            return new MatchResult(trades, last, last, last, last, 0);
        }

        return new MatchResult(trades, open, high, low == int.MaxValue ? close : low, close, volume);
    }

    /// <summary>市民缺粮时建议买单量（合）。</summary>
    public static int CalculateCivilianBuyQuantityGo(Stronghold stronghold)
    {
        var daily = LogisticsCalculator.CalculateCivilianDailyFoodConsumption(stronghold.Population);
        if (daily <= 0)
            return 0;

        var daysRemaining = stronghold.CivilianActor.Food / daily;
        // 业务：余粮不足阈值天数时才挂买单
        if (daysRemaining >= MarketConstants.CivilianBuyOrderThresholdDays)
            return 0;

        // 业务：买单量 = 目标储备天数 × 日耗 − 当前余粮
        var target = daily * MarketConstants.CivilianBuyOrderCoverDays;
        return Math.Max(0, target - stronghold.CivilianActor.Food);
    }

    /// <summary>市民买单限价：收盘价 × 溢价系数。</summary>
    public static int CalculateCivilianBuyLimitPrice(Stronghold stronghold)
        => stronghold.Market.LastClosePriceMoneyPerGo > 0
            ? stronghold.Market.LastClosePriceMoneyPerGo * MarketConstants.CivilianBuyPricePremiumBp
              / EconomyConstants.BasisPointsPer100Percent
            : MarketConstants.DefaultPriceMoneyPerGo;

    /// <summary>官府可挂卖单的余粮（合）。</summary>
    public static int CalculateGovernmentSellQuantityGo(Stronghold stronghold)
    {
        // 业务：官府卖单 = 势力余粮 − 储备底线，有最低/最高挂单量限制
        var surplus = stronghold.ForceActor.Food - MarketConstants.GovernmentFoodReserveGo;
        if (surplus < MarketConstants.GovernmentMinSellQuantityGo)
            return 0;

        return Math.Min(surplus, MarketConstants.GovernmentMaxSellQuantityGo);
    }

    /// <summary>官府卖单挂价：优先取市场收盘价。</summary>
    public static int CalculateGovernmentSellPrice(Stronghold stronghold)
        => stronghold.Market.LastClosePriceMoneyPerGo > 0
            ? stronghold.Market.LastClosePriceMoneyPerGo
            : MarketConstants.DefaultPriceMoneyPerGo;

    /// <summary>奢侈品工坊日产（单位）。</summary>
    public static int CalculateDailyLuxuryProduction(Stronghold stronghold)
    {
        if (!EconomyFacilityRules.HasLuxuryWorkshop(stronghold))
            return 0;

        return Math.Max(0, stronghold.CivilianActor.CommerceProduction / MarketConstants.LuxuryProductionDivisor);
    }
}
