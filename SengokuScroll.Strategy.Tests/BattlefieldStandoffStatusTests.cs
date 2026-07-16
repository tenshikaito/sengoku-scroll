using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattlefieldStandoffStatusTests
{
    [Fact]
    public void EnterBattlefield_SetsStandoffStatusAndTarget()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new(2, 2));
        BattlefieldEngagementRules.EnterBattlefield(unit, 9);

        Assert.Equal(UnitStatus.Standoff, unit.Status);
        Assert.Equal(9, unit.ActionTarget.UnitId);
        Assert.Equal(UnitStance.Attacking, unit.Stance);
    }

    [Fact]
    public void MaintainStandoff_KeepsStatusUntilLeave()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new(2, 2));
        BattlefieldEngagementRules.EnterBattlefield(unit, 9);
        BattlefieldEngagementRules.MaintainStandoff(unit, 9);

        Assert.Equal(UnitStatus.Standoff, unit.Status);
        Assert.Equal(9, unit.ActionTarget.UnitId);

        BattlefieldEngagementRules.LeaveBattlefield(unit);

        Assert.Equal(UnitStatus.Waiting, unit.Status);
        Assert.Equal(0, unit.ActionTarget.UnitId);
        Assert.Equal(UnitStance.Normal, unit.Stance);
    }
}
