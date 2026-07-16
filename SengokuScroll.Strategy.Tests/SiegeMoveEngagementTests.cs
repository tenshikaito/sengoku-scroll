using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>进城后占领方针应自动触发攻城接敌。</summary>
public class SiegeMoveEngagementTests
{
    [Fact]
    public void ShouldEngage_SameTileOccupyVsGarrison_ReturnsTrue()
    {
        var castle = new Point3(5, 4);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        attacker.Soldier = 2000;
        attacker.Directive = UnitDirective.Occupy;

        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        defender.Soldier = 1200;

        using var ctx = StrategyTestWorldFactory.Create();
        var forceA = ctx.World.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        ctx.World.GameData.Units[1] = attacker;
        ctx.World.GameData.Units[2] = defender;
        ctx.World.GameData.Forces[2] = forceB;
        ctx.World.GameData.Strongholds[10] = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);

        Assert.True(MoveEngagementRules.ShouldEngage(attacker, defender, forceA, forceB, ctx.World.GameData));
    }

    [Fact]
    public void AdvanceDay_AfterMovingIntoGarrisonedCity_QueuesSiegeBattle()
    {
        var castle = new Point3(5, 4);
        var adjacent = new Point3(4, 4);

        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, adjacent);
        attacker.Soldier = 2000;
        attacker.Morale = 80;
        attacker.Directive = UnitDirective.Occupy;
        attacker.Ap = 20;

        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        defender.Soldier = 1200;
        defender.Morale = 70;
        defender.Directive = UnitDirective.Move;

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[1] = attacker;
        world.GameData.Units[2] = defender;
        world.GameData.Strongholds[10] = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        world.GameData.Strongholds[11] = StrategyTestWorldBuilder.CreateTestStronghold(11, 1, new Point3(0, 0));
        MapLocationActions.RegisterUnit(world, attacker);
        MapLocationActions.RegisterUnit(world, defender);
        MapLocationActions.RegisterStronghold(world, world.GameData.Strongholds[10]);
        MapLocationActions.RegisterStronghold(world, world.GameData.Strongholds[11]);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();

        var path = pathfinding.CalculatePath(attacker, (Point2)castle);
        Assert.NotNull(path);

        attacker.Status = UnitStatus.Moving;
        attacker.ActionTarget.RoutePoints.Clear();
        foreach (var node in path!.Skip(1))
            attacker.ActionTarget.RoutePoints.Enqueue(node.Location);

        ctx.Services.GetRequiredService<IStrategyUnitSystem>().Update();
        ctx.Services.GetRequiredService<IStrategyMoveEngagementSystem>().Update();

        Assert.Equal(castle, attacker.Location);
        Assert.Equal(UnitStance.Attacking, attacker.Stance);
        Assert.Equal(defender.Id, attacker.ActionTarget.UnitId);
        Assert.Equal(BattleEngagementKind.Siege,
            BattleEngagementClassifier.Classify(attacker, defender, ctx.World.GameData));
    }
}
