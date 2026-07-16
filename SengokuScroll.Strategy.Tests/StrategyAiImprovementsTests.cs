using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Definitions;
using static SengokuScroll.Domain.Definitions.CharacterDefinition;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>AI 自动攻城、运输封锁、守军解散、居城援防。</summary>
public class StrategyAiImprovementsTests
{
    [Fact]
    public void ExecuteDailyAction_AutoAssault_WhenOnEnemyStrongholdTile()
    {
        var castle = new Point3(5, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(2, 1, castle);
        attacker.Soldier = 2000;
        attacker.Directive = UnitDirective.Occupy;
        attacker.Ap = 10;
        attacker.SiegeMode = UnitSiegeMode.None;
        attacker.Status = UnitStatus.Waiting;
        attacker.ActionTarget.RoutePoints.Clear();

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = attacker;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, attacker);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();
        var rules = ctx.Services.GetRequiredService<GameRuleConfig>();

        var decision = StrategyUnitAIRules.ExecuteDailyAction(
            attacker,
            ctx.World.GameData,
            pathfinding,
            StrategyUnitAIRules.ResolveHostileUnits(attacker, ctx.World.GameData),
            StrategyUnitAIRules.ResolveHostileStrongholds(attacker, ctx.World.GameData),
            worldContext,
            rules,
            mapMaster: ctx.World.GameMapMasterData);

        Assert.True(decision.IsSuccess);
        Assert.Equal("SiegeAssault", decision.Code);
        Assert.Equal(UnitSiegeMode.Assault, attacker.SiegeMode);
    }

    [Fact]
    public void ExecuteDailyAction_AutoEncircle_WhenAdjacentToEnemyStronghold()
    {
        var castle = new Point3(5, 4);
        var adjacent = new Point3(4, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(2, 1, adjacent);
        attacker.Soldier = 2000;
        attacker.Directive = UnitDirective.Occupy;
        attacker.Ap = 10;
        attacker.SiegeMode = UnitSiegeMode.None;
        attacker.Status = UnitStatus.Waiting;
        attacker.ActionTarget.RoutePoints.Clear();

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = attacker;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, attacker);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();
        var rules = ctx.Services.GetRequiredService<GameRuleConfig>();

        var decision = StrategyUnitAIRules.ExecuteDailyAction(
            attacker,
            ctx.World.GameData,
            pathfinding,
            StrategyUnitAIRules.ResolveHostileUnits(attacker, ctx.World.GameData),
            StrategyUnitAIRules.ResolveHostileStrongholds(attacker, ctx.World.GameData),
            worldContext,
            rules,
            mapMaster: ctx.World.GameMapMasterData);

        Assert.True(decision.IsSuccess);
        Assert.Equal("SiegeEncircle", decision.Code);
        Assert.Equal(UnitSiegeMode.Encircle, attacker.SiegeMode);
    }

