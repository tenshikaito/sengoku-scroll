using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>野战自动战斗：对峙 / 强袭分流。</summary>
public class FieldBattleAutoResolverTests
{
    [Fact]
    public void SmallArmy_ProbeDaysBeforeDecisive()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 2000;
        attacker.Morale = 70;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 2500;
        defender.Morale = 70;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var day1 = FieldBattleAutoResolver.ResolveDailyEngagement(
            world.GameData.GameDate,
            attacker,
            defender,
            standoffDaysBeforeToday: 0,
            world.GameData);

        Assert.Equal(FieldBattleAutoResolver.FieldBattleDayKind.Standoff, day1.Kind);

        var day3 = FieldBattleAutoResolver.ResolveDailyEngagement(
            world.GameData.GameDate,
            attacker,
            defender,
            standoffDaysBeforeToday: 2,
            world.GameData);

        Assert.Equal(FieldBattleAutoResolver.FieldBattleDayKind.Decisive, day3.Kind);
    }

    [Fact]
    public void LargeArmy_EqualForces_StandoffOnFirstDay()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 4000;
        attacker.Food = 100_000;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 4000;
        defender.Food = 100_000;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var result = FieldBattleAutoResolver.ResolveDailyEngagement(
            world.GameData.GameDate,
            attacker,
            defender,
            standoffDaysBeforeToday: 0,
            world.GameData);

        Assert.Equal(FieldBattleAutoResolver.FieldBattleDayKind.Standoff, result.Kind);
        Assert.Null(result.Outcome);
        Assert.Equal(1, result.StandoffDays);
    }

    [Fact]
    public void LargeArmy_ForcedCommit_OnDay30()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 4000;
        attacker.Food = 100_000;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 4000;
        defender.Food = 100_000;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var commit = BattleCommitRules.ResolveCommitSide(
            attacker,
            defender,
            BattleConstants.StandoffForceBattleDays,
            world.GameData);

        Assert.True(commit.ShouldCommit);
        Assert.NotNull(commit.Aggressor);
    }

    [Fact]
    public void LargeArmy_EnemyCutOff_TriggersCommit()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 4000;
        attacker.Food = 100_000;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 4000;
        defender.Food = 100_000;
        defender.Food = 0;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var adjusted = BattleCommitRules.ComputeAdjustedAttackerWinRatePercent(
            attacker,
            defender,
            world.GameData);

        Assert.True(adjusted >= BattleConstants.CommitAssaultWinRateThreshold);
    }
}
