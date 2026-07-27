using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>M4-b 市场撮合与收粮规则。</summary>
public class MarketAndHarvestTests
{
    [Fact]
    public void AddOrMergeBuyOrder_MergesQuantityAtSamePrice()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));

        MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            94,
            500,
            MarketCommodityType.Food,
            taxExempt: true);
        MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            94,
            300,
            MarketCommodityType.Food,
            taxExempt: true);

        var order = Assert.Single(
            stronghold.Market.Orders.Where(o =>
                MarketRules.IsBuyOrder(o)
                && o.ActorId == stronghold.ForceActor.Id
                && o.PriceMoneyPerGo == 94));
        Assert.Equal(800, order.QuantityGo);
    }

    [Fact]
    public void AddOrMergeSellOrder_MergesQuantityAtSamePrice()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));

        MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            94,
            500,
            MarketCommodityType.Food,
            taxExempt: true);
        MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            94,
            200,
            MarketCommodityType.Food,
            taxExempt: true);

        var order = Assert.Single(
            stronghold.Market.Orders.Where(o =>
                MarketRules.IsSellOrder(o)
                && o.ActorId == stronghold.ForceActor.Id
                && o.PriceMoneyPerGo == 94));
        Assert.Equal(700, order.QuantityGo);
    }

    [Fact]
    public void AddOrMergeBuyOrder_WithCommitMoney_DeductsBuyerMoney()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.ForceActor.Money = 10_000;

        var ok = MarketActions.AddOrMergeBuyOrder(
            stronghold,
            stronghold.ForceActor.Id,
            87,
            100,
            MarketCommodityType.Food,
            taxExempt: true,
            commitMoney: true);

        Assert.True(ok);
        Assert.Equal(10_000 - 8700, stronghold.ForceActor.Money);
        var order = Assert.Single(
            stronghold.Market.Orders,
            o => MarketRules.IsBuyOrder(o) && o.PriceMoneyPerGo == 87);
        Assert.True(order.MoneyCommitted);
        Assert.Equal(8700, order.CommittedMoneyGo);
        Assert.Equal(100, order.QuantityGo);
    }

    [Fact]
    public void AddOrMergeSellOrder_WithCommitInventory_DeductsSellerFood()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.ForceActor.Food = 5000;

        var ok = MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            87,
            1200,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true);

        Assert.True(ok);
        Assert.Equal(3800, stronghold.ForceActor.Food);
        var order = Assert.Single(
            stronghold.Market.Orders,
            o => MarketRules.IsSellOrder(o) && o.PriceMoneyPerGo == 87);
        Assert.True(order.InventoryCommitted);
        Assert.Equal(1200, order.CommittedInventoryGo);
        Assert.Equal(1200, order.QuantityGo);
    }

    [Fact]
    public void MatchOrders_BuyMeetsSell_ExecutesTrade()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.CivilianActor.Food = 0;
        stronghold.CivilianActor.Money = 10_000;
        stronghold.ForceActor.Food = 5000;

        stronghold.Market.Orders.Add(new Domain.Entities.MarketOrder
        {
            Id = 1,
            Side = MarketRules.BuySide,
            ActorId = stronghold.CivilianActor.Id,
            PriceMoneyPerGo = 60,
            QuantityGo = 100
        });
        stronghold.Market.Orders.Add(new Domain.Entities.MarketOrder
        {
            Id = 2,
            Side = MarketRules.SellSide,
            ActorId = stronghold.ForceActor.Id,
            PriceMoneyPerGo = 50,
            QuantityGo = 200,
            TaxExempt = true
        });

        var ledger = new Diagnostics.MerchantTaxLedger();
        var result = MarketCalculator.MatchOrders(stronghold, 50);
        var count = Actions.MarketActions.ApplyMatchResult(stronghold, result, ledger);

        Assert.Equal(1, count);
        Assert.Equal(100, stronghold.CivilianActor.Food);
        Assert.Equal(10_000 - 5000, stronghold.CivilianActor.Money);
        Assert.Equal(4900, stronghold.ForceActor.Food);
    }

    [Fact]
    public void HarvestRules_DefaultNorthernSingle_FiresOnNov1()
    {
        var profiles = new Dictionary<int, RegionHarvestProfile>();
        var date = new GameDate(1560, 11, 1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));

        Assert.True(HarvestRules.IsHarvestDay(stronghold, date, profiles, regionId: 0));
        Assert.NotNull(HarvestRules.ResolveTodayEvent(date, profiles, 0));
    }

    [Fact]
    public void ApplyHarvestSettlement_SplitsTaxAndCivilianFood()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.AgricultureTaxRate = 25;
        stronghold.CivilianActor.AgricultureProduction = 100_000;

        var evt = new HarvestEventDefinition(11, 1, 10_000);
        var result = Actions.HarvestEconomyActions.ApplyHarvestSettlement(
            stronghold,
            evt,
            HarvestConstants.DefaultInternalTributeFoodBp);

        Assert.Equal(100_000, result.GrossHarvestGo);
        Assert.True(result.TaxFoodGo > 0);
        Assert.Equal(result.GrossHarvestGo, result.TaxFoodGo + result.CivilianFoodGo);
        Assert.Equal(20_000, result.TributeObligationGo);
    }

    [Fact]
    public void CalculateConvoyTariffMoney_ScalesWithCargoAndRate()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.TariffTaxRate = 10;
        stronghold.CommerceValue = 2000;

        var cargoValue = EconomyCalculator.CalculateConvoyCargoMoneyValue(5000, 100, 50);
        Assert.Equal(10_000, cargoValue);

        var tariff = EconomyCalculator.CalculateConvoyTariffMoney(stronghold, cargoValue);
        Assert.Equal(1000, tariff);
    }

    [Fact]
    public void TryAssessTransitTariff_ForeignTradeConvoy_DeductsFromCargoAndAccruesLedger()
    {
        var origin = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        var transit = StrategyTestWorldBuilder.CreateTestStronghold(2, 2, new Common.Types.Point3(1, 0));
        transit.TariffTaxRate = 10;
        transit.CommerceValue = 2000;

        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(
            7,
            1,
            new Common.Types.Point3(1, 0),
            Domain.Entities.Types.TransportPurpose.Trade,
            UnitKind.Merchant);
        transport.TransportOriginStrongholdId = 1;
        transport.TransportTargetStrongholdId = 2;
        transport.Money = 5000;

        var ledger = new Diagnostics.TariffTaxLedger();
        var paid = Actions.TariffEconomyActions.TryAssessTransitTariff(transport, transit, ledger);

        Assert.Equal(500, paid);
        Assert.Equal(4500, transport.Money);
        Assert.Equal(500, ledger.GetAccrued(transit.Id));
    }

    [Fact]
    public void TryAssessTransitTariff_SameForce_SkipsTariff()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.CommerceValue = 2000;
        stronghold.TariffTaxRate = 10;

        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(
            8,
            1,
            stronghold.Location,
            Domain.Entities.Types.TransportPurpose.Trade,
            UnitKind.Merchant);
        transport.TransportOriginStrongholdId = 2;
        transport.Money = 5000;

        var ledger = new Diagnostics.TariffTaxLedger();
        var paid = Actions.TariffEconomyActions.TryAssessTransitTariff(transport, stronghold, ledger);

        Assert.Equal(0, paid);
        Assert.Equal(5000, transport.Money);
    }

    [Fact]
    public void GovernmentMarketAiHelper_PlacesSellOrder_WhenSurplusFood()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.ForceActor.Food = MarketConstants.GovernmentFoodReserveGo + 2000;
        stronghold.Market.LastClosePriceMoneyPerGo = 40;

        GovernmentMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold);

        var sells = stronghold.Market.Orders
            .Where(o => MarketRules.IsSellOrder(o) && o.ActorId == stronghold.ForceActor.Id)
            .OrderBy(o => o.PriceMoneyPerGo)
            .ToList();

        Assert.InRange(sells.Count, 1, MarketConstants.MarketMaxDepthLevels);
        Assert.Equal(stronghold.ForceActor.Id, sells[0].ActorId);
        Assert.True(sells.All(o => o.TaxExempt));
        Assert.Equal(2000, sells.Sum(o => o.QuantityGo));
        Assert.True(sells[0].QuantityGo >= sells[^1].QuantityGo);
        Assert.Equal(41, sells[0].PriceMoneyPerGo);
    }

    [Fact]
    public void GovernmentMarketAiHelper_DoesNotRemovePlayerRestingSellOrders()
    {
        var gameData = StrategyTestWorldBuilder.BuildMinimalWorld().GameData;
        gameData.GameDate = new GameDate(1560, 6, 1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.ForceActor.Food = MarketConstants.GovernmentFoodReserveGo + 60_000;
        stronghold.Market.LastClosePriceMoneyPerGo = 103;

        var playerQty = 50 * LogisticsConstants.GoPerKoku;
        Assert.True(MarketActions.AddOrMergeSellOrder(
            stronghold,
            stronghold.ForceActor.Id,
            askPrice: 13,
            quantityGo: playerQty,
            MarketCommodityType.Food,
            taxExempt: true,
            commitInventory: true,
            createdDate: gameData.GameDate));

        GovernmentMarketAiHelper.EvaluateAndPlaceSellOrders(stronghold);

        Assert.Contains(
            stronghold.Market.Orders,
            o => MarketRules.IsSellOrder(o) && o.PriceMoneyPerGo == 13 && o.QuantityGo == playerQty);
        Assert.Contains(
            stronghold.Market.Orders,
            o => MarketRules.IsSellOrder(o) && o.PriceMoneyPerGo == 104);
    }

    [Fact]
    public void ApplyMatchResult_GovernmentSellOrder_CreditsForceActorMoney()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.ForceActor.Food = 10_000;
        stronghold.ForceActor.Money = 0;
        stronghold.CivilianActor.Money = 20_000;
        stronghold.CivilianActor.Food = 0;

        MarketActions.UpsertGovernmentSellOrder(stronghold, 50, 100);
        MarketActions.UpsertCivilianBuyOrder(stronghold, 60, 100);

        var ledger = new Diagnostics.MerchantTaxLedger();
        var result = MarketCalculator.MatchOrders(stronghold, 50);
        Actions.MarketActions.ApplyMatchResult(stronghold, result, ledger);

        Assert.Equal(5000, stronghold.ForceActor.Money);
        Assert.Equal(15_000, stronghold.CivilianActor.Money);
        Assert.Equal(100, stronghold.CivilianActor.Food);
    }

    [Fact]
    public void TributeArrearsActions_AccruesOnVassalDiplomacy_WhenFoodShortfall()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var vassalForce = StrategyTestWorldBuilder.CreateTestForce(2);
        vassalForce.SuzerainForceId = 1;
        vassalForce.Status = Domain.Entities.Force.ForceStatus.InnerVassal;
        vassalForce.Diplomacies =
        [
            new Domain.Entities.Diplomacy
            {
                ForceId = 2,
                TargetForceId = 1,
                Relation = Domain.Entities.Diplomacy.DiplomacyRelation.Neutral
            }
        ];
        world.GameData.Forces[2] = vassalForce;

        var origin = StrategyTestWorldBuilder.CreateTestStronghold(2, 2, new Common.Types.Point3(1, 0), food: 100);
        world.GameData.Strongholds[2] = origin;

        Actions.TributeArrearsActions.AccrueShortfall(world.GameData, origin, 5000, 0);

        Assert.Equal(5000, vassalForce.Diplomacies[0].ArrearsFoodGo);
    }

    [Fact]
    public void CompleteTradeArrival_CreditsOriginAndSuppliesDestinationCivilian()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var origin = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        var destination = StrategyTestWorldBuilder.CreateTestStronghold(2, 1, new Common.Types.Point3(1, 0));
        destination.Market.LastClosePriceMoneyPerGo = 60;
        destination.CivilianActor.Food = 0;
        origin.ForceActor.Money = 0;

        world.GameData.Strongholds[1] = origin;
        world.GameData.Strongholds[2] = destination;

        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(
            3,
            1,
            destination.Location,
            Domain.Entities.Types.TransportPurpose.Trade,
            UnitKind.Merchant);
        transport.TransportOriginStrongholdId = 1;
        transport.TransportTargetStrongholdId = 2;
        transport.Food = 1000;

        var revenue = Actions.TradeEconomyActions.CompleteTradeArrival(
            transport,
            origin,
            destination,
            world.GameData);

        Assert.Equal(60_000, revenue);
        Assert.Equal(60_000, origin.ForceActor.Money);
        Assert.Equal(1000, destination.CivilianActor.Food);
    }

    [Fact]
    public void TributeArrearsActions_AccruesInternal_WhenSameForceShortfall()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var origin = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0), food: 0);
        world.GameData.Strongholds[1] = origin;

        TributeArrearsActions.AccrueShortfall(world.GameData, origin, 0, 3000);

        Assert.Equal(3000, world.GameData.Forces[1].InternalArrearsMoney);
    }

    [Fact]
    public void CanTrade_WithMarketFacility_IgnoresLowCommerceValue()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.CommerceValue = 100;
        stronghold.EconomyFacilityIds = [EconomyFacilityConstants.MarketFacilityTypeId];

        Assert.True(MarketRules.CanTrade(stronghold));
    }

    [Fact]
    public void CanTrade_WithoutMarketFacility_ReturnsFalse()
    {
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Common.Types.Point3(0, 0));
        stronghold.CommerceValue = 5000;
        stronghold.EconomyFacilityIds = [];

        Assert.False(MarketRules.CanTrade(stronghold));
    }

    [Fact]
    public void TransportRules_AdjacentEnemy_IncreasesThreat()
    {
        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(
            1,
            1,
            new Common.Types.Point3(5, 5),
            Domain.Entities.Types.TransportPurpose.Trade,
            UnitKind.Merchant);
        transport.TransportOriginStrongholdId = 1;

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var enemy = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Common.Types.Point3(5, 6));
        enemy.IsMilitary = true;
        enemy.Soldier = 100;
        world.GameData.Units[2] = enemy;
        world.GameData.Forces[2] = StrategyTestWorldBuilder.CreateTestForce(2);
        world.GameData.Forces[2].Diplomacies =
        [
            new Domain.Entities.Diplomacy
            {
                ForceId = 2,
                TargetForceId = 1,
                Relation = Domain.Entities.Diplomacy.DiplomacyRelation.Enemy
            }
        ];

        var threat = TransportRules.EvaluateThreatLevel(transport, world.GameData);
        Assert.True(threat > 0);
    }
}