    [Fact]
    public void BlockadedStronghold_DoesNotDispatchSupplyConvoy()
    {
        var world = StrategyTestWorldBuilder.BuildLogisticsWorld(new Point3(3, 0), unitFood: 100);
        var stronghold = world.GameData.Strongholds[1];
        var unit = world.GameData.Units[1];

        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(world.GameData.Forces[1], forceB);
        var occupier = StrategyTestWorldBuilder.CreateTestUnit(2, 2, stronghold.Location);
        occupier.Soldier = 1500;
        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = occupier;
        MapLocationActions.RegisterUnit(world, occupier);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var dispatchHelper = ctx.Services.GetRequiredService<SupplyConvoyDispatchHelper>();

        Assert.True(GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, ctx.World.GameData));
        Assert.Equal(0, dispatchHelper.DispatchNeededConvoys());
        Assert.Empty(ctx.World.GameData.SupplyConvoys);
        Assert.True(unit.Food < SupplyDispatchConstants.UnitFoodThresholdGo);
    }

    [Fact]
    public void BlockadedOriginConvoy_DoesNotAdvanceOutbound()
    {
        var world = StrategyTestWorldBuilder.BuildLogisticsWorld(new Point3(3, 0), unitFood: 100);
        var stronghold = world.GameData.Strongholds[1];

        var convoy = new SupplyConvoy
        {
            Id = 1,
            Name = "测试粮运",
            ForceId = 1,
            Location = stronghold.Location,
            OriginStrongholdId = stronghold.Id,
            TargetUnitId = 1,
            CargoFoodGo = 500,
            Purpose = TransportPurpose.Supply,
            Status = SupplyConvoyStatus.Moving,
            Ap = 1,
            Movement = 2,
            RoutePoints = new Queue<Point3>([new Point3(1, 0), new Point3(2, 0)])
        };
        world.GameData.SupplyConvoys[1] = convoy;

        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(world.GameData.Forces[1], forceB);
        var occupier = StrategyTestWorldBuilder.CreateTestUnit(2, 2, stronghold.Location);
        occupier.Soldier = 1500;
        world.GameData.Forces[2] = forceB;
        world.GameData.Units[2] = occupier;
        MapLocationActions.RegisterUnit(world, occupier);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var supplySystem = ctx.Services.GetRequiredService<IStrategySupplySystem>();
        var startLocation = convoy.Location;

        supplySystem.Update();

        Assert.Equal(startLocation, convoy.Location);
        Assert.Equal(1, convoy.Ap);
    }

    [Fact]
    public void TryDissolveGarrisonWhenSafe_AbsorbsSoldiersBackIntoCity()
    {
        var castle = new Point3(5, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        var garrison = StrategyTestWorldBuilder.CreateTestUnit(11, 1, castle);
        garrison.Soldier = 800;
        garrison.Directive = UnitDirective.Support;
        garrison.ActionTarget.StrongholdId = stronghold.Id;

        world.GameData.Units[11] = garrison;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, garrison);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        Assert.True(GarrisonBehaviorRules.TryDissolveGarrisonWhenSafe(
            worldContext, stronghold, ctx.World.GameData));

        Assert.Null(StrongholdGarrisonRules.FindGarrisonUnit(stronghold, ctx.World.GameData));
        Assert.Equal(800, stronghold.ForceActor.Soldier);
        Assert.False(ctx.World.GameData.Units.ContainsKey(11));
    }

    [Fact]
    public void TryDispatchLordRelief_SendsIdleUnitFromResidence()
    {
        var residence = new Point3(0, 0);
        var frontier = new Point3(5, 0);
        var enemyTile = new Point3(4, 1);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);

        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var residenceSh = StrategyTestWorldBuilder.CreateTestStronghold(1, 1, residence);
        var frontierSh = StrategyTestWorldBuilder.CreateTestStronghold(2, 1, frontier);
        var lord = new Character
        {
            Id = 100,
            Name = "当主",
            Description = "测试当主",
            Portrait = "",
            Personality = new PersonalityData(),
            Proficiency = new ProficiencyData
            {
                Infantry = 1,
                Ride = 1,
                Archery = 1,
                Firelock = 1,
                Sealing = 1,
                Military = 1,
                Fighting = 1,
                Spy = 1,
                Agriculture = 1,
                Commerce = 1,
                Construct = 1,
                Smelt = 1,
                Eloquence = 1,
                Court = 1,
                Sociality = 1,
                Healing = 1
            },
            ActionTarget = new Character.CharacterActionTarget
            {
                RoutePoints = new Queue<Point2>()
            },
            ForceId = 1,
            StrongholdId = residenceSh.Id
        };

        var relief = StrategyTestWorldBuilder.CreateTestUnit(10, 1, residence);
        relief.Soldier = 1200;
        relief.Directive = UnitDirective.Move;
        relief.Status = UnitStatus.Waiting;
        relief.ActionTarget.RoutePoints.Clear();

        var enemy = StrategyTestWorldBuilder.CreateTestUnit(20, 2, enemyTile);
        enemy.Soldier = 1800;
        enemy.Directive = UnitDirective.Occupy;

        world.GameData.Characters[100] = lord;
        world.GameData.Units[10] = relief;
        world.GameData.Units[20] = enemy;
        world.GameData.Strongholds[1] = residenceSh;
        world.GameData.Strongholds[2] = frontierSh;
        world.GameData.Forces[2] = forceB;
        MapLocationActions.RegisterUnit(world, relief);
        MapLocationActions.RegisterUnit(world, enemy);
        MapLocationActions.RegisterStronghold(world, residenceSh);
        MapLocationActions.RegisterStronghold(world, frontierSh);

        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = 2,
            ForceLordCharacterIds = new Dictionary<int, int> { [1] = 100 }
        };

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();

        Assert.True(StrategyUnitAIRules.TryDispatchLordRelief(
            1, ctx.World.GameData, meta, pathfinding, worldContext));

        Assert.Equal(UnitDirective.Support, relief.Directive);
        Assert.Equal(frontierSh.Id, relief.ActionTarget.StrongholdId);
        Assert.Equal(UnitStatus.Moving, relief.Status);
        Assert.NotEmpty(relief.ActionTarget.RoutePoints);
    }

    [Fact]
    public void TryDispatchLordRelief_SkipsPlayerForce()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var pathfinding = ctx.Services.GetRequiredService<Domain.Services.Pathfinding.IPathfindingService>();
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var meta = new StrategyScenarioMeta { PlayerForceId = 1 };

        Assert.False(StrategyUnitAIRules.TryDispatchLordRelief(
            1, ctx.World.GameData, meta, pathfinding, worldContext));
    }

    [Fact]
    public void SupportUnit_CannotStepIntoHostileMilitaryTile()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var evaluator = ctx.Services.GetRequiredService<Domain.Evaluators.UnitMoveEvaluator>();

        var support = StrategyTestWorldBuilder.CreateTestUnit(10, 1, new Point3(0, 0));
        support.Soldier = 1200;
        support.Directive = UnitDirective.Support;
        support.Status = UnitStatus.Moving;
        support.Ap = 10;
        support.ActionTarget.RoutePoints.Enqueue(new Point2(1, 0));

        var enemy = StrategyTestWorldBuilder.CreateTestUnit(20, 2, new Point3(1, 0));
        enemy.Soldier = 3000;
        enemy.Directive = UnitDirective.Occupy;

        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(ctx.World.GameData.Forces[1], forceB);
        ctx.World.GameData.Units[10] = support;
        ctx.World.GameData.Units[20] = enemy;
        ctx.World.GameData.Forces[2] = forceB;
        MapLocationActions.RegisterUnit(ctx.World, support);
        MapLocationActions.RegisterUnit(ctx.World, enemy);

        var result = evaluator.Evaluate(support, new Point2(1, 0));
        Assert.False(result.IsSuccess);
        Assert.Equal(nameof(GameError.MovementError.CannotMoveToTile), result.Error?.Code);
    }
}
