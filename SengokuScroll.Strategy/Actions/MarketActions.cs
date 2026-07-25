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
            if (buyer.Money < totalMoney)
                continue;

            if (!MarketInventoryHelper.TryRemoveStock(seller, trade.Commodity, trade.QuantityGo))
                continue;

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

    /// <summary>写入日 K 线并滚动删除过期记录。</summary>
    public static void AppendDailyBar(Stronghold stronghold, MarketCalculator.MatchResult result)
    {
        var market = stronghold.Market;

        if (result.TotalVolumeGo > 0)
            market.LastClosePriceMoneyPerGo = result.SessionClose;

        market.PriceHistory.Add(new DailyPriceBar
        {
            Date = default,
            Open = result.SessionOpen,
            High = result.SessionHigh,
            Low = result.SessionLow,
            Close = result.SessionClose
        });

        TrimPriceHistory(market);
    }

    /// <summary>为当日刚写入的 K 线补上游戏日期。</summary>
    public static void SetDailyBarDate(Stronghold stronghold, GameDate date)
    {
        if (stronghold.Market.PriceHistory.Count == 0)
            return;

        stronghold.Market.PriceHistory[^1].Date = date;
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

    /// <summary>官府粮卖单（TaxExempt）。</summary>
    public static void UpsertGovernmentSellOrder(Stronghold stronghold, int askPrice, int quantityGo)
        => UpsertSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice,
            quantityGo,
            MarketCommodityType.Food,
            taxExempt: true);

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
        bool taxExempt)
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
            TaxExempt = taxExempt,
            Commodity = commodity
        });
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

        actor = null!;
        return false;
    }

    private static void TrimPriceHistory(StrongholdMarket market)
    {
        while (market.PriceHistory.Count > EconomyConstants.MarketPriceHistoryRetentionDays)
            market.PriceHistory.RemoveAt(0);
    }
}
