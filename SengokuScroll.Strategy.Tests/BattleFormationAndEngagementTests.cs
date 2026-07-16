using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattleFormationAndEngagementTests
{
    [Fact]
    public void CraneWingFormation_BoostsAttackerWinRate()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 3000;
        attacker.Morale = 70;
        attacker.FormationId = BattleFormationRules.CraneWing;

        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 3000;
        defender.Morale = 70;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var ctx = new BattleEvaluationContext
        {
            Attacker = attacker,
            Defender = defender,
            GameData = world.GameData,
            Phase = BattleEvaluationPhase.Resolve,
            EngagementKind = BattleEngagementKind.FieldBattle
        };

        var breakdown = BattleFactorEvaluator.Evaluate(ctx);
        var note = Assert.Single(breakdown.Notes, n => n.FactorId == "formation");
        Assert.Equal(3, note.AttackerWinRateDelta);
        Assert.Equal("鹤翼阵", note.Label);
    }

    [Fact]
    public void EngagementClassifier_Siege_WhenAttackerInsideDefenderStronghold()
    {
        var strongholdLocation = new Point3(1, 0);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, strongholdLocation);
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, strongholdLocation);

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        world.GameData.Strongholds[1] = StrategyTestWorldBuilder.CreateTestStronghold(1, 2, strongholdLocation);

        var kind = BattleEngagementClassifier.Classify(attacker, defender, world.GameData);

        Assert.Equal(BattleEngagementKind.Siege, kind);
    }

    [Fact]
    public void EngagementClassifier_Siege_WhenDefenderGarrisonOnStrongholdAndAttackerOutside()
    {
        var strongholdLocation = new Point3(1, 0);
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Directive = UnitDirective.Occupy;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, strongholdLocation);
        defender.Directive = UnitDirective.Support;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        world.GameData.Strongholds[1] = StrategyTestWorldBuilder.CreateTestStronghold(1, 2, strongholdLocation);

        var kind = BattleEngagementClassifier.Classify(attacker, defender, world.GameData);

        Assert.Equal(BattleEngagementKind.Siege, kind);
    }

    [Fact]
    public void EngagementClassifier_Ambush_WhenAttackerAmbushing()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Status = UnitStatus.Ambushing;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var kind = BattleEngagementClassifier.Classify(attacker, defender, world.GameData);

        Assert.Equal(BattleEngagementKind.Ambush, kind);
    }

    [Fact]
    public void FactorMapper_ToFactorNotes_IsNonEmptyForTypicalBattle()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 4000;
        attacker.Morale = 70;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 4000;
        defender.Morale = 70;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var ctx = new BattleEvaluationContext
        {
            Attacker = attacker,
            Defender = defender,
            GameData = world.GameData,
            Phase = BattleEvaluationPhase.Resolve,
            EngagementKind = BattleEngagementKind.FieldBattle
        };

        var breakdown = BattleFactorEvaluator.Evaluate(ctx);
        var notes = BattleFactorMapper.ToFactorNotes(breakdown);

        Assert.NotEmpty(notes);
        Assert.All(notes, n => Assert.False(string.IsNullOrWhiteSpace(n.Label)));
    }
}
