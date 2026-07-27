using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>
/// 限价单统一流水线：先与对手簿撮合，剩余量可选挂簿。
/// 「买价低于卖一只挂单、否则扫货」由撮合循环自然得出，无需分段分支。
/// </summary>
public static class MarketLimitOrderExecutor
{
    public readonly record struct LimitOrderExecutionResult(int FilledQuantityGo, int RestingQuantityGo)
    {
        public bool HasTradeEffect => FilledQuantityGo > 0 || RestingQuantityGo > 0;
    }

    public sealed class LimitBuyRequest
    {
        public required Stronghold Stronghold { get; init; }

        public required GameData GameData { get; init; }

        public required MerchantTaxLedger TaxLedger { get; init; }

        public required int BuyerActorId { get; init; }

        public required int LimitPriceMoneyPerGo { get; init; }

        /// <summary>0 表示尽可能多买（受资金约束）。</summary>
        public required int QuantityGo { get; init; }

        public required MarketCommodityType Commodity { get; init; }

        public required Func<int> GetBuyerMoney { get; init; }

        public required Action<int> DeductBuyerMoney { get; init; }

        public required Action<int> AddBuyerStock { get; init; }

        public Func<int>? GetBuyerStock { get; init; }

        public bool AllowRestingOrder { get; init; } = true;

        public bool CommitMoneyOnRest { get; init; }

        public bool TaxExemptOnRest { get; init; }
    }

    public sealed class LimitSellRequest
    {
        public required Stronghold Stronghold { get; init; }

        public required GameData GameData { get; init; }

        public required MerchantTaxLedger TaxLedger { get; init; }

        public required int SellerActorId { get; init; }

        public required int LimitPriceMoneyPerGo { get; init; }

        /// <summary>0 表示尽可能多卖（受库存约束）。</summary>
        public required int QuantityGo { get; init; }

        public required MarketCommodityType Commodity { get; init; }

        public required Func<int> GetSellerStock { get; init; }

        public required Action<int> DeductSellerStock { get; init; }

        public required Action<int> AddSellerMoney { get; init; }

        public bool AllowRestingOrder { get; init; } = true;

        public bool CommitInventoryOnRest { get; init; }

        public bool TaxExemptOnRest { get; init; }
    }

    public static LimitOrderExecutionResult ExecuteLimitBuy(LimitBuyRequest request)
    {
        if (request.LimitPriceMoneyPerGo <= 0)
            return default;

        var stronghold = request.Stronghold;
        var remaining = request.QuantityGo > 0 ? request.QuantityGo : int.MaxValue / 1000;
        var totalFilled = 0;

        var sells = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.Commodity == request.Commodity)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ThenBy(o => o.Id)
            .ToList();

        foreach (var sell in sells)
        {
            if (remaining <= 0 || request.GetBuyerMoney() <= 0)
                break;

            if (sell.PriceMoneyPerGo > request.LimitPriceMoneyPerGo)
                break;

            if (!MarketActions.TryGetActorPublic(stronghold, sell.ActorId, out var seller))
            {
                sell.QuantityGo = 0;
                MarketActions.MarkOrderFullyFilled(sell, request.GameData.GameDate);
                continue;
            }

            var affordableQty = request.GetBuyerMoney() / sell.PriceMoneyPerGo;
            var qty = Math.Min(Math.Min(remaining, sell.QuantityGo), affordableQty);
            if (qty <= 0)
            {
                sell.QuantityGo = 0;
                MarketActions.MarkOrderFullyFilled(sell, request.GameData.GameDate);
                continue;
            }

            var totalMoney = sell.PriceMoneyPerGo * qty;
            if (!MarketInventoryHelper.TryRemoveStock(seller, request.Commodity, qty))
            {
                sell.QuantityGo = 0;
                MarketActions.MarkOrderFullyFilled(sell, request.GameData.GameDate);
                continue;
            }

            request.DeductBuyerMoney(totalMoney);
            request.AddBuyerStock(qty);
            seller.Money += totalMoney;

            if (!sell.TaxExempt)
            {
                var tax = EconomyCalculator.ApplyBasisPointsTax(
                    totalMoney,
                    MarketConstants.TradeTaxBasisPoints);
                if (tax > 0 && seller.Money >= tax)
                {
                    seller.Money -= tax;
                    request.TaxLedger.Accrue(stronghold.Id, seller.Id, tax);
                }
            }

            totalFilled += qty;
            remaining -= qty;
            // 砸单扫卖盘：该价位一旦被触及，整档清空（含卖家库存不足的剩余量）。
            sell.QuantityGo = 0;
            MarketActions.MarkOrderFullyFilled(sell, request.GameData.GameDate);
            MarketActions.ApplyPlayerTradeToSession(
                stronghold,
                request.GameData.GameDate,
                sell.PriceMoneyPerGo,
                qty,
                request.Commodity);
        }

