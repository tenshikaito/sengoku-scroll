using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>挂单锁资/退资：买卖挂单统一在簿时锁定资金或库存。</summary>
public static class MarketOrderCommitHelper
{
    /// <summary>撤单或删单前退还尚未成交的锁定资源。</summary>
    public static void RefundCommitment(Stronghold stronghold, MarketOrder order)
    {
        if (!MarketActions.TryGetActorPublic(stronghold, order.ActorId, out var actor))
            return;

        if (MarketRules.IsBuyOrder(order))
        {
            var refundMoney = order.CommittedMoneyGo > 0
                ? order.CommittedMoneyGo
                : order.MoneyCommitted
                    ? order.PriceMoneyPerGo * order.QuantityGo
                    : 0;
            if (refundMoney > 0)
                actor.Money += refundMoney;

            order.CommittedMoneyGo = 0;
            order.MoneyCommitted = false;
            return;
        }

        var refundGo = order.CommittedInventoryGo > 0
            ? order.CommittedInventoryGo
            : order.InventoryCommitted
                ? order.QuantityGo
                : 0;
        if (refundGo > 0)
            MarketInventoryHelper.AddStock(actor, order.Commodity, refundGo);

        order.CommittedInventoryGo = 0;
        order.InventoryCommitted = false;
    }

    /// <summary>新建买单并锁定资金；资金不足时按可用资金缩量。</summary>
    public static int ResolveAffordableBuyQuantity(
        Stronghold stronghold,
        int actorId,
        int priceMoneyPerGo,
        int desiredQuantityGo)
    {
        if (desiredQuantityGo <= 0 || priceMoneyPerGo <= 0)
            return 0;

        if (!MarketActions.TryGetActorPublic(stronghold, actorId, out var buyer))
            return 0;

        return Math.Min(desiredQuantityGo, buyer.Money / priceMoneyPerGo);
    }

    /// <summary>新建卖单并锁定库存；库存不足时按可用库存缩量。</summary>
    public static int ResolveAffordableSellQuantity(
        Stronghold stronghold,
        int actorId,
        MarketCommodityType commodity,
        int desiredQuantityGo)
    {
        if (desiredQuantityGo <= 0)
            return 0;

        if (!MarketActions.TryGetActorPublic(stronghold, actorId, out var seller))
            return 0;

        return Math.Min(desiredQuantityGo, MarketInventoryHelper.GetStock(seller, commodity));
    }

    public static bool TryCommitBuyQuantity(
        Stronghold stronghold,
        MarketOrder order,
        int quantityGo)
    {
        if (quantityGo <= 0 || order.PriceMoneyPerGo <= 0)
            return false;

        if (!MarketActions.TryGetActorPublic(stronghold, order.ActorId, out var buyer))
            return false;

        var totalMoney = order.PriceMoneyPerGo * quantityGo;
        if (buyer.Money < totalMoney)
            return false;

        buyer.Money -= totalMoney;
        order.MoneyCommitted = true;
        order.CommittedMoneyGo += totalMoney;
        order.QuantityGo = quantityGo;
        if (order.OriginalQuantityGo <= 0)
            order.OriginalQuantityGo = quantityGo;
        else
            order.OriginalQuantityGo = Math.Max(order.OriginalQuantityGo, quantityGo);

        return true;
    }

    public static bool TryCommitSellQuantity(
        Stronghold stronghold,
        MarketOrder order,
        int quantityGo)
    {
        if (quantityGo <= 0)
            return false;

        if (!MarketActions.TryGetActorPublic(stronghold, order.ActorId, out var seller))
            return false;

        if (!MarketInventoryHelper.TryRemoveStock(seller, order.Commodity, quantityGo))
            return false;

        order.InventoryCommitted = true;
        order.CommittedInventoryGo += quantityGo;
        order.QuantityGo = quantityGo;
        if (order.OriginalQuantityGo <= 0)
            order.OriginalQuantityGo = quantityGo;
        else
            order.OriginalQuantityGo = Math.Max(order.OriginalQuantityGo, quantityGo);

        return true;
    }

    /// <summary>AI/演示同步：将既有买单量调整至目标；返回实际同步量（可能因资金不足缩量）。</summary>
    public static int SyncBuyQuantity(
        Stronghold stronghold,
        MarketOrder order,
        int targetQuantityGo)
    {
        if (targetQuantityGo <= 0)
            return 0;

        if (!MarketActions.TryGetActorPublic(stronghold, order.ActorId, out var buyer))
            return 0;

        var currentQty = order.QuantityGo;
        if (targetQuantityGo == currentQty)
            return currentQty;

        if (targetQuantityGo < currentQty)
        {
            var releaseQty = currentQty - targetQuantityGo;
            var releaseMoney = order.PriceMoneyPerGo * releaseQty;
            if (order.MoneyCommitted && releaseMoney > 0)
            {
                buyer.Money += releaseMoney;
                order.CommittedMoneyGo = Math.Max(0, order.CommittedMoneyGo - releaseMoney);
            }

            order.QuantityGo = targetQuantityGo;
            order.OriginalQuantityGo = Math.Max(order.OriginalQuantityGo, targetQuantityGo);
            return targetQuantityGo;
        }

        var addQty = targetQuantityGo - currentQty;
        var addMoney = order.PriceMoneyPerGo * addQty;
        if (buyer.Money < addMoney)
            addQty = buyer.Money / order.PriceMoneyPerGo;

        if (addQty <= 0)
            return currentQty;

        addMoney = order.PriceMoneyPerGo * addQty;
        buyer.Money -= addMoney;
        order.MoneyCommitted = true;
        order.CommittedMoneyGo += addMoney;
        order.QuantityGo = currentQty + addQty;
        order.OriginalQuantityGo = Math.Max(order.OriginalQuantityGo, order.QuantityGo);
        return order.QuantityGo;
    }

