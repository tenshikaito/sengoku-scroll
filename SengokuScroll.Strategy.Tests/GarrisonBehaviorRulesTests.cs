using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>守军 InStronghold、封锁与抽象出击。</summary>
public class GarrisonBehaviorRulesTests
{
    [Fact]
    public void ThreatApproaching_WhenOutnumbered_HoldsInCityAwaitingRelief()
    {
        var castle = new Point3(5, 4);
        var approachFrom = new Point3(3, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        stronghold.ForceActor.Soldier = 1200;
        stronghold.ForceActor.Morale = 70;

        var enemy = StrategyTestWorldBuilder.CreateTestUnit(2, 2, approachFrom);
        enemy.Soldier = 2000;
        enemy.Directive = UnitDirective.Move;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = enemy;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, enemy);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        Assert.True(GarrisonBehaviorRules.HasFieldBattleProximityThreat(stronghold, ctx.World.GameData));
        Assert.True(GarrisonBehaviorRules.ShouldHoldInCityAwaitingRelief(stronghold, ctx.World.GameData, meta));
#pragma warning disable CS0618 // 回归测试锁定旧入口不会再物化守军。
        Assert.False(GarrisonBehaviorRules.TryPrepareGarrisonOnThreat(
            worldContext, stronghold, ctx.World.GameData, meta));
#pragma warning restore CS0618
        Assert.Null(StrongholdGarrisonRules.FindGarrisonUnit(stronghold, ctx.World.GameData));
        Assert.Equal(1200, stronghold.ForceActor.Soldier);
        Assert.True(GarrisonBehaviorRules.IsStrongholdUnderAttack(stronghold, ctx.World.GameData));
    }

    [Fact]
    public void SiegeTrigger_WhenStrongEnough_AutoComposesInStrongholdUnit()
    {
        var castle = new Point3(5, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        stronghold.ForceActor.Soldier = 2000;
        stronghold.ForceActor.Morale = 70;

        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        var garrison = StrongholdGarrisonActions.EnsureDefenderUnit(
            worldContext, stronghold, ctx.World.GameData, meta);

        Assert.NotNull(garrison);
        Assert.True(garrison!.InStronghold);
        Assert.Equal(stronghold.Id, garrison.LocationStrongholdId);
        Assert.Equal(2000, garrison.Soldier);
        Assert.Equal(0, stronghold.ForceActor.Soldier);
    }

    [Fact]
    public void MapGarrison_WhenOddsTurnBad_EntersStronghold()
    {
        var castle = new Point3(5, 4);
        var approachFrom = new Point3(3, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        var garrison = StrategyTestWorldBuilder.CreateTestUnit(11, 1, castle);
        garrison.Soldier = 1200;
        garrison.Directive = UnitDirective.Support;
        garrison.Stance = UnitStance.Hold;
        garrison.HomeStrongholdId = stronghold.Id;
        garrison.ActionTarget.StrongholdId = stronghold.Id;

        var enemy = StrategyTestWorldBuilder.CreateTestUnit(2, 2, approachFrom);
        enemy.Soldier = 2000;
        enemy.Directive = UnitDirective.Move;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[11] = garrison;
        world.GameData.Units[2] = enemy;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, garrison);
        MapLocationActions.RegisterUnit(world, enemy);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        Assert.True(GarrisonBehaviorRules.TryRetreatGarrisonToCityWhenOutnumbered(
            worldContext, stronghold, ctx.World.GameData, meta));

        var inCity = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, ctx.World.GameData);
        Assert.NotNull(inCity);
        Assert.True(inCity!.InStronghold);
        Assert.Equal(1200, inCity.Soldier);
    }

    [Fact]
    public void EnemyOnStrongholdTile_WhenOutnumbered_AutoComposesInStrongholdOnSiege()
    {
        var castle = new Point3(5, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        stronghold.ForceActor.Soldier = 1500;
        stronghold.ForceActor.Morale = 75;

        var occupier = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        occupier.Soldier = 1800;
        occupier.Directive = UnitDirective.Occupy;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = occupier;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, occupier);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        Assert.True(GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, ctx.World.GameData));
        Assert.True(GarrisonBehaviorRules.ShouldHoldInCityAwaitingRelief(stronghold, ctx.World.GameData, meta));

        var defender = StrongholdGarrisonActions.EnsureDefenderUnit(
            worldContext, stronghold, ctx.World.GameData, meta);
        Assert.NotNull(defender);
        Assert.True(defender!.InStronghold);
        Assert.Equal(1500, defender.Soldier);
        Assert.Equal(0, stronghold.ForceActor.Soldier);
    }

    [Fact]
    public void EnemyOnStrongholdTile_WhenOutnumbered_DoesNotAbstractSally()
    {
        var castle = new Point3(5, 4);
        var adjacent = new Point3(4, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        stronghold.ForceActor.Soldier = 1500;
        stronghold.ForceActor.Morale = 75;

        var occupier = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        occupier.Soldier = 1800;
        occupier.Directive = UnitDirective.Occupy;

        var outsider = StrategyTestWorldBuilder.CreateTestUnit(3, 2, adjacent);
        outsider.Soldier = 400;
        outsider.Directive = UnitDirective.Occupy;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = occupier;
        world.GameData.Units[3] = outsider;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, occupier);
        MapLocationActions.RegisterUnit(world, outsider);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        Assert.False(GarrisonBehaviorRules.TryAbstractSally(
            worldContext, stronghold, ctx.World.GameData, meta, out _));
        Assert.Equal(1500, stronghold.ForceActor.Soldier);
    }

    [Fact]
    public void EncircledGarrison_SalliesFromTileWithoutMovingOutside()
    {
        var castle = new Point3(5, 4);
        var adjacent = new Point3(4, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        var garrison = StrategyTestWorldBuilder.CreateTestUnit(11, 1, castle);
        garrison.Soldier = 1000;
        garrison.Morale = 80;
        garrison.Directive = UnitDirective.Support;
        garrison.Stance = UnitStance.Hold;
        garrison.Status = UnitStatus.BeingSurround;
        garrison.ActionTarget.StrongholdId = stronghold.Id;

        var enemy = StrategyTestWorldBuilder.CreateTestUnit(2, 2, adjacent);
        enemy.Soldier = 500;
        enemy.Directive = UnitDirective.Occupy;
        enemy.SiegeMode = UnitSiegeMode.Encircle;
        enemy.ActionTarget.StrongholdId = stronghold.Id;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[11] = garrison;
        world.GameData.Units[2] = enemy;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, garrison);
        MapLocationActions.RegisterUnit(world, enemy);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        Assert.True(GarrisonBehaviorRules.TryAbstractSally(
            worldContext, stronghold, ctx.World.GameData, meta, out _));

        Assert.Equal(castle, garrison.Location);
        Assert.Equal(UnitStance.Attacking, garrison.Stance);
        Assert.Equal(enemy.Id, garrison.ActionTarget.UnitId);
    }
}