        MarketActions.RemoveZeroQuantityOrders(stronghold, request.GameData.GameDate);

        var resting = 0;
        if (request.AllowRestingOrder && remaining > 0)
        {
            var affordableQty = request.GetBuyerMoney() / request.LimitPriceMoneyPerGo;
            var restingQty = Math.Min(remaining, affordableQty);
            if (restingQty > 0
                && MarketActions.AddOrMergeBuyOrder(
                    stronghold,
                    request.BuyerActorId,
                    request.LimitPriceMoneyPerGo,
                    restingQty,
                    request.Commodity,
                    request.TaxExemptOnRest,
                    commitMoney: request.CommitMoneyOnRest,
                    createdDate: request.GameData.GameDate))
            {
                resting = restingQty;
            }
        }

        return new LimitOrderExecutionResult(totalFilled, resting);
    }

    public static LimitOrderExecutionResult ExecuteLimitSell(LimitSellRequest request)
    {
        if (request.LimitPriceMoneyPerGo <= 0)
            return default;

        var stronghold = request.Stronghold;
        var remaining = request.QuantityGo > 0
            ? Math.Min(request.QuantityGo, request.GetSellerStock())
            : request.GetSellerStock();
        var totalFilled = 0;

        var buys = stronghold.Market.Orders
            .Where(o => MarketRules.IsBuyOrder(o) && o.Commodity == request.Commodity)
            .OrderByDescending(o => o.PriceMoneyPerGo)
            .ThenBy(o => o.Id)
            .ToList();

        foreach (var buy in buys)
        {
            if (remaining <= 0)
                break;

            if (buy.PriceMoneyPerGo < request.LimitPriceMoneyPerGo)
                break;

            if (!MarketActions.TryGetActorPublic(stronghold, buy.ActorId, out var buyer))
            {
                buy.QuantityGo = 0;
                MarketActions.MarkOrderFullyFilled(buy, request.GameData.GameDate);
                continue;
            }

            var affordableQty = buyer.Money / buy.PriceMoneyPerGo;
            var qty = Math.Min(Math.Min(remaining, buy.QuantityGo), affordableQty);
            if (qty <= 0)
            {
                buy.QuantityGo = 0;
                MarketActions.MarkOrderFullyFilled(buy, request.GameData.GameDate);
                continue;
            }

            var totalMoney = buy.PriceMoneyPerGo * qty;
            request.DeductSellerStock(qty);
            buyer.Money -= totalMoney;
            MarketInventoryHelper.AddStock(buyer, request.Commodity, qty);
            request.AddSellerMoney(totalMoney);

            totalFilled += qty;
            remaining -= qty;
            // 砸单扫买盘：该价位一旦被触及，整档清空（含买家资金不足的剩余量）。
            buy.QuantityGo = 0;
            MarketActions.MarkOrderFullyFilled(buy, request.GameData.GameDate);
            MarketActions.ApplyPlayerTradeToSession(
                stronghold,
                request.GameData.GameDate,
                buy.PriceMoneyPerGo,
                qty,
                request.Commodity);
        }

        MarketActions.RemoveZeroQuantityOrders(stronghold, request.GameData.GameDate);

        var resting = 0;
        if (request.AllowRestingOrder && remaining > 0)
        {
            remaining = Math.Min(remaining, request.GetSellerStock());
            if (remaining > 0
                && MarketActions.AddOrMergeSellOrder(
                    stronghold,
                    request.SellerActorId,
                    request.LimitPriceMoneyPerGo,
                    remaining,
                    request.Commodity,
                    request.TaxExemptOnRest,
                    commitInventory: request.CommitInventoryOnRest,
                    createdDate: request.GameData.GameDate))
            {
                resting = remaining;
            }
        }

        return new LimitOrderExecutionResult(totalFilled, resting);
    }
}
