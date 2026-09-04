using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattleReportDifficultyAndFleeTests
{
    [Fact]
    public void DifficultyRules_Normal_DisallowsImmediateBattleReport()
    {
        Assert.False(GameStartOptions.ForDifficulty(StrategyDifficulty.Normal).InstantEventMessages);
        Assert.True(GameStartOptions.ForDifficulty(StrategyDifficulty.Easy).InstantEventMessages);
    }

    [Fact]
    public void ScenarioLoader_AssignsFixedSimulationSeed()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);

        Assert.Equal(StrategyDifficulty.Normal, loaded.Meta.Difficulty);
        Assert.Equal(15600101, loaded.World.GameData.SimulationSeed);
    }

    [Fact]
    public void ComputeResolutionSeed_IncludesSimulationSeed()
    {
        var date = new Domain.Types.GameDate(1560, 1, 1);
        var a = InstantBattleCalculator.ComputeResolutionSeed(100, date, 1, 2, 3, 4);
        var b = InstantBattleCalculator.ComputeResolutionSeed(200, date, 1, 2, 3, 4);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TryFleeAfterDefeat_AbsorbsIntoAdjacentFriendlyStronghold()
    {
        var castle = new Point3(5, 4);
        var field = new Point3(4, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        stronghold.ForceActor.Soldier = 500;

        var loser = StrategyTestWorldBuilder.CreateTestUnit(11, 1, field);
        loser.Soldier = 800;
        loser.Directive = UnitDirective.Occupy;

        var winner = StrategyTestWorldBuilder.CreateTestUnit(12, 2, new Point3(3, 4));
        winner.Soldier = 2000;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[11] = loser;
        world.GameData.Units[12] = winner;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, loser);
        MapLocationActions.RegisterUnit(world, winner);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();

        var refuge = BattleFleeToStrongholdRules.TryFleeAfterDefeat(
            worldContext, loser, winner, ctx.World.GameData, buffer);

        Assert.NotNull(refuge);
        Assert.Equal(10, refuge!.Id);
        Assert.Equal(1300, stronghold.ForceActor.Soldier);
        Assert.False(ctx.World.GameData.Units.ContainsKey(11));
        Assert.Contains(buffer.Events, e => e.Category == "UnitFledToStronghold");
    }

    [Fact]
    public void TryFleeAfterDefeat_SupportOnBesiegedTile_AbsorbsDespiteEnemyOccupier()
    {
        var castle = new Point3(5, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 1, castle);
        stronghold.ForceActor.Soldier = 200;

        var loser = StrategyTestWorldBuilder.CreateTestUnit(11, 1, castle);
        loser.Soldier = 400;
        loser.Directive = UnitDirective.Support;
        loser.ActionTarget.StrongholdId = stronghold.Id;

        var winner = StrategyTestWorldBuilder.CreateTestUnit(12, 2, castle);
        winner.Soldier = 2000;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[11] = loser;
        world.GameData.Units[12] = winner;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, loser);
        MapLocationActions.RegisterUnit(world, winner);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var worldContext = ctx.Services.GetRequiredService<IGameWorldContext>();
        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();

        var refuge = BattleFleeToStrongholdRules.TryFleeAfterDefeat(
            worldContext, loser, winner, ctx.World.GameData, buffer);

        Assert.NotNull(refuge);
        Assert.Equal(600, stronghold.ForceActor.Soldier);
        Assert.False(ctx.World.GameData.Units.ContainsKey(11));
    }

    [Fact]
    public void DeliverDecisiveBattleReport_Normal_DoesNotImmediateUnlock()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        var helper = ctx.Services.GetRequiredService<BattleReportDeliveryHelper>();
        var meta = ctx.Services.GetRequiredService<StrategyScenarioMeta>();

        Assert.Equal(StrategyDifficulty.Normal, meta.Difficulty);

        var attacker = loaded.World.GameData.Units.Values.First(u => u.ForceId == 1);
        var defender = loaded.World.GameData.Units.Values.First(u => u.ForceId == 2);
        defender.Location = new Point3(8, 8);

        var outcome = new InstantBattleOutcome
        {
            AttackerWon = true,
            AttackerSoldiersBefore = attacker.Soldier,
            DefenderSoldiersBefore = defender.Soldier,
            AttackerCasualties = 100,
            DefenderCasualties = 500,
            AttackerWinRatePercent = 70,
            ResolutionSeed = 42,
            ResolutionRoll = 10
        };

        var dto = new StrategyBattleResultDto
        {
            AttackerUnitId = attacker.Id,
            DefenderUnitId = defender.Id,
            AttackerName = attacker.Name,
            DefenderName = defender.Name,
            AttackerForceId = attacker.ForceId,
            DefenderForceId = defender.ForceId,
            AttackerWon = true,
            AttackerCasualties = 100,
            DefenderCasualties = 500,
            AttackerSoldiersBefore = attacker.Soldier,
            DefenderSoldiersBefore = defender.Soldier,
            AttackerSoldiersAfter = Math.Max(0, attacker.Soldier - 100),
            DefenderSoldiersAfter = Math.Max(0, defender.Soldier - 500),
            AttackerWinRatePercent = 70,
            ResolutionSeed = 42,
            ResolutionRoll = 10,
            IsSurrendered = false,
            EngagementKind = "FieldBattle",
            LogEntries = [],
            FactorNotes = [],
            AttackerReinforcementNames = [],
            DefenderReinforcementNames = []
        };

        helper.DeliverDecisiveBattleReport(
            attacker.ForceId,
            attacker.Location,
            loaded.World.GameData,
            outcome,
            attacker,
            defender,
            dto);

        Assert.DoesNotContain(
            buffer.Events,
            e => e.Category == "BattleReportArrived"
                 && e.Message.Contains("当日前线战报", StringComparison.Ordinal));
    }
}
