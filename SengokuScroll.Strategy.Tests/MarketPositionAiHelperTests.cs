using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class MarketPositionAiHelperTests
{
    [Fact]
    public void CalculateBuyQuantityToBalance_WhenMoneyHeavy_ReturnsPositiveBuy()
    {
        var actor = new StrongholdActor
        {
            Id = 1,
            Name = "测试",
            Type = ActorType.Merchant,
            ForceId = 1,
            StrongholdId = 1,
            CharacterIds = [],
            SubUnitIds = [],
            Money = 100_000,
            Food = 100,
        };

        var qty = MarketPositionAiHelper.CalculateBuyQuantityToBalance(
            actor,
            referencePrice: 100,
            targetMoneyShareBp: MarketConstants.TargetMoneyShareBp,
            MarketCommodityType.Food);

        Assert.True(qty > 0);
    }

    [Fact]
    public void OpportunisticCrashBuy_SmashingAsks_AndLeavesRestingBid()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0), food: 50_000);
        stronghold.Market.LastClosePriceMoneyPerGo = 100;
        stronghold.ForceActor.Money = 500_000;
        stronghold.ForceActor.Food = 100;

        MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.CivilianActor.Id,
            80,
            5_000,
            MarketCommodityType.Food,
            taxExempt: true);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Strongholds[stronghold.Id] = stronghold;
        var taxLedger = new MerchantTaxLedger();
        var ctx = new MarketAiDayContext
        {
            World = world,
            GameData = world.GameData,
            ScenarioMeta = new Data.Models.StrategyScenarioMeta
            {
                PlayerForceId = 1,
                LordUnitId = 1,
                LordName = "测试",
                RegionHarvestProfiles = new Dictionary<int, Data.Models.RegionHarvestProfile>(),
            },
            TaxLedger = taxLedger,
            AllowOpportunisticSmash = true,
        };

        var signals = new StrongholdMarketSignals(
            EnemyForceCount: 0,
            EnemyNearStronghold: false,
            IsBlockaded: false,
            IsHarvestDay: false,
            HarvestFearWindow: false,
            PriceCrashObserved: true,
            PriceRallyObserved: false,
            HoardBiasBp: 0,
            DumpBiasBp: 0);

        MarketPositionAiHelper.EvaluateOpportunisticTrades(stronghold, signals, ctx);

        Assert.True(stronghold.ForceActor.Food > 100);
        Assert.Contains(
            stronghold.Market.Orders,
            o => MarketRules.IsBuyOrder(o)
                 && o.ActorId == stronghold.ForceActor.Id
                 && o.PriceMoneyPerGo == 80
                 && o.QuantityGo > 0);
    }
}
