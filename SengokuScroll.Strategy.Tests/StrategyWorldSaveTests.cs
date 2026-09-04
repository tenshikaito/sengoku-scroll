using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Persistence;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

/// <summary>存档捕获/恢复（M3-d）测试。</summary>
public class StrategyWorldSaveTests
{
    [Fact]
    public void CaptureAndApply_RestoresForceTreasuryAndUnitPosition()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;

        var unit = world.GameData.Units[1];
        unit.Location = new Common.Types.Point3(5, 5);
        unit.Soldier = 42;
        world.GameData.Forces[1].Money = 99999;

        var ledger = new StrategyVisibilityLedger();
        ledger.Initialize(world, loaded.Meta);

        var save = StrategyWorldSaveService.Capture(
            world,
            "mini_kanto",
            loaded.Meta.PlayerForceId,
            ledger,
            loaded.Meta);

        unit.Location = new Common.Types.Point3(0, 0);
        unit.Soldier = 1;
        world.GameData.Forces[1].Money = 0;

        StrategyWorldSaveService.Apply(save, world);

        Assert.Equal(99999, world.GameData.Forces[1].Money);
        Assert.Equal(42, world.GameData.Units[1].Soldier);
        Assert.Equal(5, world.GameData.Units[1].Location.X);
        Assert.Equal(5, world.GameData.Units[1].Location.Y);
        var restoredIndex = world.GameMapMasterData.TileMap.GetIndex(new Common.Types.Point2(5, 5));
        Assert.Contains(1, world.GameMapData.Units[restoredIndex]);
        Assert.Equal(StrategyDifficulty.Normal.ToString(), save.Difficulty);
        Assert.NotNull(save.StartOptions);
    }

    [Fact]
    public void V2RoundTrip_RestoresDynamicAndRemovedEntitiesMarketsDiplomacyAndBattles()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;

        // 模拟长局中的销毁、新建与跨系统运行时状态。
        world.GameData.Units.Remove(2);
        var stronghold = world.GameData.Strongholds[1];
        stronghold.MerchantActors.Add(new StrongholdActor
        {
            Id = 999_000,
            Name = "动态商户",
            Type = ActorType.Merchant,
            ForceId = stronghold.ForceId,
            StrongholdId = stronghold.Id,
            CharacterIds = [],
            SubUnitIds = []
        });
        var destination = world.GameData.Strongholds.Values.First(s => s.Id != stronghold.Id);
        var dynamicTransport = ConvoyUnitFactory.CreateTransportUnit(
            world,
            "动态贸易商队",
            forceId: stronghold.ForceId,
            leaderId: 0,
            location: stronghold.Location,
            originStrongholdId: stronghold.Id,
            targetUnitId: 0,
            targetStrongholdId: destination.Id,
            foodCargo: 12_345,
            moneyCargo: 6_789,
            cargoPopulation: 0,
            purpose: TransportPurpose.Trade,
            routePoints: new Queue<Point2>([new Point2(3, 8), new Point2(4, 8)]));

        stronghold.MerchantActors[0].Money = 54_321;
        stronghold.Market.Orders.Add(new MarketOrder
        {
            Id = 999_001,
            Side = "Buy",
            ActorId = stronghold.MerchantActors[0].Id,
            PriceMoneyPerGo = 87,
            QuantityGo = 321,
            OriginalQuantityGo = 321,
            Commodity = MarketCommodityType.Food
        });
        stronghold.Market.SetLastClose(MarketCommodityType.Food, 87);

        var diplomacy = world.GameData.Forces[1].Diplomacies.First();
        diplomacy.Relationship = -77;
        diplomacy.ArrearsFoodGo = 4_444;

        world.GameData.Wars[77] = new War
        {
            Id = 77,
            AggressorForceId = 1,
            DefenderForceId = 2,
            AggressorForceIds = [1],
            DefenderForceIds = [2],
            StartDate = world.GameData.GameDate,
            AggressorWarScore = 42,
            WarScoreEvents =
            [
                new WarScoreEvent
                {
                    Date = world.GameData.GameDate,
                    Delta = 42,
                    Reason = "StrongholdOccupied",
                    ActingForceId = 1,
                    TargetForceId = 2,
                    SourceEntityId = stronghold.Id,
                }
            ]
        };
        world.GameData.Battlefields[88] = new Battlefield
        {
            Id = 88,
            Kind = BattlefieldKind.Field,
            Location = new Point2(6, 6),
            WarId = 77,
            SideAUnitIds = [1],
            SideBUnitIds = [],
            MainCombatantAUnitId = 1,
            StandoffDays = 9
        };
        world.GameData.NextBattlefieldId = 89;

        var ledger = new StrategyVisibilityLedger();
        ledger.Initialize(world, loaded.Meta);
        var captured = StrategyWorldSaveService.Capture(
            world,
            "mini_kanto",
            loaded.Meta.PlayerForceId,
            ledger,
            loaded.Meta);

        // 必须经过真实 JSON 边界，防止测试只验证内存对象引用。
        var serialized = StrategySimulationHost.SerializeSave(captured);
        var parsed = StrategySimulationHost.DeserializeSave(serialized);
        Assert.True(parsed.IsSuccess);

        var freshWorld = StrategyScenarioLoader.LoadFromFile(path).World;
        StrategyWorldSaveService.Apply(parsed.Value!, freshWorld);

        Assert.Equal(2, captured.FormatVersion);
        Assert.NotNull(captured.RuntimeState);
        Assert.DoesNotContain(2, freshWorld.GameData.Units.Keys);
        var restoredTransport = freshWorld.GameData.Units[dynamicTransport.Id];
        Assert.Equal(UnitKind.Merchant, restoredTransport.Kind);
        Assert.Equal(12_345, restoredTransport.Food);
        Assert.Equal(6_789, restoredTransport.Money);
        Assert.Equal(2, restoredTransport.ActionTarget.RoutePoints.Count);
        Assert.Equal(54_321, freshWorld.GameData.Strongholds[stronghold.Id].MerchantActors[0].Money);
        Assert.Contains(freshWorld.GameData.Strongholds[stronghold.Id].Market.Orders, order => order.Id == 999_001);
        Assert.Equal(87, freshWorld.GameData.Strongholds[stronghold.Id].Market.LastClosePriceMoneyPerGo);
        Assert.Equal(-77, freshWorld.GameData.Forces[1].Diplomacies.First().Relationship);
        Assert.Equal(4_444, freshWorld.GameData.Forces[1].Diplomacies.First().ArrearsFoodGo);
        Assert.Equal(9, freshWorld.GameData.Battlefields[88].StandoffDays);
        Assert.Equal(42, freshWorld.GameData.Wars[77].AggressorWarScore);
        Assert.Equal("StrongholdOccupied", Assert.Single(freshWorld.GameData.Wars[77].WarScoreEvents).Reason);
        Assert.Equal(89, freshWorld.GameData.NextBattlefieldId);
        var restoredTile = freshWorld.GameMapMasterData.TileMap.GetIndex(restoredTransport.Location);
        Assert.Contains(restoredTransport.Id, freshWorld.GameMapData.Units[restoredTile]);
    }
}
