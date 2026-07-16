using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>坕佝途绝/进入杮点格的移动测试�?/summary>
public class UnitMoveThroughStrongholdTests
{
    [Fact]
    public void UnitMoveEvaluator_AllowsMoveOntoFriendlyStronghold()
    {
        using var ctx = CreateStrongholdOnPathContext();
        var evaluator = ctx.Services.GetRequiredService<UnitMoveEvaluator>();
        var unit = ctx.World.GameData.Units[1];

        unit.Ap = 10;

        var result = evaluator.Evaluate(unit, new Point2(1, 0));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AdvanceDay_UnitPassesThroughStrongholdOnRoute()
    {
        using var ctx = CreateStrongholdOnPathContext();
        var unit = ctx.World.GameData.Units[1];

        unit.Status = UnitStatus.Moving;
        unit.Ap = 10;
        unit.IsReadyToMove = true;
        unit.ActionTarget.RoutePoints.Clear();
        unit.ActionTarget.RoutePoints.Enqueue(new Point2(1, 0));
        unit.ActionTarget.RoutePoints.Enqueue(new Point2(2, 0));

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(new Point3(2, 0), unit.Location);
        Assert.Equal(UnitStatus.Moving, unit.Status);
    }

    [Fact]
    public void Pathfinding_FindsPathThroughFriendlyStronghold()
    {
        using var ctx = CreateStrongholdOnPathContext();
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var unit = ctx.World.GameData.Units[1];

        var path = pathfinding.CalculatePath(unit, new Point2(2, 0));

        Assert.NotNull(path);
        Assert.Contains(path!, n => n.Location == new Point2(1, 0));
    }

    [Fact]
    public void AdvanceDay_UnitAt1_2_Reaches3_2_ThroughFriendlyInuyama()
    {
        var scenarioPath = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(scenarioPath);
        var world = loaded.World;
        IsolateEnemyUnits(world);
        var unit = world.GameData.Units[1];

        TeleportUnit(world, unit, new Point3(1, 2));
        unit.Ap = 5;
        unit.Status = UnitStatus.Waiting;
        unit.ActionTarget.RoutePoints.Clear();

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var path = pathfinding.CalculatePath(unit, new Point2(3, 2));

        Assert.NotNull(path);

        unit.Status = UnitStatus.Moving;
        foreach (var node in path!.Skip(1))
            unit.ActionTarget.RoutePoints.Enqueue(node.Location);

        for (var day = 0; day < 6; day++)
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.Equal(3, unit.Location.X);
        Assert.Equal(2, unit.Location.Y);
    }

    private static void TeleportUnit(GameWorld world, Unit unit, Point3 location)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var oldIndex = tileMap.GetIndex(unit.Location);
        if (world.GameMapData.Units.TryGetValue(oldIndex, out var __list)) { __list.Remove(unit.Id); if (__list.Count==0) world.GameMapData.Units.Remove(oldIndex); }
        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }

    private static void IsolateEnemyUnits(GameWorld world)
    {
        foreach (var unit in world.GameData.Units.Values.Where(u => u.ForceId != 1).ToList())
            TeleportUnit(world, unit, new Point3(9, 9));
    }

    [Fact]
    public void AdvanceDay_ResumesMovingAfterApShortfall()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(1, 0));
        world.GameData.Strongholds[1] = stronghold;
        world.GameData.Units[1] = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0), food: 0);
        world.GameData.Units[1].Ap = 0;
        MapLocationActions.RegisterStronghold(world, stronghold);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[1]);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var unit = ctx.World.GameData.Units[1];

        unit.Status = UnitStatus.Moving;
        unit.IsReadyToMove = true;
        unit.ActionTarget.RoutePoints.Clear();
        unit.ActionTarget.RoutePoints.Enqueue(new Point2(1, 0));

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
        Assert.Equal(new Point3(0, 0), unit.Location);
        Assert.Equal(UnitStatus.Moving, unit.Status);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
        Assert.Equal(new Point3(1, 0), unit.Location);
    }

    [Fact]
    public void UnitMoveEvaluator_AllowsMoveOntoSameForceMilitaryTile()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units[1] = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0), food: 0);
        world.GameData.Units[2] = StrategyTestWorldBuilder.CreateTestUnit(2, 1, new Point3(1, 0), food: 0);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[1]);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[2]);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var evaluator = ctx.Services.GetRequiredService<UnitMoveEvaluator>();
        var unit = ctx.World.GameData.Units[1];
        unit.Ap = 10;

        var result = evaluator.Evaluate(unit, new Point2(1, 0));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Pathfinding_AllowsPathThroughSameForceMilitaryTile()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units[1] = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0), food: 0);
        world.GameData.Units[2] = StrategyTestWorldBuilder.CreateTestUnit(2, 1, new Point3(1, 0), food: 0);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[1]);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[2]);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var unit = ctx.World.GameData.Units[1];

        var path = pathfinding.CalculatePath(unit, new Point2(2, 0));

        Assert.NotNull(path);
        Assert.Contains(path!, n => n.Location == new Point2(1, 0));
    }

    private static StrategyTestContext CreateStrongholdOnPathContext()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, new Point3(1, 0));
        world.GameData.Strongholds[1] = stronghold;
        world.GameData.Units[1] = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0), food: 0);
        world.GameData.Units[1].Ap = 30;

        MapLocationActions.RegisterStronghold(world, stronghold);
        MapLocationActions.RegisterUnit(world, world.GameData.Units[1]);

        return StrategyTestWorldFactory.CreateFromWorld(world);
    }
}
