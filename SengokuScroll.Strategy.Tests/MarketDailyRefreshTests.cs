using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class MarketDailyRefreshTests
{
    [Fact]
    public void PlayerTrade_DoesNotInvokeAiOrderRefresh()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Point3(2, 3));
        gameData.Strongholds[stronghold.Id] = stronghold;
        stronghold.Market.LastClosePriceMoneyPerGo = 90;
        stronghold.ForceActor.Money = 50_000_000;
        stronghold.ForceActor.Food = 50_000_000;

        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = 901,
            Name = "商铺",
            Type = ActorType.Merchant,
            ForceId = 2,
            StrongholdId = stronghold.Id,
            CharacterIds = [],
            SubUnitIds = [],
            Money = 50_000_000,
            Food = 5_000_000,
        });

        MarketActions.SyncAiRestingOrder(
            stronghold,
            901,
            MarketRules.BuySide,
            88,
            100 * LogisticsConstants.GoPerKoku,
            MarketCommodityType.Food,
            taxExempt: false);
        var merchantOrdersBefore = stronghold.Market.Orders.Count(o => o.ActorId == 901);

        var bidQty = 100 * LogisticsConstants.GoPerKoku;
        MarketTestOrderSeedHelper.PlaceBuy(stronghold, stronghold.CivilianActor.Id, 95, bidQty);

        _ = MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = new MerchantTaxLedger(),
            SellerActorId = stronghold.ForceActor.Id,
            LimitPriceMoneyPerGo = 95,
            QuantityGo = bidQty,
            Commodity = MarketCommodityType.Food,
            GetSellerStock = () => stronghold.ForceActor.Food,
            DeductSellerStock = qty => stronghold.ForceActor.Food -= qty,
            AddSellerMoney = amount => stronghold.ForceActor.Money += amount,
            AllowRestingOrder = false,
            CommitInventoryOnRest = true,
            TaxExemptOnRest = true,
        });

        Assert.Equal(merchantOrdersBefore, stronghold.Market.Orders.Count(o => o.ActorId == 901));
    }

    [Fact]
    public void AppendDailyBar_MergesMatchVolumeIntoPlayerTradeBarSameDay()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));

        MarketActions.ApplyPlayerTradeToSession(
            stronghold,
            gameData.GameDate,
            tradePriceMoneyPerGo: 90,
            quantityGo: 100,
            MarketCommodityType.Food);

        Assert.Single(stronghold.Market.PriceHistory);

        var match = new MarketCalculator.MatchResult(
            [
                new MarketCalculator.TradeExecution(
                    BuyOrderId: 1,
                    SellOrderId: 2,
                    PriceMoneyPerGo: 92,
                    QuantityGo: 50,
                    BuyerActorId: 1,
                    SellerActorId: 2,
                    SellerTaxExempt: true,
                    SellerInventoryCommitted: true,
                    BuyerMoneyCommitted: true,
                    MarketCommodityType.Food),
            ],
            SessionOpen: 92,
            SessionHigh: 92,
            SessionLow: 92,
            SessionClose: 92,
            TotalVolumeGo: 50);

        MarketActions.AppendDailyBar(stronghold, match, gameData.GameDate);

        var bar = Assert.Single(stronghold.Market.PriceHistory);
        Assert.Equal(150, bar.VolumeGo);
        Assert.Equal(92, bar.Close);
    }

    [Fact]
    public void AdvanceDay_PreservesPlayerRestingSellOrderNotCrossingBook()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var world = host.GetState().Value!;
        var qingzhou = world.Strongholds.First(sh =>
            string.Equals(sh.Name, "清洲", StringComparison.Ordinal)
            || string.Equals(sh.Name, "清州城", StringComparison.Ordinal));
        var strongholdId = qingzhou.Id;

        var sellQtyGo = 500 * LogisticsConstants.GoPerKoku;
        var limitPrice = 200;
        Assert.True(host.StrongholdLordSmashSellFood(strongholdId, limitPrice, sellQtyGo).IsSuccess);

        var snapshotBefore = host.GetMarketSnapshot(strongholdId).Value!;
        var playerOrderBefore = Assert.Single(
            snapshotBefore.PlayerOpenOrders.Where(o => o.PriceMoneyPerGo == limitPrice));
        Assert.Equal(sellQtyGo, playerOrderBefore.QuantityGo);

        Assert.True(host.AdvanceDay().IsSuccess);

        var snapshotAfter = host.GetMarketSnapshot(strongholdId).Value!;
        var playerOrderAfter = Assert.Single(
            snapshotAfter.PlayerOpenOrders.Where(o => o.PriceMoneyPerGo == limitPrice));
        Assert.Equal(sellQtyGo, playerOrderAfter.QuantityGo);
    }

    [Fact]
    public void AdvanceDay_KeepsNonCrossingRestingSellAtSamePriceAsGovernmentAiAsk()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var qingzhouId = host.GetState().Value!.Strongholds
            .First(sh => sh.Name is "清洲" or "清州城").Id;
        var snapshot = host.GetMarketSnapshot(qingzhouId).Value!;
        var govAskPrice = snapshot.AskLevels.Max(l => l.PriceMoneyPerGo);
        var sellQtyGo = 120 * LogisticsConstants.GoPerKoku;

        Assert.True(host.StrongholdLordSmashSellFood(qingzhouId, govAskPrice, sellQtyGo).IsSuccess);

        var before = host.GetMarketSnapshot(qingzhouId).Value!;
        Assert.Contains(before.PlayerOpenOrders, o => o.PriceMoneyPerGo == govAskPrice && o.QuantityGo == sellQtyGo);

        Assert.True(host.AdvanceDay().IsSuccess);

        var after = host.GetMarketSnapshot(qingzhouId).Value!;
        Assert.Contains(after.PlayerOpenOrders, o => o.PriceMoneyPerGo == govAskPrice && o.QuantityGo == sellQtyGo);
    }

    [Fact]
    public void AdvanceDay_PreservesPlayerSellMergedIntoSameActorExistingAsk()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var gameData = world.GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        gameData.Strongholds[stronghold.Id] = stronghold;
        stronghold.Market.LastClosePriceMoneyPerGo = 90;
        stronghold.ForceActor.Money = 50_000_000;
        stronghold.ForceActor.Food = 50_000_000;

        const int askPrice = 100;
        var aiQty = 200 * LogisticsConstants.GoPerKoku;
        MarketActions.SyncAiRestingOrder(
            stronghold,
            stronghold.ForceActor.Id,
            MarketRules.SellSide,
            askPrice,
            aiQty,
            MarketCommodityType.Food,
            taxExempt: true);

        var playerQty = 300 * LogisticsConstants.GoPerKoku;
        var result = MarketLimitOrderExecutor.ExecuteLimitSell(new MarketLimitOrderExecutor.LimitSellRequest
        {
            Stronghold = stronghold,
            GameData = gameData,
            TaxLedger = new MerchantTaxLedger(),
            SellerActorId = stronghold.ForceActor.Id,
            LimitPriceMoneyPerGo = askPrice,
            QuantityGo = playerQty,
            Commodity = MarketCommodityType.Food,
            GetSellerStock = () => stronghold.ForceActor.Food,
            DeductSellerStock = qty => stronghold.ForceActor.Food -= qty,
            AddSellerMoney = amount => stronghold.ForceActor.Money += amount,
            AllowRestingOrder = true,
            CommitInventoryOnRest = true,
            TaxExemptOnRest = true,
        });
        Assert.Equal(playerQty, result.RestingQuantityGo);

        var merged = Assert.Single(
            stronghold.Market.Orders.Where(o =>
                MarketRules.IsSellOrder(o)
                && o.ActorId == stronghold.ForceActor.Id
                && o.PriceMoneyPerGo == askPrice
                && o.QuantityGo > 0));
        Assert.Equal(aiQty + playerQty, merged.QuantityGo);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        gameData.GameDate = gameData.GameDate.AddDays(1);
        ctx.Services.GetRequiredService<IStrategyMarketSystem>().Update();

        var after = stronghold.Market.Orders
            .Where(o =>
                MarketRules.IsSellOrder(o)
                && o.ActorId == stronghold.ForceActor.Id
                && o.PriceMoneyPerGo == askPrice)
            .ToList();
        Assert.Contains(after, o => o.QuantityGo == aiQty + playerQty);
    }

    [Fact]
    public void AdvanceDay_QingzhouBookDepthRemainsAfterOneDay()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario("mini_kanto").IsSuccess);

        var qingzhouId = host.GetState().Value!.Strongholds
            .First(sh => sh.Name is "清洲" or "清州城").Id;

        Assert.True(host.AdvanceDay().IsSuccess);

        var after = host.GetMarketSnapshot(qingzhouId).Value!;
        var askNonZero = after.AskLevels.Count(l => l.PriceMoneyPerGo > 0 && l.QuantityGo > 0);
        var bidNonZero = after.BidLevels.Count(l => l.PriceMoneyPerGo > 0 && l.QuantityGo > 0);

        Assert.True(askNonZero >= 3, $"asks={askNonZero} session={after.SessionPriceMoneyPerGo}");
        Assert.True(bidNonZero >= 3, $"bids={bidNonZero}");
    }
}