    /// <summary>AI/演示同步：将既有卖单量调整至目标；返回实际同步量（可能因库存不足缩量）。</summary>
    public static int SyncSellQuantity(
        Stronghold stronghold,
        MarketOrder order,
        int targetQuantityGo)
    {
        if (targetQuantityGo <= 0)
            return 0;

        if (!MarketActions.TryGetActorPublic(stronghold, order.ActorId, out var seller))
            return 0;

        var currentQty = order.QuantityGo;
        if (targetQuantityGo == currentQty)
            return currentQty;

        if (targetQuantityGo < currentQty)
        {
            var releaseQty = currentQty - targetQuantityGo;
            if (order.InventoryCommitted && releaseQty > 0)
            {
                MarketInventoryHelper.AddStock(seller, order.Commodity, releaseQty);
                order.CommittedInventoryGo = Math.Max(0, order.CommittedInventoryGo - releaseQty);
            }

            order.QuantityGo = targetQuantityGo;
            order.OriginalQuantityGo = Math.Max(order.OriginalQuantityGo, targetQuantityGo);
            return targetQuantityGo;
        }

        var addQty = targetQuantityGo - currentQty;
        if (!MarketInventoryHelper.TryRemoveStock(seller, order.Commodity, addQty))
            addQty = MarketInventoryHelper.GetStock(seller, order.Commodity);

        if (addQty <= 0)
            return currentQty;

        order.InventoryCommitted = true;
        order.CommittedInventoryGo += addQty;
        order.QuantityGo = currentQty + addQty;
        order.OriginalQuantityGo = Math.Max(order.OriginalQuantityGo, order.QuantityGo);
        return order.QuantityGo;
    }

    /// <summary>砸单/即时成交：可成交买量（已锁资优先）。</summary>
    public static int ResolveBuyFillQuantity(
        Stronghold stronghold,
        MarketOrder buy,
        int tradePricePerGo)
    {
        if (buy.QuantityGo <= 0 || tradePricePerGo <= 0)
            return 0;

        if (buy.MoneyCommitted)
        {
            var byCommit = buy.CommittedMoneyGo / tradePricePerGo;
            return Math.Min(buy.QuantityGo, byCommit);
        }

        if (!MarketActions.TryGetActorPublic(stronghold, buy.ActorId, out var buyer))
            return 0;

        return Math.Min(buy.QuantityGo, buyer.Money / tradePricePerGo);
    }

    /// <summary>砸单：可成交卖量（已锁库存优先）。</summary>
    public static int ResolveSellFillQuantity(MarketOrder sell)
    {
        if (sell.QuantityGo <= 0)
            return 0;

        if (sell.InventoryCommitted)
            return Math.Min(sell.QuantityGo, sell.CommittedInventoryGo);

        return sell.QuantityGo;
    }

    /// <summary>砸单成交后扣减买单锁定量；未锁资时由调用方扣款。</summary>
    public static void ApplyBuyFill(MarketOrder buy, int tradePricePerGo, int quantityGo)
    {
        if (quantityGo <= 0)
            return;

        buy.QuantityGo = Math.Max(0, buy.QuantityGo - quantityGo);
        if (buy.MoneyCommitted)
            buy.CommittedMoneyGo = Math.Max(0, buy.CommittedMoneyGo - tradePricePerGo * quantityGo);
    }

    /// <summary>砸单成交后扣减卖单锁定量；未锁库存时由调用方扣粮。</summary>
    public static void ApplySellFill(MarketOrder sell, int quantityGo)
    {
        if (quantityGo <= 0)
            return;

        sell.QuantityGo = Math.Max(0, sell.QuantityGo - quantityGo);
        if (sell.InventoryCommitted)
            sell.CommittedInventoryGo = Math.Max(0, sell.CommittedInventoryGo - quantityGo);
    }

    public static void RefundAndRemoveOrders(
        Stronghold stronghold,
        IEnumerable<MarketOrder> orders)
    {
        foreach (var order in orders.ToList())
        {
            RefundCommitment(stronghold, order);
            stronghold.Market.Orders.Remove(order);
        }
    }
}
