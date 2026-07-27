using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class MigrantConvoyMovementTests
{
    [Fact]
    public void MigrantConvoy_WithPopulationOnly_IsNotMarkedDestroyedOnAdvanceDay()
    {
        using var ctx = StrategyTestWorldFactory.CreateLogisticsWorld();
        var gameData = ctx.World.GameData;
        var origin = gameData.Strongholds.Values.First();

        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(
            2,
            forceId: 0,
            origin.Location,
            TransportPurpose.Migrant,
            UnitKind.Migrant);
        transport.TransportOriginStrongholdId = origin.Id;
        transport.TransportTargetStrongholdId = origin.Id;
        transport.CargoPopulation = 200;
        transport.EscortSoldierCount = 0;
        transport.Ap = 1;
        transport.ActionTarget.RoutePoints = new Queue<Point2>([new Point2(1, 0)]);
        StrategyTestWorldBuilder.RegisterTransportUnit(ctx.World, transport);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var remaining = Assert.Single(
            gameData.Units.Values.Where(TransportUnitRules.IsTransportUnit));
        Assert.Equal(UnitStatus.Moving, remaining.Status);
    }
}
