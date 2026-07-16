using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattleFactorEvaluatorTests
{
    [Fact]
    public void LowMorale_BlocksEngage()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        unit.Morale = (byte)(BattleConstants.LowMoraleEngageThreshold - 1);

        Assert.False(BattleFactorEvaluator.CanUnitEngage(unit));
    }

    [Fact]
    public void ApplyBattleOutcome_WinnerMoraleRises_LoserFalls()
    {
        var winner = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        winner.Morale = 60;
        var loser = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Common.Types.Point3(1, 0));
        loser.Soldier = 100;
        loser.Morale = 40;

        BattleMoraleRules.ApplyBattleOutcome(winner, loser, decisiveVictory: true);

        Assert.True(winner.Morale > 60);
        Assert.True(loser.Morale < 60);
        Assert.Equal(UnitDirective.Retreat, loser.Directive);
    }
}
