using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class TradeMarketAiIntelTests
{
    [Fact]
    public void TradeDispatch_RequiresKnownAndFreshDestinationPrice()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var gameData = world.GameData;
        gameData.GameDate = new GameDate(1560, 1, 1);

        var origin = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(0, 0));
        var destination = StrategyTestWorldBuilder.CreateTestStronghold(2, 1, new Point3(1, 0));
        origin.Market.SetLastClose(MarketCommodityType.Food, 100);
        destination.Market.SetLastClose(MarketCommodityType.Food, 200);
        destination.Population = 10_000;
        destination.CivilianActor.Food = 0;
        destination.CivilianActor.Money = 100_000_000;
        gameData.Strongholds[origin.Id] = origin;
        gameData.Strongholds[destination.Id] = destination;

        var merchantForce = OrganizationForceHelper.GetOrCreate(
            gameData,
            "测试商会",
            ForceCategory.Merchant);
        var merchant = new StrongholdActor
        {
            Id = 901,
            Name = "测试商会",
            Type = ActorType.Merchant,
            ForceId = merchantForce.Id,
            StrongholdId = origin.Id,
            Food = MarketConstants.MerchantFoodReserveGo + LogisticsConstants.DefaultConvoyCargoGo,
            Money = 1_000_000,
            CharacterIds = [],
            SubUnitIds = []
        };
        origin.MerchantActors.Add(merchant);

        var ledger = new StrategyIntelligenceLedger();
        Assert.False(TradeMarketAiHelper.ShouldDispatchTrade(origin, destination, merchant, gameData, ledger));

        ledger.Record(new StrategyIntelligenceLedger.MarketPriceObservation(
            merchantForce.Id,
            destination.Id,
            200,
            gameData.GameDate,
            gameData.GameDate,
            "BranchShop",
            10_000));

        Assert.True(TradeMarketAiHelper.ShouldDispatchTrade(origin, destination, merchant, gameData, ledger));

        gameData.GameDate = gameData.GameDate.AddDays(MarketConstants.TradeIntelMaxAgeDays + 1);
        Assert.False(TradeMarketAiHelper.ShouldDispatchTrade(origin, destination, merchant, gameData, ledger));
    }
}
