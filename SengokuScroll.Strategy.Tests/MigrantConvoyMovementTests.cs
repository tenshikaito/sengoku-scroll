using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class MigrantConvoyMovementTests
{
    [Fact]
    public void MigrantConvoy_WithPopulationOnly_IsNotMarkedDestroyedOnAdvanceDay()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var gameData = ctx.World.GameData;
        var origin = gameData.Strongholds.Values.First();

        gameData.SupplyConvoys[1] = new SupplyConvoy
        {
            Id = 1,
            Name = "移民队",
            ForceId = 0,
            Location = origin.Location,
            OriginStrongholdId = origin.Id,
            TargetStrongholdId = origin.Id,
            Purpose = TransportPurpose.Migrant,
            CargoPopulation = 200,
            CargoFoodGo = 0,
            CargoMoney = 0,
            PorterCount = 10,
            EscortSoldierCount = 0,
            Status = SupplyConvoyStatus.Moving,
            Ap = 1,
            RoutePoints = new Queue<Point3>([new Point3(1, 0)])
        };

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var convoy = Assert.Single(gameData.SupplyConvoys.Values);
        Assert.NotEqual(SupplyConvoyStatus.Destroyed, convoy.Status);
    }
}
