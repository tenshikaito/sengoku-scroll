using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>可贸易据点演示数据：约 1 年日 K + 10 档买卖挂单。</summary>
public static class MarketBootstrapHelper
{
    public const int DemoHistoryDays = 365;

    public const int DemoDepthLevels = 20;

    public static void EnsureDemoMarketData(GameData gameData)
    {
        var anchorDate = gameData.GameDate;

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (!MarketRules.CanTrade(stronghold))
                continue;

            if (stronghold.Market.PriceHistory.Count < DemoHistoryDays)
            {
                stronghold.Market.PriceHistory.Clear();
                SeedPriceHistory(stronghold, anchorDate);
            }

            SeedDemoOrders(stronghold, anchorDate);
            SeedDemoHorseData(stronghold, anchorDate);
        }
    }

    private static void EnsureSellerFood(Stronghold stronghold, int actorId, int sellQuantityGo)
    {
        if (!MarketActions.TryGetActorPublic(stronghold, actorId, out var seller))
            return;

        var reserve = seller.Id == stronghold.ForceActor.Id
            ? MarketConstants.GovernmentFoodReserveGo
            : MarketConstants.MerchantFoodReserveGo;
        seller.Food = Math.Max(seller.Food, sellQuantityGo + reserve);
    }

    private static void PlaceDemoSellOrder(
        Stronghold stronghold,
        int actorId,
        int price,
        int quantityGo,
        bool taxExempt)
    {
        EnsureSellerFood(stronghold, actorId, quantityGo);
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            actorId,
            price,
            quantityGo,
            MarketCommodityType.Food,
            taxExempt);
    }

    private static void SeedPriceHistory(Stronghold stronghold, GameDate anchorDate)
    {
        var market = stronghold.Market;
        var basePrice = MarketCalculator.CalculateGovernmentSellPrice(stronghold);
        if (basePrice <= 0)
            basePrice = MarketConstants.DefaultPriceMoneyPerGo;

        var rng = new Random(stronghold.Id * 7919 + basePrice);
        var close = basePrice;
        var endDate = anchorDate.AddDays(-1);
        var startDate = endDate.AddDays(-(DemoHistoryDays - 1));

        for (var day = 0; day < DemoHistoryDays; day++)
        {
            var date = startDate.AddDays(day);
            var open = close;
            close = Math.Max(10, close + rng.Next(-3, 4));
            var high = Math.Max(open, close) + rng.Next(0, 3);
            var low = Math.Max(5, Math.Min(open, close) - rng.Next(0, 3));
            var volume = rng.Next(20, 800) * LogisticsConstants.GoPerKoku;
            var turnover = volume * (open + close) / 2;

            market.PriceHistory.Add(new DailyPriceBar
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                VolumeGo = volume,
                TurnoverMoney = turnover,
            });
        }

        market.LastClosePriceMoneyPerGo = close;
    }

    private static void SeedDemoOrders(Stronghold stronghold, GameDate gameDate)
    {
        var market = stronghold.Market;
        var lastClose = market.LastClosePriceMoneyPerGo > 0
            ? market.LastClosePriceMoneyPerGo
            : MarketConstants.DefaultPriceMoneyPerGo;

        market.Orders.RemoveAll(o => o.Commodity == MarketCommodityType.Food);

        var merchants = stronghold.MerchantActors.ToList();

        // 中线 = lastClose（K 线收盘）；买卖各在两侧 1~N 档，不在中线同价挂单。
        for (var level = 1; level <= DemoDepthLevels; level++)
        {
            var askPrice = lastClose + level;
            var askQty = (80 + (stronghold.Id * 37 + level * 113) % 420) * LogisticsConstants.GoPerKoku;
            var askActor = merchants.Count > 0
                ? merchants[(level - 1) % merchants.Count].Id
                : stronghold.ForceActor.Id;
            var askTaxExempt = askActor == stronghold.ForceActor.Id;

            PlaceDemoSellOrder(stronghold, askActor, askPrice, askQty, askTaxExempt);

            var bidPrice = Math.Max(1, lastClose - level);
            var bidQty = (90 + (stronghold.Id * 53 + level * 97) % 510) * LogisticsConstants.GoPerKoku;
            var bidActor = level % 3 == 0 ? stronghold.ForceActor.Id : stronghold.CivilianActor.Id;
            var bidTaxExempt = bidActor == stronghold.ForceActor.Id || bidActor == stronghold.CivilianActor.Id;

            MarketActions.AddLimitOrder(
                stronghold,
                MarketRules.BuySide,
                bidActor,
                bidPrice,
                bidQty,
                MarketCommodityType.Food,
                bidTaxExempt);

            if (bidActor == stronghold.CivilianActor.Id)
            {
                stronghold.CivilianActor.Money = Math.Max(
                    stronghold.CivilianActor.Money,
                    bidQty * bidPrice);
            }
        }

        SeedPlayerTestOrderAtQuote(stronghold, gameDate, quotePrice: 95);
    }

    private static void SeedDemoHorseData(Stronghold stronghold, GameDate anchorDate)
    {
        SeedHorsePriceHistory(stronghold, anchorDate);
        SeedDemoHorseOrders(stronghold, anchorDate);
    }

    private static void EnsureSellerHorse(Stronghold stronghold, int actorId, int sellQuantity)
    {
        if (!MarketActions.TryGetActorPublic(stronghold, actorId, out var seller))
            return;

        var reserve = seller.Id == stronghold.ForceActor.Id
            ? 0
            : MarketConstants.MerchantHorseReserve;
        seller.Horse = Math.Max(seller.Horse, sellQuantity + reserve);
    }

    private static void SeedHorsePriceHistory(Stronghold stronghold, GameDate anchorDate)
    {
        var market = stronghold.Market;
        var horseHistory = market.ResolveMutablePriceHistory(MarketCommodityType.Horse);
        if (horseHistory.Count >= DemoHistoryDays)
            return;

        horseHistory.Clear();
        var basePrice = CommodityInventoryHelper.ResolveDefaultPrice(null, MarketCommodityType.Horse);
        var rng = new Random(stronghold.Id * 8831 + basePrice);
        var close = basePrice;
        var endDate = anchorDate.AddDays(-1);
        var startDate = endDate.AddDays(-(DemoHistoryDays - 1));

        for (var day = 0; day < DemoHistoryDays; day++)
        {
            var date = startDate.AddDays(day);
            var open = close;
            close = Math.Max(20, close + rng.Next(-5, 6));
            var high = Math.Max(open, close) + rng.Next(0, 4);
            var low = Math.Max(10, Math.Min(open, close) - rng.Next(0, 4));
            var volume = rng.Next(2, 40);
            var turnover = volume * (open + close) / 2;

            horseHistory.Add(new DailyPriceBar
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                VolumeGo = volume,
                TurnoverMoney = turnover,
            });
        }

        market.SetLastClose(MarketCommodityType.Horse, close);
    }

    private static void SeedDemoHorseOrders(Stronghold stronghold, GameDate gameDate)
    {
        var market = stronghold.Market;
        var lastClose = market.ResolveLastClose(MarketCommodityType.Horse);
        if (lastClose <= 0)
            lastClose = CommodityInventoryHelper.ResolveDefaultPrice(null, MarketCommodityType.Horse);

        market.Orders.RemoveAll(o => o.Commodity == MarketCommodityType.Horse);

        stronghold.ForceActor.Horse = Math.Max(stronghold.ForceActor.Horse, 120);
        foreach (var merchant in stronghold.MerchantActors)
            merchant.Horse = Math.Max(merchant.Horse, 24);

        var merchants = stronghold.MerchantActors.ToList();
        var depthLevels = Math.Min(DemoDepthLevels, 12);

        for (var level = 1; level <= depthLevels; level++)
        {
            var askPrice = lastClose + level;
            var askQty = 4 + (stronghold.Id * 11 + level * 17) % 18;
            var askActor = merchants.Count > 0
                ? merchants[(level - 1) % merchants.Count].Id
                : stronghold.ForceActor.Id;
            var askTaxExempt = askActor == stronghold.ForceActor.Id;

            EnsureSellerHorse(stronghold, askActor, askQty);
            MarketActions.AddLimitOrder(
                stronghold,
                MarketRules.SellSide,
                askActor,
                askPrice,
                askQty,
                MarketCommodityType.Horse,
                askTaxExempt);

            var bidPrice = Math.Max(1, lastClose - level);
            var bidQty = 3 + (stronghold.Id * 13 + level * 19) % 16;
            var bidActor = level % 2 == 0 ? stronghold.ForceActor.Id : stronghold.CivilianActor.Id;
            var bidTaxExempt = bidActor == stronghold.ForceActor.Id || bidActor == stronghold.CivilianActor.Id;

            MarketActions.AddLimitOrder(
                stronghold,
                MarketRules.BuySide,
                bidActor,
                bidPrice,
                bidQty,
                MarketCommodityType.Horse,
                bidTaxExempt);

            if (bidActor == stronghold.CivilianActor.Id)
            {
                stronghold.CivilianActor.Money = Math.Max(
                    stronghold.CivilianActor.Money,
                    bidQty * bidPrice);
            }
        }
    }

    /// <summary>官府测试挂单：固定价位 95，仅占一侧（不与买卖同价并存）。</summary>
    private static void SeedPlayerTestOrderAtQuote(
        Stronghold stronghold,
        GameDate gameDate,
        int quotePrice)
    {
        if (quotePrice <= 0)
            return;

        var qty = (48 + stronghold.Id * 7) * LogisticsConstants.GoPerKoku;
        EnsureSellerFood(stronghold, stronghold.ForceActor.Id, qty);

        stronghold.Market.Orders.RemoveAll(o =>
            o.Commodity == MarketCommodityType.Food && o.PriceMoneyPerGo == quotePrice);

        MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            quotePrice,
            qty,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true,
            createdDate: gameDate);
    }
}
