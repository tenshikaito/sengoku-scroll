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

public class MarketCancelOrderTests
{
    [Fact]
    public void CancelBuyOrder_RefundsCommittedMoney()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData);
        var actor = stronghold.ForceActor;
        var moneyBefore = actor.Money;

        stronghold.Market.Orders.Clear();
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.BuySide,
            actor.Id,
            80,
            100 * LogisticsConstants.GoPerKoku,
            MarketCommodityType.Food,
            taxExempt: true,
            gameData.GameDate);
        var order = Assert.Single(stronghold.Market.Orders);
        order.MoneyCommitted = true;
        order.CommittedMoneyGo = order.PriceMoneyPerGo * order.QuantityGo;
        actor.Money -= order.CommittedMoneyGo;

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
        var foodBefore = actor.Food;
        var qty = 50 * LogisticsConstants.GoPerKoku;

        stronghold.Market.Orders.Clear();
        actor.Food -= qty;
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
        order.InventoryCommitted = true;
        order.CommittedInventoryGo = qty;

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

        var open = Assert.Single(snapshot.PlayerOpenOrders);
        Assert.Equal(MarketRules.BuySide, open.Side);
        Assert.Equal(75, open.PriceMoneyPerGo);
        Assert.Equal("Open", open.FillStatus);
        Assert.Equal(1560, open.CreatedYear);
    }

    private static Stronghold CreateStronghold(GameData gameData)
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Common.Types.Point3(2, 3));
        stronghold.Name = "测试城";
        stronghold.Market.LastClosePriceMoneyPerGo = 90;
        gameData.Strongholds[stronghold.Id] = stronghold;
        return stronghold;
    }
}
