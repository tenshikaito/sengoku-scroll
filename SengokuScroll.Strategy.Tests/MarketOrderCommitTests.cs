using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
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

/// <summary>统一挂单锁资：交叉盘口应在撮合后消除。</summary>
public class MarketOrderCommitTests
{
    [Fact]
    public void MatchOrders_CrossedCommittedBook_ClearsSpread()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Point3(2, 3));
        gameData.Strongholds[stronghold.Id] = stronghold;
        stronghold.Market.LastClosePriceMoneyPerGo = 92;
        stronghold.MerchantActors.Add(new Domain.Entities.StrongholdActor
        {
            Id = 901,
            Name = "商铺",
            Type = ActorType.Merchant,
            ForceId = 1,
            StrongholdId = stronghold.Id,
            CharacterIds = [],
            SubUnitIds = [],
            Money = 50_000_000,
            Food = 0,
        });

        var sellQty = 709 * LogisticsConstants.GoPerKoku;
        var buyQty = 200 * LogisticsConstants.GoPerKoku;
        MarketTestOrderSeedHelper.PlaceSell(stronghold, stronghold.CivilianActor.Id, 90, sellQty);
        MarketTestOrderSeedHelper.PlaceBuy(stronghold, stronghold.MerchantActors[0].Id, 92, buyQty);

        Assert.True(
            MarketDepthDisplayHelper.ResolveBookQuote(stronghold.Market, MarketCommodityType.Food, 92).BestBidPriceMoneyPerGo
            >= MarketDepthDisplayHelper.ResolveBookQuote(stronghold.Market, MarketCommodityType.Food, 92).BestAskPriceMoneyPerGo);

        var ledger = new MerchantTaxLedger();
        var result = MarketCalculator.MatchOrders(stronghold, 92, gameData.GameDate);
        var settled = MarketActions.ApplyMatchResult(stronghold, result, ledger, gameData.GameDate);

        Assert.True(settled > 0);
        var quote = MarketDepthDisplayHelper.ResolveBookQuote(stronghold.Market, MarketCommodityType.Food, 92);
        Assert.True(
            quote.BestBidPriceMoneyPerGo <= 0
            || quote.BestAskPriceMoneyPerGo <= 0
            || quote.BestBidPriceMoneyPerGo < quote.BestAskPriceMoneyPerGo);
    }

    [Fact]
    public void SyncAiRestingOrder_BuyOrder_LocksMoney()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        var merchant = new Domain.Entities.StrongholdActor
        {
            Id = 901,
            Name = "商铺",
            Type = ActorType.Merchant,
            ForceId = 1,
            StrongholdId = stronghold.Id,
            CharacterIds = [],
            SubUnitIds = [],
            Money = 10_000,
            Food = 0,
        };
        stronghold.MerchantActors.Add(merchant);

        var qty = 50 * LogisticsConstants.GoPerKoku;
        merchant.Money = 90 * qty + 1_000;
        MarketActions.SyncAiRestingOrder(
            stronghold,
            merchant.Id,
            MarketRules.BuySide,
            90,
            qty,
            MarketCommodityType.Food,
            taxExempt: false);

        var order = Assert.Single(stronghold.Market.Orders);
        Assert.True(order.MoneyCommitted);
        Assert.Equal(90 * qty, order.CommittedMoneyGo);
        Assert.Equal(1_000, merchant.Money);
    }
}
