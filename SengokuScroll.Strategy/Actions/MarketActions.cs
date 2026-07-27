using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>市场成交与 K 线写入（M4-b/d）。</summary>
public static class MarketActions
{
    /// <summary>执行撮合结果并更新 Actor 库存；返回成交笔数。</summary>
    public static int ApplyMatchResult(
        Stronghold stronghold,
        MarketCalculator.MatchResult result,
        MerchantTaxLedger taxLedger)
    {
        foreach (var trade in result.Trades)
        {
            var totalMoney = trade.PriceMoneyPerGo * trade.QuantityGo;
            if (!TryGetActor(stronghold, trade.BuyerActorId, out var buyer)
                || !TryGetActor(stronghold, trade.SellerActorId, out var seller))
                continue;

            // 业务：买方资金或卖方库存不足则跳过该笔成交
            if (!trade.BuyerMoneyCommitted && buyer.Money < totalMoney)
                continue;

            if (!trade.SellerInventoryCommitted
                && !MarketInventoryHelper.TryRemoveStock(seller, trade.Commodity, trade.QuantityGo))
            {
                continue;
            }

            if (!trade.BuyerMoneyCommitted)
                buyer.Money -= totalMoney;
            MarketInventoryHelper.AddStock(buyer, trade.Commodity, trade.QuantityGo);
            seller.Money += totalMoney;

            // 业务：非免税卖方的成交按万分比征收交易税并入台账
            if (!trade.SellerTaxExempt)
            {
                var tax = EconomyCalculator.ApplyBasisPointsTax(
                    totalMoney,
                    MarketConstants.TradeTaxBasisPoints);
                if (tax > 0 && seller.Money >= tax)
                {
                    seller.Money -= tax;
                    taxLedger.Accrue(stronghold.Id, seller.Id, tax);
                }
            }
        }

        AppendDailyBar(stronghold, result);

        return result.Trades.Count;
    }

    /// <summary>玩家砸单成交写入当日 K 线与 LastClose（即时刷新行情 UI）。</summary>
    public static void ApplyPlayerTradeToSession(
        Stronghold stronghold,
        GameDate gameDate,
        int tradePriceMoneyPerGo,
        int quantityGo,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        if (tradePriceMoneyPerGo <= 0 || quantityGo <= 0)
            return;

        var market = stronghold.Market;
        var turnover = tradePriceMoneyPerGo * quantityGo;
        market.SetLastClose(commodity, tradePriceMoneyPerGo);

        var bar = EnsureSessionBar(market, gameDate, tradePriceMoneyPerGo, commodity);

        if (bar.Open <= 0)
            bar.Open = tradePriceMoneyPerGo;

        bar.High = Math.Max(bar.High, tradePriceMoneyPerGo);
        bar.Low = bar.Low <= 0 ? tradePriceMoneyPerGo : Math.Min(bar.Low, tradePriceMoneyPerGo);
        bar.Close = tradePriceMoneyPerGo;
        bar.VolumeGo += quantityGo;
        bar.TurnoverMoney += turnover;
    }

    /// <summary>挂单/报价更新会话现价（未成交也刷新盘口中线与 K 线收盘）。</summary>
    public static void MarkSessionQuotePrice(
        Stronghold stronghold,
        GameDate gameDate,
        int quotePriceMoneyPerGo)
    {
        if (quotePriceMoneyPerGo <= 0)
            return;

        var market = stronghold.Market;
        market.LastClosePriceMoneyPerGo = quotePriceMoneyPerGo;

        var bar = EnsureSessionBar(market, gameDate, quotePriceMoneyPerGo);
        if (bar.Open <= 0)
            bar.Open = quotePriceMoneyPerGo;

        bar.High = Math.Max(bar.High, quotePriceMoneyPerGo);
        bar.Low = bar.Low <= 0 ? quotePriceMoneyPerGo : Math.Min(bar.Low, quotePriceMoneyPerGo);
        bar.Close = quotePriceMoneyPerGo;
    }

