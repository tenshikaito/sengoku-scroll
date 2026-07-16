using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattleDirectiveAndCompositionTests
{
    [Fact]
    public void FightToDeathDirective_BoostsDefenderPower()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 3000;
        attacker.Morale = 70;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 3000;
        defender.Morale = 70;
        defender.Stance = UnitStance.Hold;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        var ctx = new BattleEvaluationContext
        {
            Attacker = attacker,
            Defender = defender,
            GameData = world.GameData,
            Phase = BattleEvaluationPhase.Resolve
        };

        var breakdown = BattleFactorEvaluator.Evaluate(ctx);
        var holdNote = Assert.Single(breakdown.Notes, n => n.FactorId == "directive.fight_to_death");
        Assert.Equal(5, holdNote.DefenderWinRateDelta);
    }

    [Fact]
    public void Composition_CavalryStrongerOnPlain()
    {
        var plainScale = BattleCompositionCalculator.ResolveTroopTypeScale(
            StrategyTroopTypes.Cavalry,
            TerrainType.Plain);
        var mountainScale = BattleCompositionCalculator.ResolveTroopTypeScale(
            StrategyTroopTypes.Cavalry,
            TerrainType.Mountain);

        Assert.True(plainScale > mountainScale);
    }

    [Fact]
    public void Stratagem_DeceivedDefender_ForcesCommit()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 4000;
        attacker.Morale = 70;
        attacker.Food = 100_000;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 4000;
        defender.Morale = 70;
        defender.Food = 100_000;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        world.GameData.Messengers[1] = new Messenger
        {
            Id = 1,
            Name = "假报",
            ForceId = 2,
            LeaderId = 0,
            Location = defender.Location,
            SourceStrongholdId = 0,
            TargetUnitId = defender.Id,
            CourierCount = 1,
            EscortSoldierCount = 0,
            PayloadType = MessengerPayloadType.FalseIntelligence,
            Status = MessengerStatus.Arrived,
            RoutePoints = new Queue<Point3>()
        };

        var commit = BattleCommitRules.ResolveCommitSide(
            attacker,
            defender,
            standoffDays: 1,
            world.GameData);

        Assert.True(commit.ShouldCommit);
        Assert.True(commit.Breakdown!.ForceCommit);
    }
}
