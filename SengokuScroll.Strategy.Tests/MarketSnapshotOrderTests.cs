using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>市场盘口展示顺序（后端判定，与 UI 自上而下一致）。</summary>
public class MarketSnapshotOrderTests
{
    private const int DepthCount = 5;

    [Fact]
    public void BuildSnapshot_Qingzhou_SellAt9_DisplaysExpectedDepthOrder()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateQingzhouStronghold(gameData);
        SeedLadderAroundSession(stronghold, sessionPrice: 9, depth: DepthCount);

        var sellQtyGo = 20_000 * LogisticsConstants.GoPerKoku;
        stronghold.ForceActor.Food = sellQtyGo + MarketConstants.GovernmentFoodReserveGo;
        Assert.True(MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice: 9,
            quantityGo: sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true));

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal("清州城", snapshot.StrongholdName);
        Assert.Equal("Both", snapshot.BookQuoteSide);
        Assert.Equal(8, snapshot.BestBidPriceMoneyPerGo);
        Assert.Equal(9, snapshot.BestAskPriceMoneyPerGo);
        Assert.Equal(9, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(sellQtyGo, snapshot.CloseLevelQuantityGo);

        Assert.Equal([13, 12, 11, 10, 9], ActivePrices(snapshot.AskLevels));
        Assert.Equal([8, 7, 6, 5, 4], ActivePrices(snapshot.BidLevels));
    }

    [Fact]
    public void BuildSnapshot_Qingzhou_SellAt9_ThenOverBudgetBuyAboveSession_DisplaysExpectedDepthOrder()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateQingzhouStronghold(gameData);
        SeedLadderAroundSession(stronghold, sessionPrice: 9, depth: DepthCount);

        var sellQtyGo = 20_000 * LogisticsConstants.GoPerKoku;
        stronghold.ForceActor.Food = sellQtyGo + MarketConstants.GovernmentFoodReserveGo;
        Assert.True(MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice: 9,
            quantityGo: sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true));

        stronghold.ForceActor.Money = 5_000;
        var overQtyGo = 9_000 * LogisticsConstants.GoPerKoku;
        var affordableGo = stronghold.ForceActor.Money / 10;
        Assert.True(affordableGo < overQtyGo);
        Assert.True(MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            limitPrice: 10,
            quantityGo: affordableGo,
            MarketCommodityType.Food,
            taxExempt: true,
            commitMoney: true));

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal(9, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal("Both", snapshot.BookQuoteSide);
        Assert.Equal(sellQtyGo, snapshot.CloseLevelQuantityGo);
        Assert.Equal([13, 12, 11, 10, 9], ActivePrices(snapshot.AskLevels));
        Assert.Equal([10, 8, 7, 6, 5], ActivePrices(snapshot.BidLevels));

        var buyAtTen = Assert.Single(snapshot.BidLevels, level => level.PriceMoneyPerGo == 10);
        Assert.Equal(affordableGo, buyAtTen.QuantityGo);
        Assert.Equal(0, stronghold.ForceActor.Money);
    }

    [Fact]
    public void BuildSnapshot_Qingzhou_SellAt9_WhenSessionHigher_ShowsNineAsLowestAsk()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateQingzhouStronghold(gameData);
        const int sessionPrice = 86;
        stronghold.Market.LastClosePriceMoneyPerGo = sessionPrice;
        stronghold.Market.PriceHistory[^1] = new DailyPriceBar
        {
            Date = gameData.GameDate,
            Open = sessionPrice,
            High = sessionPrice,
            Low = sessionPrice,
            Close = sessionPrice,
            VolumeGo = 100 * LogisticsConstants.GoPerKoku,
            TurnoverMoney = sessionPrice * 100 * LogisticsConstants.GoPerKoku,
        };

        for (var level = 1; level <= DepthCount; level++)
        {
            MarketTestOrderSeedHelper.PlaceSell(
                stronghold,
                stronghold.CivilianActor.Id,
                100 + level,
                (200 + level) * LogisticsConstants.GoPerKoku);
        }

        var sellQtyGo = 20_000 * LogisticsConstants.GoPerKoku;
        stronghold.ForceActor.Food = sellQtyGo + MarketConstants.GovernmentFoodReserveGo;
        Assert.True(MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice: 9,
            quantityGo: sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true));

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal(sessionPrice, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(sessionPrice, snapshot.LastClosePriceMoneyPerGo);
        Assert.Equal("Ask", snapshot.BookQuoteSide);
        Assert.Equal(0, snapshot.BestBidPriceMoneyPerGo);
        Assert.Equal(9, snapshot.BestAskPriceMoneyPerGo);
        Assert.Equal(0, snapshot.CloseLevelQuantityGo);
        Assert.Equal([104, 103, 102, 101, 9], ActivePrices(snapshot.AskLevels));
        Assert.Equal(9, snapshot.AskLevels.Last(level => level.PriceMoneyPerGo > 0).PriceMoneyPerGo);
        Assert.Equal(sellQtyGo, snapshot.AskLevels.Last(level => level.PriceMoneyPerGo > 0).QuantityGo);
        Assert.Empty(ActivePrices(snapshot.BidLevels));
    }

    [Fact]
    public void RestingSellBelowSession_KeepsSessionPrice_AndShowsNineAsLowestAsk()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateQingzhouStronghold(gameData);
        const int sessionPrice = 86;
        stronghold.Market.LastClosePriceMoneyPerGo = sessionPrice;
        stronghold.Market.PriceHistory[^1] = new DailyPriceBar
        {
            Date = gameData.GameDate,
            Open = sessionPrice,
            High = sessionPrice,
            Low = sessionPrice,
            Close = sessionPrice,
            VolumeGo = 100 * LogisticsConstants.GoPerKoku,
            TurnoverMoney = sessionPrice * 100 * LogisticsConstants.GoPerKoku,
        };

        for (var level = 1; level <= DepthCount; level++)
        {
            MarketTestOrderSeedHelper.PlaceSell(
                stronghold,
                stronghold.CivilianActor.Id,
                100 + level,
                (200 + level) * LogisticsConstants.GoPerKoku);
        }

        var sellQtyGo = 20_000 * LogisticsConstants.GoPerKoku;
        stronghold.ForceActor.Food = sellQtyGo + MarketConstants.GovernmentFoodReserveGo;
        Assert.True(MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice: 9,
            quantityGo: sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true));

        // 关键：纯挂卖不得改写会话现价（旧逻辑 MarkSessionQuotePrice 会把中线压到挂单价）
        Assert.Equal(sessionPrice, stronghold.Market.LastClosePriceMoneyPerGo);
        Assert.Equal(sessionPrice, stronghold.Market.PriceHistory[^1].Close);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: DepthCount);

        Assert.Equal(sessionPrice, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(sessionPrice, snapshot.LastClosePriceMoneyPerGo);
        Assert.Equal("Ask", snapshot.BookQuoteSide);
        Assert.Equal(0, snapshot.CloseLevelQuantityGo);
        Assert.Equal([104, 103, 102, 101, 9], ActivePrices(snapshot.AskLevels));
        Assert.Equal(sellQtyGo, snapshot.AskLevels.Single(l => l.PriceMoneyPerGo == 9).QuantityGo);
    }

    [Fact]
    public void BuildSnapshot_TenDepthLevels_UiFiveAsksIncludeNearestSellOne()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateQingzhouStronghold(gameData);
        const int sessionPrice = 98;
        stronghold.Market.LastClosePriceMoneyPerGo = sessionPrice;
        stronghold.Market.PriceHistory[^1] = new DailyPriceBar
        {
            Date = gameData.GameDate,
            Open = sessionPrice,
            High = sessionPrice,
            Low = sessionPrice,
            Close = sessionPrice,
            VolumeGo = 100 * LogisticsConstants.GoPerKoku,
            TurnoverMoney = sessionPrice * 100 * LogisticsConstants.GoPerKoku,
        };

        for (var level = 1; level <= MarketBootstrapHelper.DemoDepthLevels; level++)
        {
            MarketTestOrderSeedHelper.PlaceSell(
                stronghold,
                stronghold.CivilianActor.Id,
                sessionPrice + level,
                (200 + level) * LogisticsConstants.GoPerKoku);
        }

        var sellQtyGo = 20_000 * LogisticsConstants.GoPerKoku;
        stronghold.ForceActor.Food = sellQtyGo + MarketConstants.GovernmentFoodReserveGo;
        Assert.True(MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice: 9,
            quantityGo: sellQtyGo,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true));

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: MarketBootstrapHelper.DemoDepthLevels);

        Assert.Equal(MarketBootstrapHelper.DemoDepthLevels, snapshot.AskLevels.Count);
        Assert.Equal([102, 101, 100, 99, 9], UiAskPrices(snapshot.AskLevels, uiDepth: DepthCount));
        Assert.Equal(9, UiAskPrices(snapshot.AskLevels, DepthCount)[^1]);
    }

    private static Stronghold CreateQingzhouStronghold(GameData gameData)
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(
            id: 7,
            forceId: 1,
            location: new Point3(2, 3));
        stronghold.Name = "清州城";
        stronghold.Market.LastClosePriceMoneyPerGo = 9;
        stronghold.Market.PriceHistory.Add(new DailyPriceBar
        {
            Date = gameData.GameDate,
            Open = 9,
            High = 9,
            Low = 9,
            Close = 9,
            VolumeGo = 100 * LogisticsConstants.GoPerKoku,
            TurnoverMoney = 900 * LogisticsConstants.GoPerKoku,
        });
        gameData.Strongholds[stronghold.Id] = stronghold;
        return stronghold;
    }

    private static void SeedLadderAroundSession(Stronghold stronghold, int sessionPrice, int depth)
    {
        stronghold.Market.Orders.Clear();

        for (var level = 1; level <= depth; level++)
        {
            var askPrice = sessionPrice + level;
            var askQty = (100 + level) * LogisticsConstants.GoPerKoku;
            MarketTestOrderSeedHelper.PlaceSell(
                stronghold,
                stronghold.CivilianActor.Id,
                askPrice,
                askQty);

            var bidPrice = sessionPrice - level;
            var bidQty = (110 + level) * LogisticsConstants.GoPerKoku;
            MarketTestOrderSeedHelper.PlaceBuy(
                stronghold,
                stronghold.CivilianActor.Id,
                bidPrice,
                bidQty);
        }
    }

    private static int[] ActivePrices(IEnumerable<Models.StrategyMarketDepthLevelDto> levels)
        => levels
            .Where(level => level.PriceMoneyPerGo > 0)
            .Select(level => level.PriceMoneyPerGo)
            .ToArray();

    private static int[] UiAskPrices(
        IReadOnlyList<Models.StrategyMarketDepthLevelDto> levels,
        int uiDepth)
        => MarketSnapshotDiagnostics.UiVisibleAskLevels(levels, uiDepth)
            .Select(level => level.PriceMoneyPerGo)
            .ToArray();
}
