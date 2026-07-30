using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class MarketCancelOrderTests
{
    [Fact]
    public void CancelBuyOrder_RefundsCommittedMoney()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData);
        var actor = stronghold.ForceActor;
        var qty = 100 * LogisticsConstants.GoPerKoku;

        stronghold.Market.Orders.Clear();
        MarketTestOrderSeedHelper.EnsureBuyFunds(stronghold, actor.Id, 80, qty);
        var moneyBefore = actor.Money;
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.BuySide,
            actor.Id,
            80,
            qty,
            MarketCommodityType.Food,
            taxExempt: true,
            gameData.GameDate);
        var order = Assert.Single(stronghold.Market.Orders);
        Assert.True(order.MoneyCommitted);

        Assert.True(MarketActions.TryCancelOrder(
            stronghold,
            actor.Id,
            order.Id,
            MarketCommodityType.Food,
            out var error));
        Assert.Null(error);
        Assert.Empty(stronghold.Market.Orders);
        Assert.Equal(moneyBefore, actor.Money);
    }

    [Fact]
    public void CancelSellOrder_RefundsCommittedInventory()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData);
        var actor = stronghold.ForceActor;
        var qty = 50 * LogisticsConstants.GoPerKoku;

        stronghold.Market.Orders.Clear();
        MarketTestOrderSeedHelper.EnsureSellStock(stronghold, actor.Id, qty);
        var foodBefore = actor.Food;
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            actor.Id,
            90,
            qty,
            MarketCommodityType.Food,
            taxExempt: true,
            gameData.GameDate);
        var order = Assert.Single(stronghold.Market.Orders);
        Assert.True(order.InventoryCommitted);

        Assert.True(MarketActions.TryCancelOrder(
            stronghold,
            actor.Id,
            order.Id,
            MarketCommodityType.Food,
            out var error));
        Assert.Null(error);
        Assert.Empty(stronghold.Market.Orders);
        Assert.Equal(foodBefore, actor.Food);
    }

    [Fact]
    public void Snapshot_IncludesPlayerOpenOrders()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData);
        var actor = stronghold.ForceActor;

        stronghold.Market.Orders.Clear();
        MarketTestOrderSeedHelper.EnsureBuyFunds(stronghold, actor.Id, 75, 200 * LogisticsConstants.GoPerKoku);
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.BuySide,
            actor.Id,
            75,
            200 * LogisticsConstants.GoPerKoku,
            MarketCommodityType.Food,
            taxExempt: true,
            gameData.GameDate);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            playerForceId: stronghold.ForceId);

        Assert.Single(snapshot.PlayerOpenOrders);
    }

    private static Stronghold CreateStronghold(GameData gameData)
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        gameData.Strongholds[stronghold.Id] = stronghold;
        return stronghold;
    }
}