    private static DailyPriceBar EnsureSessionBar(
        StrongholdMarket market,
        GameDate gameDate,
        int referencePrice,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        var history = market.ResolveMutablePriceHistory(commodity);
        if (history.Count > 0)
        {
            var last = history[^1];
            if (IsSameCalendarDay(last.Date, gameDate))
                return last;
        }

        var open = history.Count > 0
            ? history[^1].Close
            : referencePrice;
        if (open <= 0)
            open = referencePrice;

        var bar = new DailyPriceBar
        {
            Date = gameDate,
            Open = open,
            High = Math.Max(open, referencePrice),
            Low = Math.Min(open, referencePrice),
            Close = referencePrice,
        };
        history.Add(bar);
        TrimPriceHistory(history);
        return bar;
    }

    private static bool IsSameCalendarDay(GameDate a, GameDate b)
        => a.Year == b.Year && a.Month == b.Month && a.Day == b.Day;

    /// <summary>订单完全成交时记录游戏日（用于挂单列表当日展示）。</summary>
    public static void MarkOrderFullyFilled(MarketOrder order, GameDate gameDate)
    {
        if (order.QuantityGo > 0 || order.FilledYear > 0)
            return;

        order.FilledYear = gameDate.Year;
        order.FilledMonth = gameDate.Month;
        order.FilledDay = gameDate.Day;
    }

    /// <summary>已成官府挂单是否仍在当日展示窗口内。</summary>
    public static bool IsFilledOrderVisibleToday(
        Stronghold stronghold,
        MarketOrder order,
        GameDate gameDate)
    {
        if (order.QuantityGo > 0 || order.ActorId != stronghold.ForceActor.Id || order.FilledYear <= 0)
            return false;

        return IsSameCalendarDay(
            new GameDate(order.FilledYear, order.FilledMonth, order.FilledDay),
            gameDate);
    }

    /// <summary>清除零余量挂单；官府已成单仅保留至成交当日结束。</summary>
    public static void RemoveZeroQuantityOrders(Stronghold stronghold, GameDate? gameDate = null)
    {
        stronghold.Market.Orders.RemoveAll(o =>
        {
            if (o.QuantityGo > 0)
                return false;

            if (gameDate is null)
                return true;

            return !IsFilledOrderVisibleToday(stronghold, o, gameDate.Value);
        });
    }

    /// <summary>移除已废弃商品种类的挂单（如旧档 Luxury）。</summary>
    public static void RemoveDeprecatedCommodityOrders(Stronghold stronghold)
        => stronghold.Market.Orders.RemoveAll(o => !Enum.IsDefined(o.Commodity));

    /// <summary>写入日 K 线并滚动删除过期记录。</summary>
    public static void AppendDailyBar(Stronghold stronghold, MarketCalculator.MatchResult result)
    {
        if (result.Trades.Count == 0)
            return;

        foreach (var group in result.Trades.GroupBy(t => t.Commodity))
        {
            var trades = group.ToList();
            var open = 0;
            var high = 0;
            var low = int.MaxValue;
            var close = 0;
            var volume = 0;
            var turnover = 0;

            foreach (var trade in trades)
            {
                if (open == 0)
                    open = trade.PriceMoneyPerGo;

                high = Math.Max(high, trade.PriceMoneyPerGo);
                low = Math.Min(low, trade.PriceMoneyPerGo);
                close = trade.PriceMoneyPerGo;
                volume += trade.QuantityGo;
                turnover += trade.PriceMoneyPerGo * trade.QuantityGo;
            }

            var commodity = group.Key;
            if (volume > 0)
                stronghold.Market.SetLastClose(commodity, close);

            var history = stronghold.Market.ResolveMutablePriceHistory(commodity);
            history.Add(new DailyPriceBar
            {
                Date = default,
                Open = open,
                High = high,
                Low = low == int.MaxValue ? close : low,
                Close = close,
                VolumeGo = volume,
                TurnoverMoney = turnover,
            });

            TrimPriceHistory(history);
        }
    }

    /// <summary>为当日刚写入的 K 线补上游戏日期。</summary>
    public static void SetDailyBarDate(Stronghold stronghold, GameDate date)
    {
        StampLatestBarDate(stronghold.Market.PriceHistory, date);

        foreach (var history in stronghold.Market.PriceHistoryByCommodity.Values)
            StampLatestBarDate(history, date);
    }

    private static void StampLatestBarDate(List<DailyPriceBar> history, GameDate date)
    {
        if (history.Count == 0)
            return;

        history[^1].Date = date;
    }

