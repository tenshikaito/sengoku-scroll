using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Policies.UnitAi;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>战后重整：败方设 Retreat 方针，不强制后撤一格。</summary>
public class BattleAftermathRetreatTests
{
    [Fact]
    public void Apply_Loser_SetsRetreatButDoesNotAutoMove()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var helper = ctx.Services.GetRequiredService<BattleAftermathHelper>();

        var playerUnit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(2, 2));
        playerUnit.Soldier = 500;
        var start = playerUnit.Location;
        var enemyUnit = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(3, 2));
        enemyUnit.Soldier = 800;

        ctx.World.GameData.Units[1] = playerUnit;
        ctx.World.GameData.Units[2] = enemyUnit;

        var outcome = new InstantBattleOutcome
        {
            AttackerWon = true,
            AttackerSoldiersBefore = enemyUnit.Soldier,
            DefenderSoldiersBefore = playerUnit.Soldier,
            AttackerCasualties = 50,
            DefenderCasualties = 200,
            AttackerWinRatePercent = 70,
            ResolutionSeed = 1,
            ResolutionRoll = 10
        };

        helper.Apply(enemyUnit, playerUnit, outcome);

        Assert.Equal(UnitDirective.Retreat, playerUnit.Directive);
        Assert.Equal(start, playerUnit.Location);
        Assert.True(playerUnit.Status is UnitStatus.Waiting or UnitStatus.Fearful or UnitStatus.Routing);
        Assert.Empty(playerUnit.ActionTarget.RoutePoints);
    }

    [Fact]
    public void ApplyDefeatRetreat_ClearsSiegeOrderSoAiCanMarch()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var loser = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(3, 2));
        loser.Soldier = 500;
        loser.SiegeMode = UnitSiegeMode.Encircle;
        loser.ActionTarget.StrongholdId = 99;
        loser.Directive = UnitDirective.Occupy;
        loser.Stance = UnitStance.Surrounding;

        var winner = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(3, 2));
        winner.Soldier = 800;

        BattleRetreatRules.ApplyDefeatRetreat(loser, winner, commander: null, ctx.World.GameData);

        Assert.Equal(UnitDirective.Retreat, loser.Directive);
        Assert.Equal(UnitSiegeMode.None, loser.SiegeMode);
        Assert.Equal(0, loser.ActionTarget.StrongholdId);
        Assert.False(SiegeOrderRules.IsSiegeMovementLocked(loser));
        Assert.False(UnitAiSkipBehaviorRegistry.ShouldSkipDailyAi(loser));
    }

    [Fact]
    public void Apply_WinnerWithoutPursuitPersonality_KeepsOccupyDirective()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var helper = ctx.Services.GetRequiredService<BattleAftermathHelper>();

        var winner = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(2, 2));
        winner.Soldier = 800;
        winner.LeaderId = 0;
        var loser = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(3, 2));
        loser.Soldier = 500;

        ctx.World.GameData.Units[1] = winner;
        ctx.World.GameData.Units[2] = loser;

        var outcome = new InstantBattleOutcome
        {
            AttackerWon = true,
            AttackerSoldiersBefore = winner.Soldier,
            DefenderSoldiersBefore = loser.Soldier,
            AttackerCasualties = 50,
            DefenderCasualties = 200,
            AttackerWinRatePercent = 70,
            ResolutionSeed = 1,
            ResolutionRoll = 10
        };

        helper.Apply(winner, loser, outcome);

        Assert.Equal(UnitDirective.Occupy, winner.Directive);
        Assert.NotEqual(UnitDirective.Move, winner.Directive);
    }
}
