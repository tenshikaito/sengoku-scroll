using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>限价单统一撮合流水线（场景 A/B/C）。</summary>
public class MarketLimitOrderExecutorTests
{
    private const int DepthCount = 5;

    [Fact]
    public void ScenarioA_LastTradeAt95WithNoRestingAt95_ShowsEmptyMidQtyAndSpreadDepth()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData, lastTradePrice: 95);

        SeedBidLadder(stronghold, fromPrice: 94, depth: DepthCount);
        SeedAskLadder(stronghold, fromPrice: 96, depth: DepthCount);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal(95, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(0, snapshot.CloseLevelQuantityGo);
        Assert.Equal([100, 99, 98, 97, 96], ActivePrices(snapshot.AskLevels));
        Assert.Equal([94, 93, 92, 91, 90], ActivePrices(snapshot.BidLevels));
    }

    [Fact]
    public void ScenarioB_LimitBuy100_SweepsTo96AndRestsAt100()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData, lastTradePrice: 90);
        var actor = stronghold.ForceActor;
        actor.Money = 20_000_000;
        stronghold.CivilianActor.Food = 2_000_000;

        var sellQtyGo = 50 * LogisticsConstants.GoPerKoku;
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            stronghold.CivilianActor.Id,
            96,
            sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true);
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            stronghold.CivilianActor.Id,
            98,
            sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true);

        var fillQtyGo = sellQtyGo * 2;
        var restingQty = 50 * LogisticsConstants.GoPerKoku;
        var result = MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = new MerchantTaxLedger(),
            BuyerActorId = actor.Id,
            LimitPriceMoneyPerGo = 100,
            QuantityGo = fillQtyGo + restingQty,
            Commodity = MarketCommodityType.Food,
            GetBuyerMoney = () => actor.Money,
            DeductBuyerMoney = amount => actor.Money -= amount,
            AddBuyerStock = qty => actor.Food += qty,
            AllowRestingOrder = true,
            CommitMoneyOnRest = true,
            TaxExemptOnRest = true,
        });

        Assert.Equal(fillQtyGo, result.FilledQuantityGo);
        Assert.Equal(restingQty, result.RestingQuantityGo);
        Assert.Equal(98, stronghold.Market.LastClosePriceMoneyPerGo);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal(98, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(restingQty, snapshot.BidLevels.Single(l => l.PriceMoneyPerGo == 100).QuantityGo);
        Assert.Contains(100, ActivePrices(snapshot.BidLevels));
    }

    [Fact]
    public void ScenarioC_LimitBuy90BelowAsk96_DoesNotTradeAndRestsBid()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        const int lastTrade = 95;
        var stronghold = CreateStronghold(gameData, lastTradePrice: lastTrade);
        var actor = stronghold.ForceActor;
        var restingQty = 120 * LogisticsConstants.GoPerKoku;
        actor.Money = restingQty * 90 + 10_000;

        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            stronghold.CivilianActor.Id,
            96,
            200 * LogisticsConstants.GoPerKoku,
            MarketCommodityType.Food,
            taxExempt: true);

        var result = MarketLimitOrderExecutor.ExecuteLimitBuy(new MarketLimitOrderExecutor.LimitBuyRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = new MerchantTaxLedger(),
            BuyerActorId = actor.Id,
            LimitPriceMoneyPerGo = 90,
            QuantityGo = restingQty,
            Commodity = MarketCommodityType.Food,
            GetBuyerMoney = () => actor.Money,
            DeductBuyerMoney = amount => actor.Money -= amount,
            AddBuyerStock = qty => actor.Food += qty,
            AllowRestingOrder = true,
            CommitMoneyOnRest = true,
            TaxExemptOnRest = true,
        });

        Assert.Equal(0, result.FilledQuantityGo);
        Assert.Equal(restingQty, result.RestingQuantityGo);
        Assert.Equal(lastTrade, stronghold.Market.LastClosePriceMoneyPerGo);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal(lastTrade, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(90, snapshot.BestBidPriceMoneyPerGo);
        Assert.Equal(96, snapshot.BestAskPriceMoneyPerGo);
        Assert.Contains(90, ActivePrices(snapshot.BidLevels));
    }

    private static Stronghold CreateStronghold(GameData gameData, int lastTradePrice)
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Common.Types.Point3(2, 3));
        stronghold.Name = "清州城";
        stronghold.Market.LastClosePriceMoneyPerGo = lastTradePrice;
        stronghold.Market.PriceHistory.Add(new DailyPriceBar
        {
            Date = gameData.GameDate,
            Open = lastTradePrice,
            High = lastTradePrice,
            Low = lastTradePrice,
            Close = lastTradePrice,
            VolumeGo = 100 * LogisticsConstants.GoPerKoku,
            TurnoverMoney = lastTradePrice * 100 * LogisticsConstants.GoPerKoku,
        });
        gameData.Strongholds[stronghold.Id] = stronghold;
        return stronghold;
    }

    private static void SeedBidLadder(Stronghold stronghold, int fromPrice, int depth)
    {
        for (var i = 0; i < depth; i++)
        {
            var price = fromPrice - i;
            MarketActions.AddLimitOrder(
                stronghold,
                MarketRules.BuySide,
                stronghold.CivilianActor.Id,
                price,
                (100 + i) * LogisticsConstants.GoPerKoku,
                MarketCommodityType.Food,
                taxExempt: true);
        }
    }

    private static void SeedAskLadder(Stronghold stronghold, int fromPrice, int depth)
    {
        for (var i = 0; i < depth; i++)
        {
            var price = fromPrice + i;
            MarketActions.AddLimitOrder(
                stronghold,
                MarketRules.SellSide,
                stronghold.CivilianActor.Id,
                price,
                (100 + i) * LogisticsConstants.GoPerKoku,
                MarketCommodityType.Food,
                taxExempt: true);
        }
    }

    private static int[] ActivePrices(IEnumerable<Models.StrategyMarketDepthLevelDto> levels)
        => levels
            .Where(level => level.PriceMoneyPerGo > 0)
            .Select(level => level.PriceMoneyPerGo)
            .ToArray();
}
