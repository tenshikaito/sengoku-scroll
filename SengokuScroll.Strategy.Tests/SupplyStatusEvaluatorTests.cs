using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
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

        world.GameData.SupplyConvoys[1] = new SupplyConvoy
        {
            Id = 1,
            Name = "测试粮运队",
            ForceId = 1,
            Location = new Point3(0, 0),
            OriginStrongholdId = 1,
            TargetUnitId = 1,
            CargoFoodGo = 2000,
            PorterCount = 10,
            EscortSoldierCount = 5,
            Status = SupplyConvoyStatus.Moving,
            RoutePoints = new Queue<Point3>([new Point3(1, 0)])
        };

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

        world.GameData.SupplyConvoys[1] = new SupplyConvoy
        {
            Id = 1,
            Name = "测试粮运队",
            ForceId = 1,
            Location = unit.Location,
            OriginStrongholdId = 1,
            TargetUnitId = 1,
            CargoFoodGo = 0,
            PorterCount = 10,
            EscortSoldierCount = 5,
            Status = SupplyConvoyStatus.Moving,
            IsReturningToOrigin = true
        };

        Assert.Empty(SupplyStatusEvaluator.GetInTransitSummaries(unit, world.GameData));
    }
}
