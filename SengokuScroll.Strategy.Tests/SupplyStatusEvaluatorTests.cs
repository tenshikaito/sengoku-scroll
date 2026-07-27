using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class SupplyStatusEvaluatorTests
{
    [Fact]
    public void EvaluateStatus_Sufficient_WhenFoodAboveThreshold()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var unit = world.GameData.Units[1];
        unit.Food = SupplyDispatchConstants.UnitFoodThresholdGo + 100;

        Assert.Equal(SupplyStatusEvaluator.Sufficient, SupplyStatusEvaluator.EvaluateStatus(unit, world.GameData));
    }

    [Fact]
    public void EvaluateStatus_Strained_WhenLowFoodButInboundConvoy()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var unit = world.GameData.Units[1];
        unit.Food = 100;

        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(2, 1, new Point3(0, 0));
        transport.TransportOriginStrongholdId = 1;
        transport.TransportTargetUnitId = 1;
        transport.Food = 2000;
        transport.ActionTarget.RoutePoints = new Queue<Point2>([new Point2(1, 0)]);
        StrategyTestWorldBuilder.RegisterTransportUnit(world, transport);

        Assert.Equal(SupplyStatusEvaluator.Strained, SupplyStatusEvaluator.EvaluateStatus(unit, world.GameData));
    }

    [Fact]
    public void EvaluateStatus_CutOff_WhenNoFoodNoConvoyNoSource()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var unit = world.GameData.Units[1];
        unit.Food = 0;

        Assert.Equal(SupplyStatusEvaluator.CutOff, SupplyStatusEvaluator.EvaluateStatus(unit, world.GameData));
    }

    [Fact]
    public void GetInTransitSummaries_ExcludesReturningConvoy()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var unit = world.GameData.Units[1];

        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(2, 1, unit.Location);
        transport.TransportOriginStrongholdId = 1;
        transport.TransportTargetUnitId = 1;
        transport.IsReturningToOrigin = true;
        StrategyTestWorldBuilder.RegisterTransportUnit(world, transport);

        Assert.Empty(SupplyStatusEvaluator.GetInTransitSummaries(unit, world.GameData));
    }
}
