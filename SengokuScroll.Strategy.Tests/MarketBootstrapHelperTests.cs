using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>演示市场种子数据一致性。</summary>
public class MarketBootstrapHelperTests
{
    [Fact]
    public void SeedDemoOrders_SpreadAroundLastClose_NoOrderAtQuotePrice()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Common.Types.Point3(2, 3));
        stronghold.Name = "清州城";
        stronghold.CommerceValue = 5000;
        stronghold.EconomyFacilityIds = [EconomyFacilityConstants.MarketFacilityTypeId];
        gameData.Strongholds[stronghold.Id] = stronghold;

        MarketBootstrapHelper.EnsureDemoMarketData(gameData);

        var lastClose = stronghold.Market.LastClosePriceMoneyPerGo;
        Assert.True(lastClose > 0);
        Assert.DoesNotContain(
            stronghold.Market.Orders,
            o => o.Commodity == MarketCommodityType.Food && o.PriceMoneyPerGo == lastClose);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: MarketBootstrapHelper.DemoDepthLevels);

        Assert.Equal("Both", snapshot.BookQuoteSide);
        Assert.Equal(lastClose, snapshot.SessionPriceMoneyPerGo);
        Assert.Equal(0, snapshot.CloseLevelQuantityGo);
        Assert.Equal(lastClose + 1, snapshot.BestAskPriceMoneyPerGo);
        Assert.Equal(lastClose - 1, snapshot.BestBidPriceMoneyPerGo);

        var askPrices = snapshot.AskLevels
            .Where(level => level.PriceMoneyPerGo > 0)
            .Select(level => level.PriceMoneyPerGo)
            .ToHashSet();
        var bidPrices = snapshot.BidLevels
            .Where(level => level.PriceMoneyPerGo > 0)
            .Select(level => level.PriceMoneyPerGo)
            .ToHashSet();
        Assert.Empty(askPrices.Intersect(bidPrices));
    }

    [Fact]
    public void SeedDemoOrders_AddsPlayerTestSellOrderAt95_WithCreatedDate()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 15);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Common.Types.Point3(2, 3));
        stronghold.Name = "清州城";
        stronghold.CommerceValue = 5000;
        stronghold.EconomyFacilityIds = [EconomyFacilityConstants.MarketFacilityTypeId];
        gameData.Strongholds[stronghold.Id] = stronghold;

        MarketBootstrapHelper.EnsureDemoMarketData(gameData);

        var at95 = stronghold.Market.Orders
            .Where(o => o.Commodity == MarketCommodityType.Food && o.PriceMoneyPerGo == 95)
            .ToList();
        Assert.Single(at95);
        Assert.True(MarketRules.IsSellOrder(at95[0]));
        Assert.Equal(stronghold.ForceActor.Id, at95[0].ActorId);
        Assert.Equal(gameData.GameDate.Year, at95[0].CreatedYear);
        Assert.Equal(gameData.GameDate.Month, at95[0].CreatedMonth);
        Assert.Equal(gameData.GameDate.Day, at95[0].CreatedDay);
        Assert.True(at95[0].InventoryCommitted);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            depthLevels: MarketBootstrapHelper.DemoDepthLevels,
            playerForceId: 1);

        var open = Assert.Single(snapshot.PlayerOpenOrders, o => o.PriceMoneyPerGo == 95);
        Assert.Equal("Sell", open.Side);
        Assert.Equal("1560/6/15", $"{open.CreatedYear}/{open.CreatedMonth}/{open.CreatedDay}");
    }
}