    /// <summary>移除已有同侧同 Actor 的市民粮买单并挂新单。</summary>
    public static void UpsertCivilianBuyOrder(Stronghold stronghold, int limitPrice, int quantityGo)
    {
        if (quantityGo <= 0 || limitPrice <= 0)
            return;

        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsBuyOrder(o)
            && o.ActorId == stronghold.CivilianActor.Id
            && o.Commodity == MarketCommodityType.Food);

        AddOrder(stronghold, MarketRules.BuySide, stronghold.CivilianActor.Id, limitPrice, quantityGo,
            MarketCommodityType.Food, taxExempt: true);
    }

    /// <summary>官府粮卖单（TaxExempt；同价覆盖 AI 单，保留玩家限价挂单）。</summary>
    public static void UpsertGovernmentSellOrder(Stronghold stronghold, int askPrice, int quantityGo)
    {
        if (quantityGo <= 0 || askPrice <= 0)
            return;

        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsSellOrder(o)
            && o.ActorId == stronghold.ForceActor.Id
            && o.Commodity == MarketCommodityType.Food
            && o.PriceMoneyPerGo == askPrice
            && !MarketRules.IsPlayerRestingOrder(o));

        AddOrder(
            stronghold,
            MarketRules.SellSide,
            stronghold.ForceActor.Id,
            askPrice,
            quantityGo,
            MarketCommodityType.Food,
            taxExempt: true);
    }

    /// <summary>官府粮买单（TaxExempt；同价覆盖 AI 单，保留玩家限价挂单）。</summary>
    public static void UpsertGovernmentBuyOrder(Stronghold stronghold, int limitPrice, int quantityGo)
    {
        if (quantityGo <= 0 || limitPrice <= 0)
            return;

        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsBuyOrder(o)
            && o.ActorId == stronghold.ForceActor.Id
            && o.Commodity == MarketCommodityType.Food
            && o.PriceMoneyPerGo == limitPrice
            && !MarketRules.IsPlayerRestingOrder(o));

        AddLimitOrder(
            stronghold,
            MarketRules.BuySide,
            stronghold.ForceActor.Id,
            limitPrice,
            quantityGo,
            MarketCommodityType.Food,
            taxExempt: true);
    }

    /// <summary>AI 挂单：同 Actor 同侧同价同步至目标量（不触碰玩家限价单）。</summary>
    public static void SyncAiRestingOrder(
        Stronghold stronghold,
        int actorId,
        string side,
        int priceMoneyPerGo,
        int targetQuantityGo,
        MarketCommodityType commodity,
        bool taxExempt)
    {
        if (priceMoneyPerGo <= 0)
            return;

        var isBuy = MarketRules.IsBuySide(side);
        var existing = stronghold.Market.Orders.FirstOrDefault(o =>
            (isBuy ? MarketRules.IsBuyOrder(o) : MarketRules.IsSellOrder(o))
            && o.ActorId == actorId
            && o.Commodity == commodity
            && o.PriceMoneyPerGo == priceMoneyPerGo
            && !MarketRules.IsPlayerRestingOrder(o));

        if (targetQuantityGo <= 0)
        {
            if (existing is not null)
                stronghold.Market.Orders.Remove(existing);
            return;
        }

        if (existing is not null)
        {
            existing.QuantityGo = targetQuantityGo;
            if (existing.OriginalQuantityGo > 0)
                existing.OriginalQuantityGo = Math.Max(existing.OriginalQuantityGo, targetQuantityGo);
            return;
        }

        AddOrder(
            stronghold,
            side,
            actorId,
            priceMoneyPerGo,
            targetQuantityGo,
            commodity,
            taxExempt);
    }

    /// <summary>撤销 Actor AI 挂单中不在保留价位集合内的订单。</summary>
    public static void PruneAiRestingOrders(
        Stronghold stronghold,
        int actorId,
        string side,
        MarketCommodityType commodity,
        IReadOnlySet<int> keepPrices)
    {
        var isBuy = MarketRules.IsBuySide(side);
        stronghold.Market.Orders.RemoveAll(o =>
            (isBuy ? MarketRules.IsBuyOrder(o) : MarketRules.IsSellOrder(o))
            && o.ActorId == actorId
            && o.Commodity == commodity
            && !keepPrices.Contains(o.PriceMoneyPerGo)
            && !MarketRules.IsPlayerRestingOrder(o));
    }

    /// <summary>商户 AI 卖单：同 Actor 同价同步至目标量（不触碰玩家限价单）。</summary>
    public static void SyncMerchantAiSellOrder(
        Stronghold stronghold,
        int merchantActorId,
        int askPrice,
        int targetQuantityGo,
        MarketCommodityType commodity,
        bool taxExempt)
        => SyncAiRestingOrder(
            stronghold,
            merchantActorId,
            MarketRules.SellSide,
            askPrice,
            targetQuantityGo,
            commodity,
            taxExempt);

    /// <summary>撤销指定 Actor 的挂单并退还已锁定资源。</summary>
    public static bool TryCancelOrder(
        Stronghold stronghold,
        int actorId,
        int orderId,
        MarketCommodityType commodity,
        out GameError? error)
    {
        var order = stronghold.Market.Orders.FirstOrDefault(o =>
            o.Id == orderId && o.Commodity == commodity);

        if (order is null)
        {
            error = GameError.MarketError.OrderNotFound;
            return false;
        }

        if (order.ActorId != actorId)
        {
            error = GameError.MarketError.OrderNotOwned;
            return false;
        }

        if (order.QuantityGo <= 0)
        {
            error = GameError.MarketError.OrderNotFound;
            return false;
        }

        if (!TryGetActor(stronghold, actorId, out var actor))
        {
            error = GameError.DataNotFound;
            return false;
        }

        if (MarketRules.IsBuyOrder(order))
        {
            var refundMoney = order.CommittedMoneyGo > 0
                ? order.CommittedMoneyGo
                : order.MoneyCommitted
                    ? order.PriceMoneyPerGo * order.QuantityGo
                    : 0;
            if (refundMoney > 0)
                actor.Money += refundMoney;
        }

        if (MarketRules.IsSellOrder(order))
        {
            var refundGo = order.CommittedInventoryGo > 0
                ? order.CommittedInventoryGo
                : order.InventoryCommitted
                    ? order.QuantityGo
                    : 0;
            if (refundGo > 0)
                MarketInventoryHelper.AddStock(actor, order.Commodity, refundGo);
        }

        stronghold.Market.Orders.Remove(order);
        error = null;
        return true;
    }

    /// <summary>同 Actor 同价合并买单量（用于玩家官府限价）。</summary>
    public static bool AddOrMergeBuyOrder(
        Stronghold stronghold,
        int actorId,
        int limitPrice,
        int quantityGo,
        MarketCommodityType commodity,
        bool taxExempt,
        bool commitMoney = false,
        GameDate? createdDate = null)
    {
        if (quantityGo <= 0 || limitPrice <= 0)
            return false;

        if (commitMoney)
        {
            var totalMoney = limitPrice * quantityGo;
            if (!TryGetActor(stronghold, actorId, out var buyer) || buyer.Money < totalMoney)
                return false;

            buyer.Money -= totalMoney;
        }

        var existing = stronghold.Market.Orders.FirstOrDefault(o =>
            MarketRules.IsBuyOrder(o)
            && o.ActorId == actorId
            && o.Commodity == commodity
            && o.PriceMoneyPerGo == limitPrice);

        if (existing is not null)
        {
            EnsureOriginalQuantity(existing);
            existing.QuantityGo += quantityGo;
            existing.OriginalQuantityGo += quantityGo;
            if (commitMoney)
            {
                existing.MoneyCommitted = true;
                existing.CommittedMoneyGo += limitPrice * quantityGo;
            }

            return true;
        }

        AddLimitOrder(stronghold, MarketRules.BuySide, actorId, limitPrice, quantityGo, commodity, taxExempt, createdDate);
        if (commitMoney)
        {
            var added = stronghold.Market.Orders[^1];
            added.MoneyCommitted = true;
            added.CommittedMoneyGo = limitPrice * quantityGo;
        }

        return true;
    }

    /// <summary>同 Actor 同价合并卖单量（用于玩家官府限价）。</summary>
    public static bool AddOrMergeSellOrder(
        Stronghold stronghold,
        int actorId,
        int askPrice,
        int quantityGo,
        MarketCommodityType commodity,
        bool taxExempt,
        bool commitInventory = false,
        GameDate? createdDate = null)
    {
        if (quantityGo <= 0 || askPrice <= 0)
            return false;

        if (commitInventory)
        {
            if (!TryGetActor(stronghold, actorId, out var seller)
                || !MarketInventoryHelper.TryRemoveStock(seller, commodity, quantityGo))
            {
                return false;
            }
        }

        var existing = stronghold.Market.Orders.FirstOrDefault(o =>
            MarketRules.IsSellOrder(o)
            && o.ActorId == actorId
            && o.Commodity == commodity
            && o.PriceMoneyPerGo == askPrice);

        if (existing is not null)
        {
            EnsureOriginalQuantity(existing);
            existing.QuantityGo += quantityGo;
            existing.OriginalQuantityGo += quantityGo;
            if (commitInventory)
            {
                existing.InventoryCommitted = true;
                existing.CommittedInventoryGo += quantityGo;
            }

            return true;
        }

        AddLimitOrder(stronghold, MarketRules.SellSide, actorId, askPrice, quantityGo, commodity, taxExempt, createdDate);
        if (commitInventory)
        {
            var added = stronghold.Market.Orders[^1];
            added.InventoryCommitted = true;
            added.CommittedInventoryGo = quantityGo;
        }

        return true;
    }

    /// <summary>追加限价单（不合并同 Actor 既有挂单；用于演示 seed）。</summary>
    public static void AddLimitOrder(
        Stronghold stronghold,
        string side,
        int actorId,
        int price,
        int quantityGo,
        MarketCommodityType commodity,
        bool taxExempt,
        GameDate? createdDate = null)
    {
        if (quantityGo <= 0 || price <= 0)
            return;

        AddOrder(stronghold, side, actorId, price, quantityGo, commodity, taxExempt, createdDate);
    }

    /// <summary>通用卖单 upsert（M4-d）。</summary>
    public static void UpsertSellOrder(
        Stronghold stronghold,
        int actorId,
        int askPrice,
        int quantityGo,
        MarketCommodityType commodity,
        bool taxExempt)
    {
        if (quantityGo <= 0 || askPrice <= 0)
            return;

        stronghold.Market.Orders.RemoveAll(o =>
            MarketRules.IsSellOrder(o)
            && o.ActorId == actorId
            && o.Commodity == commodity);

        AddOrder(stronghold, MarketRules.SellSide, actorId, askPrice, quantityGo, commodity, taxExempt);
    }

    private static void AddOrder(
        Stronghold stronghold,
        string side,
        int actorId,
        int price,
        int quantityGo,
        MarketCommodityType commodity,
        bool taxExempt,
        GameDate? createdDate = null)
    {
        var nextId = stronghold.Market.Orders.Count == 0
            ? 1
            : stronghold.Market.Orders.Max(o => o.Id) + 1;

        stronghold.Market.Orders.Add(new MarketOrder
        {
            Id = nextId,
            Side = side,
            ActorId = actorId,
            PriceMoneyPerGo = price,
            QuantityGo = quantityGo,
            OriginalQuantityGo = quantityGo,
            CreatedYear = createdDate?.Year ?? 0,
            CreatedMonth = createdDate?.Month ?? 0,
            CreatedDay = createdDate?.Day ?? 0,
            TaxExempt = taxExempt,
            Commodity = commodity
        });
    }

    private static void EnsureOriginalQuantity(MarketOrder order)
    {
        if (order.OriginalQuantityGo <= 0)
            order.OriginalQuantityGo = order.QuantityGo;
    }

    public static bool TryGetActorPublic(Stronghold stronghold, int actorId, out StrongholdActor actor)
        => TryGetActor(stronghold, actorId, out actor);

    private static bool TryGetActor(Stronghold stronghold, int actorId, out StrongholdActor actor)
    {
        if (stronghold.ForceActor.Id == actorId)
        {
            actor = stronghold.ForceActor;
            return true;
        }

        if (stronghold.CivilianActor.Id == actorId)
        {
            actor = stronghold.CivilianActor;
            return true;
        }

        foreach (var merchant in stronghold.MerchantActors)
        {
            if (merchant.Id == actorId)
            {
                actor = merchant;
                return true;
            }
        }

        foreach (var religion in stronghold.ReligionActors)
        {
            if (religion.Id == actorId)
            {
                actor = religion;
                return true;
            }
        }

        actor = null!;
        return false;
    }

    private static void TrimPriceHistory(List<DailyPriceBar> history)
    {
        while (history.Count > EconomyConstants.MarketPriceHistoryRetentionDays)
            history.RemoveAt(0);
    }

    private static void TrimPriceHistory(StrongholdMarket market)
        => TrimPriceHistory(market.PriceHistory);
}
