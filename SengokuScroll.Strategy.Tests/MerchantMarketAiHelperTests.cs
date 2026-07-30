using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class MerchantMarketAiHelperTests
{
    [Fact]
    public void EvaluateAndPlaceOrders_WithoutExternalBids_UndercutsBelowReference()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 90;

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var sells = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.Commodity == MarketCommodityType.Food)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ToList();
        var buys = stronghold.Market.Orders
            .Where(o => MarketRules.IsBuyOrder(o) && o.ActorId == 901)
            .ToList();

        Assert.InRange(sells.Count, MarketConstants.MarketMinDepthLevels, MarketConstants.MarketMaxDepthLevels);
        // 业务：无外部买盘时低价抛售（至少低于中枢捡漏折扣），并撤销本店买盘防自成交
        Assert.True(sells[0].PriceMoneyPerGo <= 85);
        Assert.Empty(buys);
        Assert.All(sells, o => Assert.False(o.TaxExempt));
    }

    [Fact]
    public void EvaluateAndPlaceOrders_WithExternalBidBelowReference_UndercutsToBestBid()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 100;
        MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            92,
            2_000,
            MarketCommodityType.Food,
            taxExempt: true);

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var bestAsk = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.ActorId == 901)
            .Min(o => o.PriceMoneyPerGo);

        Assert.Equal(92, bestAsk);
    }

    [Fact]
    public void EvaluateAndPlaceOrders_WithExternalBidAtReference_PlacesAsksAtOrAboveMid()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 90;
        MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            90,
            5_000,
            MarketCommodityType.Food,
            taxExempt: true);

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var sells = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.ActorId == 901)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ToList();

        Assert.NotEmpty(sells);
        Assert.Equal(90, sells[0].PriceMoneyPerGo);
        Assert.True(sells[^1].PriceMoneyPerGo >= sells[0].PriceMoneyPerGo);
    }

    [Fact]
    public void EvaluateAndPlaceOrders_UpdatesExistingLevel_InPlace()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 90;
        MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            90,
            5_000,
            MarketCommodityType.Food,
            taxExempt: true);

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);
        var firstPass = stronghold.Market.Orders
            .Single(o => o.PriceMoneyPerGo == 90 && MarketRules.IsSellOrder(o) && o.ActorId == 901);
        var firstId = firstPass.Id;
        var firstQty = firstPass.QuantityGo;

        stronghold.MerchantActors[0].Food += 200_000;
        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var secondPass = stronghold.Market.Orders
            .Single(o => o.PriceMoneyPerGo == 90 && MarketRules.IsSellOrder(o) && o.ActorId == 901);
        Assert.Equal(firstId, secondPass.Id);
        Assert.True(secondPass.QuantityGo >= firstQty);
    }

    [Fact]
    public void EvaluateAndPlaceOrders_EachMerchantRunsIndependentBook()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 800_000, merchantMoney: 300_000);
        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = 902,
            Name = "二号店",
            Type = ActorType.Merchant,
            ForceId = 2,
            StrongholdId = stronghold.Id,
            CharacterIds = [],
            SubUnitIds = [],
            Food = 800_000,
            Money = 300_000,
        });
        stronghold.Market.LastClosePriceMoneyPerGo = 100;
        MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            100,
            8_000,
            MarketCommodityType.Food,
            taxExempt: true);

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);

        foreach (var merchant in stronghold.MerchantActors)
        {
            var sellCount = stronghold.Market.Orders.Count(o =>
                MarketRules.IsSellOrder(o) && o.ActorId == merchant.Id && o.Commodity == MarketCommodityType.Food);

            Assert.InRange(sellCount, 1, MarketConstants.MarketMaxDepthLevels);
        }
    }

    [Fact]
    public void BuildGoAllocations_UsesInverseSquareNearReferenceWeights()
    {
        var allocations = MarketMakerAiHelper.BuildGoAllocations(
            referencePrice: 100,
            actorId: 901,
            totalGo: 100_000,
            maxPerLevel: 50_000,
            defaultMinGo: 500,
            MarketMakerAiHelper.BookSkew.NearReference,
            asksAboveReference: true);

        Assert.NotEmpty(allocations);
        Assert.True(allocations[0].QuantityGo > allocations[^1].QuantityGo);
        Assert.Equal(101, allocations[0].PriceMoneyPerGo);
    }

    private static Stronghold CreateStrongholdWithMerchant(int merchantFood, int merchantMoney)
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(7, 1, new Point3(2, 3));
        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = 901,
            Name = "测试商铺",
            Type = ActorType.Merchant,
            ForceId = 2,
            StrongholdId = stronghold.Id,
            CharacterIds = [],
            SubUnitIds = [],
            Food = merchantFood,
            Money = merchantMoney,
        });
        stronghold.ForceActor.Money = 50_000_000;
        return stronghold;
    }
}
