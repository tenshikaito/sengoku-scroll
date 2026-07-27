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

public class MarketFilledOrderRetentionTests
{
    [Fact]
    public void FilledPlayerOrder_VisibleOnSameDay_HiddenAfterDayChange()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData);
        var actor = stronghold.ForceActor;
        var qty = 100 * LogisticsConstants.GoPerKoku;

        stronghold.Market.Orders.Clear();
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
        order.MoneyCommitted = true;
        order.CommittedMoneyGo = order.PriceMoneyPerGo * qty;
        order.QuantityGo = 0;
        MarketActions.MarkOrderFullyFilled(order, gameData.GameDate);

        var sameDaySnapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            playerForceId: stronghold.ForceId);
        var visible = Assert.Single(sameDaySnapshot.PlayerOpenOrders);
        Assert.Equal("Filled", visible.FillStatus);
        Assert.Equal(0, visible.QuantityGo);

        gameData.GameDate = gameData.GameDate.AddDays(1);
        MarketActions.RemoveZeroQuantityOrders(stronghold, gameData.GameDate);

        var nextDaySnapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            playerForceId: stronghold.ForceId);
        Assert.Empty(nextDaySnapshot.PlayerOpenOrders);
    }

    [Fact]
    public void OpenPlayerOrder_RemainsVisibleAcrossDays()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = CreateStronghold(gameData);
        var actor = stronghold.ForceActor;

        stronghold.Market.Orders.Clear();
        MarketActions.AddLimitOrder(
            stronghold,
            MarketRules.SellSide,
            actor.Id,
            95,
            50 * LogisticsConstants.GoPerKoku,
            MarketCommodityType.Food,
            taxExempt: true,
            gameData.GameDate);
        var order = Assert.Single(stronghold.Market.Orders);
        order.InventoryCommitted = true;
        order.CommittedInventoryGo = order.QuantityGo;

        gameData.GameDate = gameData.GameDate.AddDays(1);
        MarketActions.RemoveZeroQuantityOrders(stronghold, gameData.GameDate);

        var snapshot = MarketSnapshotHelper.BuildSnapshot(
            stronghold,
            gameData,
            MarketCommodityType.Food,
            playerForceId: stronghold.ForceId);

        Assert.Single(snapshot.PlayerOpenOrders);
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
