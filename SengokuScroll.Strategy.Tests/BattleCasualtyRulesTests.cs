using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Tests;

/// <summary>野战伤亡应反映「打散建制」而非歼灭战，尤其兵力悬殊时。</summary>
public class BattleCasualtyRulesTests
{
    [Fact]
    public void CapOutcome_2000vs800_AttackerWin_DefenderNotAnnihilated()
    {
        var raw = new InstantBattleOutcome(
            AttackerWon: true,
            AttackerWinRatePercent: 72,
            AttackerCasualties: 400,
            DefenderCasualties: 800,
            ResolutionSeed: 1,
            ResolutionRoll: 10,
            AttackerSoldiersBefore: 2000,
            DefenderSoldiersBefore: 800);

        var capped = BattleCasualtyRules.CapOutcome(raw, StrategyDifficulty.Normal);

        Assert.True(capped.DefenderCasualties <= 240, "2.5 倍兵力悬殊时败方伤亡应 ≤30%");
        Assert.True(capped.DefenderCasualties >= 100);
        Assert.True(capped.AttackerCasualties <= 200, "胜方碾压时伤亡应 ≤10%");
    }

    [Fact]
    public void CapOutcome_RespectsMinSurvivorFloor()
    {
        var raw = new InstantBattleOutcome(
            AttackerWon: true,
            AttackerWinRatePercent: 55,
            AttackerCasualties: 50,
            DefenderCasualties: 500,
            ResolutionSeed: 1,
            ResolutionRoll: 10,
            AttackerSoldiersBefore: 900,
            DefenderSoldiersBefore: 900);

        var capped = BattleCasualtyRules.CapOutcome(raw, StrategyDifficulty.Normal);

        Assert.True(capped.DefenderCasualties <= 450, "标准难度败方至少保留 50% 残部");
        Assert.True(capped.DefenderCasualties >= 200);
    }

    [Fact]
    public void DefeatResidualSoldierRatio_MatchesSurvivorFloor()
    {
        Assert.Equal(0.50, StrategyDifficultyRules.DefeatResidualSoldierRatio(StrategyDifficulty.Normal));
    }
}
