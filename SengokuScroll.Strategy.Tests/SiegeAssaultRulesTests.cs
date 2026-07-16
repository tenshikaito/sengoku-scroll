using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class SiegeAssaultRulesTests
{
    [Fact]
    public void CalculateDailyAttackerCasualties_HighDefense_IsSubstantial()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        attacker.Soldier = 2000;
        var casualties = SiegeAssaultRules.CalculateDailyAttackerCasualties(attacker, totalDefense: 80);

        Assert.True(casualties >= 100, "高城防强攻攻方应有明显日伤亡");
        Assert.True(casualties <= 200);
    }

    [Fact]
    public void CalculateDailyAttackerCasualties_LowDefense_IsModerate()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Common.Types.Point3(0, 0));
        attacker.Soldier = 2000;
        var casualties = SiegeAssaultRules.CalculateDailyAttackerCasualties(attacker, totalDefense: 10);

        Assert.InRange(casualties, 16, 40);
    }

    [Fact]
    public void ShouldWearDefenseFacilityToday_AfterSeveralDays_ReturnsTrue()
    {
        Assert.True(SiegeAssaultRules.ShouldWearDefenseFacilityToday(10, 50, 1500));
    }
}
