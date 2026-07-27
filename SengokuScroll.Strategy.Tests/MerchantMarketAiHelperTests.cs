using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class MerchantMarketAiHelperTests
{
    [Fact]
    public void EvaluateAndPlaceOrders_PlacesVariableDepthAsks_WithNearHeavySizing()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 90;

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var sells = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.Commodity == MarketCommodityType.Food)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ToList();

        Assert.InRange(sells.Count, MarketConstants.MarketMinDepthLevels, MarketConstants.MarketMaxDepthLevels);
        Assert.Equal(91, sells[0].PriceMoneyPerGo);
        Assert.True(sells[^1].PriceMoneyPerGo > sells[0].PriceMoneyPerGo);
        Assert.True(sells[0].QuantityGo > sells[^1].QuantityGo);
        Assert.All(sells, o => Assert.False(o.TaxExempt));
    }

    [Fact]
    public void EvaluateAndPlaceOrders_PlacesVariableDepthBids_BelowReference()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 90;

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var buys = stronghold.Market.Orders
            .Where(o => MarketRules.IsBuyOrder(o) && o.Commodity == MarketCommodityType.Food)
            .OrderByDescending(o => o.PriceMoneyPerGo)
            .ToList();

        Assert.InRange(buys.Count, MarketConstants.MarketMinDepthLevels, MarketConstants.MarketMaxDepthLevels);
        Assert.True(buys[0].PriceMoneyPerGo <= 89);
        Assert.True(buys[0].PriceMoneyPerGo > buys[^1].PriceMoneyPerGo);
        Assert.True(buys[^1].QuantityGo >= buys[0].QuantityGo);
    }

    [Fact]
    public void EvaluateAndPlaceOrders_UpdatesExistingLevel_InPlace()
    {
        var stronghold = CreateStrongholdWithMerchant(merchantFood: 500_000, merchantMoney: 300_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 90;

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);
        var firstPass = stronghold.Market.Orders
            .Single(o => o.PriceMoneyPerGo == 91 && MarketRules.IsSellOrder(o));
        var firstId = firstPass.Id;
        var firstQty = firstPass.QuantityGo;

        stronghold.MerchantActors[0].Food += 200_000;
        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold);

        var secondPass = stronghold.Market.Orders
            .Single(o => o.PriceMoneyPerGo == 91 && MarketRules.IsSellOrder(o));
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

        MerchantMarketAiHelper.EvaluateAndPlaceOrders(stronghold, MarketCommodityType.Food);

        foreach (var merchant in stronghold.MerchantActors)
        {
            var sellCount = stronghold.Market.Orders.Count(o =>
                MarketRules.IsSellOrder(o) && o.ActorId == merchant.Id && o.Commodity == MarketCommodityType.Food);
            var buyCount = stronghold.Market.Orders.Count(o =>
                MarketRules.IsBuyOrder(o) && o.ActorId == merchant.Id && o.Commodity == MarketCommodityType.Food);

            Assert.InRange(sellCount, 1, MarketConstants.MarketMaxDepthLevels);
            Assert.InRange(buyCount, 1, MarketConstants.MarketMaxDepthLevels);
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
        return stronghold;
    }
}
