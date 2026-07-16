using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattleSurrenderAndThreatAiTests
{
    [Fact]
    public void ScoreNearbyThreat_AdjacentStrongEnemy_OutranksDistantWeakEnemy()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var self = loaded.World.GameData.Units[1];
        self.Location = new Point3(4, 4);
        self.Soldier = 2000;

        var nearStrong = loaded.World.GameData.Units[2];
        nearStrong.Location = new Point3(5, 4);
        nearStrong.Soldier = 2800;
        nearStrong.Directive = UnitDirective.Occupy;
        nearStrong.Stance = UnitStance.Attacking;
        nearStrong.ActionTarget.UnitId = self.Id;

        var farWeak = StrategyTestWorldBuilder.CreateTestUnit(30, 2, new Point3(8, 8));
        farWeak.Soldier = 400;
        farWeak.Morale = 70;
        loaded.World.GameData.Units[30] = farWeak;

        var nearScore = BattleEngagementScorer.ScoreNearbyThreat(self, nearStrong, loaded.World.GameData);
        var farScore = BattleEngagementScorer.ScoreNearbyThreat(self, farWeak, loaded.World.GameData);

        Assert.True(nearScore > farScore, $"near={nearScore} far={farScore}");
    }

    [Fact]
    public void ShouldOfferSurrender_AbsoluteAdvantage_ReturnsTrue()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var aggressor = loaded.World.GameData.Units[1];
        var defender = loaded.World.GameData.Units[2];

        aggressor.Soldier = 8000;
        aggressor.Morale = 90;
        aggressor.Directive = UnitDirective.Occupy;
        defender.Soldier = 800;
        defender.Morale = 25;
        defender.Directive = UnitDirective.Retreat;
        defender.Status = UnitStatus.Fearful;

        aggressor.Location = new Point3(4, 4);
        defender.Location = new Point3(5, 4);

        var offered = BattleSurrenderRules.ShouldOfferSurrender(
            aggressor,
            defender,
            loaded.World.GameData,
            loaded.World.GameMapMasterData,
            standoffDays: 5,
            out var acceptChance,
            out var reason);

        Assert.True(offered, reason);
        Assert.True(acceptChance >= BattleConstants.SurrenderMinAcceptChanceToOffer);
    }

    [Fact]
    public void ResolveDailyEngagement_AbsoluteAdvantage_CanSurrender()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var aggressor = loaded.World.GameData.Units[1];
        var defender = loaded.World.GameData.Units[2];

        aggressor.Soldier = 9000;
        aggressor.Morale = 95;
        aggressor.Directive = UnitDirective.Occupy;
        defender.Soldier = 600;
        defender.Morale = 20;
        defender.Directive = UnitDirective.Retreat;
        defender.Status = UnitStatus.Fearful;
        aggressor.Location = new Point3(4, 4);
        defender.Location = new Point3(5, 4);

        // 多日尝试：劝降掷骰依赖种子，推进日期直到成功或确认可劝降
        var offered = BattleSurrenderRules.ShouldOfferSurrender(
            aggressor, defender, loaded.World.GameData, loaded.World.GameMapMasterData, 8,
            out _, out _);
        Assert.True(offered);

        FieldBattleAutoResolver.FieldBattleDayResult? surrender = null;
        for (var day = 1; day <= 40; day++)
        {
            var date = new Domain.Types.GameDate(1560, 1, day);
            var result = FieldBattleAutoResolver.ResolveDailyEngagement(
                date,
                aggressor,
                defender,
                standoffDaysBeforeToday: day,
                loaded.World.GameData,
                loaded.World.GameMapMasterData);

            if (result.Kind == FieldBattleAutoResolver.FieldBattleDayKind.Surrender)
            {
                surrender = result;
                break;
            }
        }

        Assert.NotNull(surrender);
        var outcome = surrender!.Value.Outcome;
        Assert.NotNull(outcome);
        Assert.True(outcome!.Value.IsSurrendered);
        Assert.Equal(0, outcome.Value.AttackerCasualties);
        Assert.Equal(0, outcome.Value.DefenderCasualties);
    }
}
