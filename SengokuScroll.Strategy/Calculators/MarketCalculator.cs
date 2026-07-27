using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
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
        bool SellerInventoryCommitted,
        bool BuyerMoneyCommitted,
        MarketCommodityType Commodity);

    /// <summary>撮合会话结果：成交列表与 OHLC 行情。</summary>
    public sealed record MatchResult(
        IReadOnlyList<TradeExecution> Trades,
        int SessionOpen,
        int SessionHigh,
        int SessionLow,
        int SessionClose,
        int TotalVolumeGo);

    /// <summary>连续撮合：全部可交易商品。</summary>
    public static MarketCalculator.MatchResult MatchOrders(
        Stronghold stronghold,
        int fallbackPrice,
        GameDate? gameDate = null)
    {
        var trades = new List<TradeExecution>();
        var open = 0;
        var high = 0;
        var low = int.MaxValue;
        var close = fallbackPrice;
        var volume = 0;

        foreach (var commodity in Enum.GetValues<MarketCommodityType>())
        {
            var commodityFallback = stronghold.Market.ResolveLastClose(commodity);
            if (commodityFallback <= 0)
                commodityFallback = CommodityInventoryHelper.ResolveDefaultPrice(null, commodity);

            var partial = MatchOrdersForCommodity(stronghold, commodityFallback, commodity, gameDate);
            if (partial.Trades.Count == 0)
                continue;

            trades.AddRange(partial.Trades);
            if (open == 0)
                open = partial.SessionOpen;
            high = Math.Max(high, partial.SessionHigh);
            low = Math.Min(low, partial.SessionLow == int.MaxValue ? partial.SessionClose : partial.SessionLow);
            if (partial.TotalVolumeGo > 0)
                close = partial.SessionClose;
            volume += partial.TotalVolumeGo;
        }

        if (trades.Count == 0)
            return MatchOrdersForCommodity(stronghold, fallbackPrice, MarketCommodityType.Food, gameDate);

        return new MatchResult(
            trades,
            open,
            high,
            low == int.MaxValue ? close : low,
            close,
            volume);
    }

    public static MatchResult MatchOrdersForCommodity(
        Stronghold stronghold,
        int fallbackPrice,
        MarketCommodityType commodity,
        GameDate? gameDate)
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
                sell.InventoryCommitted,
                buy.MoneyCommitted,
                commodity));

            buy.QuantityGo -= qty;
            sell.QuantityGo -= qty;
            if (buy.MoneyCommitted)
                buy.CommittedMoneyGo = Math.Max(0, buy.CommittedMoneyGo - price * qty);
            if (sell.InventoryCommitted)
                sell.CommittedInventoryGo = Math.Max(0, sell.CommittedInventoryGo - qty);
            volume += qty;

            if (open == 0)
                open = price;
            high = Math.Max(high, price);
            low = Math.Min(low, price);
            close = price;

            if (buy.QuantityGo <= 0)
            {
                if (gameDate is not null)
                    MarketActions.MarkOrderFullyFilled(buy, gameDate.Value);
                buys.RemoveAt(0);
            }

            if (sell.QuantityGo <= 0)
            {
                if (gameDate is not null)
                    MarketActions.MarkOrderFullyFilled(sell, gameDate.Value);
                sells.RemoveAt(0);
            }
        }

        MarketActions.RemoveZeroQuantityOrders(stronghold, gameDate);

        if (trades.Count == 0)
        {
            var last = fallbackPrice > 0
                ? fallbackPrice
                : CommodityInventoryHelper.ResolveDefaultPrice(null, commodity);
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

    /// <summary>低价卖盘触发的小额捡漏采购（合）。</summary>
    public static int CalculateBargainBuyQuantityGo(Stronghold stronghold, int referencePrice, int bestAsk)
    {
        if (bestAsk <= 0)
            return 0;

        var fairReference = Math.Max(
            referencePrice,
            MarketMakerAiHelper.ResolveFairReferencePrice(stronghold, MarketCommodityType.Food));
        if (fairReference <= 0 || bestAsk > fairReference)
            return 0;

        var discountBp = (fairReference - bestAsk) * EconomyConstants.BasisPointsPer100Percent / fairReference;
        if (discountBp < MarketConstants.BargainBuyDiscountThresholdBp)
            return 0;

        var daily = LogisticsCalculator.CalculateCivilianDailyFoodConsumption(stronghold.Population);
        if (daily <= 0)
            return 0;

        return Math.Min(
            daily * MarketConstants.BargainBuyCoverDays,
            MarketConstants.CivilianMaxBuyFoodGoPerLevel);
    }

    /// <summary>市民最高有效买价：紧缺时近中枢一档，平时最远档。</summary>
    public static int CalculateCivilianBuyLimitPrice(Stronghold stronghold)
    {
        var reference = MarketMakerAiHelper.ResolveReferencePrice(stronghold);
        var depth = MarketMakerAiHelper.ResolveMaxDepthLevels(
            stronghold.CivilianActor.Id,
            MarketCalculator.CalculateCivilianBuyQuantityGo(stronghold),
            MarketConstants.GovernmentMinSellQuantityGo);

        return MarketMakerAiHelper.PreferNearReferenceBids(stronghold)
            ? MarketMakerAiHelper.BidPrice(reference, 1)
            : MarketMakerAiHelper.BidPrice(reference, depth);
    }

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
}
