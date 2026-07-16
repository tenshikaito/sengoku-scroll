using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>攻城自动战斗：接敌分类、决战与据点占领。</summary>
public class SiegeAutoBattleTests
{
    [Fact]
    public void Apply_SiegeGarrisonBroken_TransfersStrongholdAndMovesWinnerIn()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var helper = ctx.Services.GetRequiredService<BattleAftermathHelper>();

        var castle = new Point3(5, 4);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        stronghold.Defense = 55;

        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        attacker.Soldier = 2000;
        attacker.Directive = UnitDirective.Occupy;
        attacker.SiegeMode = UnitSiegeMode.Assault;
        attacker.ActionTarget.StrongholdId = stronghold.Id;

        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        defender.Soldier = 0;
        defender.Morale = 0;
        defender.Directive = UnitDirective.Support;

        ctx.World.GameData.Units[1] = attacker;
        ctx.World.GameData.Units[2] = defender;
        ctx.World.GameData.Strongholds[10] = stronghold;

        var outcome = new InstantBattleOutcome
        {
            AttackerWon = true,
            AttackerSoldiersBefore = attacker.Soldier,
            DefenderSoldiersBefore = 800,
            AttackerCasualties = 200,
            DefenderCasualties = 800,
            AttackerWinRatePercent = 68,
            ResolutionSeed = 99,
            ResolutionRoll = 10
        };

        helper.Apply(attacker, defender, outcome, BattleEngagementKind.Siege);

        Assert.Equal(1, stronghold.ForceId);
        Assert.Equal(0, stronghold.LordId);
        Assert.Equal(castle, attacker.Location);
        Assert.Equal(UnitDirective.Occupy, attacker.Directive);
        Assert.Equal(0, attacker.ActionTarget.StrongholdId);
        Assert.Equal(10, attacker.DirectiveTargetId);
    }

    [Fact]
    public void ResolveDailyEngagement_SiegeOccupyAttackerInsideCity_CommitsAfterProbe()
    {
        var castle = new Point3(5, 4);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        attacker.Soldier = 2500;
        attacker.Directive = UnitDirective.Occupy;
        attacker.Morale = 85;

        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        defender.Soldier = 1200;
        defender.Morale = 60;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        world.GameData.Units[1] = attacker;
        world.GameData.Units[2] = defender;
        world.GameData.Strongholds[10] = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);

        var date = world.GameData.GameDate;
        var day1 = FieldBattleAutoResolver.ResolveDailyEngagement(
            date, attacker, defender, standoffDaysBeforeToday: 0, world.GameData);
        Assert.Equal(BattleEngagementKind.Siege, day1.EngagementKind);

        var day3 = FieldBattleAutoResolver.ResolveDailyEngagement(
            date, attacker, defender, standoffDaysBeforeToday: 2, world.GameData);
        Assert.Equal(FieldBattleAutoResolver.FieldBattleDayKind.Decisive, day3.Kind);
        Assert.Equal(BattleEngagementKind.Siege, day3.EngagementKind);
        Assert.NotNull(day3.Outcome);
    }

    [Fact]
    public void TryCaptureVacantStronghold_RequiresAssaultSiegeOrder()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var captureHelper = ctx.Services.GetRequiredService<StrongholdCaptureHelper>();

        var castle = new Point3(3, 3);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        attacker.Directive = UnitDirective.Occupy;
        attacker.Soldier = 500;

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        ctx.World.GameData.Units[1] = attacker;
        ctx.World.GameData.Strongholds[10] = stronghold;

        Assert.False(captureHelper.TryCaptureVacantStronghold(attacker, ctx.World.GameData));

        attacker.SiegeMode = UnitSiegeMode.Assault;
        attacker.ActionTarget.StrongholdId = stronghold.Id;

        Assert.True(captureHelper.TryCaptureVacantStronghold(attacker, ctx.World.GameData));
        Assert.Equal(1, stronghold.ForceId);
    }

    [Fact]
    public void CaptureStronghold_ClosesBattlefieldAndClearsSiegeMode()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var captureHelper = ctx.Services.GetRequiredService<StrongholdCaptureHelper>();
        var gameData = ctx.World.GameData;

        var castle = new Point3(4, 4);
        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        stronghold.ForceActor.Morale = 0;
        stronghold.ForceActor.Soldier = 0;

        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        attacker.Soldier = 2000;
        attacker.Directive = UnitDirective.Occupy;
        attacker.SiegeMode = UnitSiegeMode.Assault;
        attacker.ActionTarget.StrongholdId = stronghold.Id;

        var encircler = StrategyTestWorldBuilder.CreateTestUnit(3, 1, new Point3(3, 4));
        encircler.Soldier = 1500;
        encircler.Directive = UnitDirective.Occupy;
        encircler.SiegeMode = UnitSiegeMode.Encircle;
        encircler.Stance = UnitStance.Surrounding;
        encircler.ActionTarget.StrongholdId = stronghold.Id;

        gameData.Units[1] = attacker;
        gameData.Units[3] = encircler;
        gameData.Strongholds[10] = stronghold;

        var battlefield = BattlefieldContainerRules.EnsureSiegeBattlefield(attacker, stronghold, gameData);
        Assert.False(battlefield.IsClosed);
        Assert.Equal(battlefield.Id, attacker.BattlefieldId);

        Assert.True(captureHelper.CaptureStronghold(attacker, stronghold, 2, gameData));

        Assert.Equal(1, stronghold.ForceId);
        Assert.Equal(UnitDirective.Occupy, attacker.Directive);
        Assert.Equal(0, attacker.ActionTarget.StrongholdId);
        Assert.Equal(10, attacker.DirectiveTargetId);
        Assert.Equal(0, attacker.BattlefieldId);
        Assert.Equal(UnitSiegeMode.None, encircler.SiegeMode);
        Assert.True(battlefield.IsClosed);
        Assert.DoesNotContain(
            gameData.Battlefields.Values,
            bf => !bf.IsClosed && bf.Location.X == castle.X && bf.Location.Y == castle.Y);
    }

    [Fact]
    public void ShouldSkipDailyAi_WhenSiegeOrderActive()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(1, 1));
        unit.Soldier = 500;
        unit.SiegeMode = UnitSiegeMode.Encircle;
        unit.Stance = UnitStance.Surrounding;
        unit.Status = UnitStatus.Waiting;

        Assert.True(StrategyUnitAIRules.ShouldSkipDailyAi(unit));
    }
}
