using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class BattleEngagementResolverTests
{
    [Fact]
    public void ResolveRoles_SingleAttack_OrdererIsAttacker()
    {
        var a = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        var b = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        a.Movement = 3;
        b.Movement = 10;

        var (attacker, defender, both) = BattleEngagementResolver.ResolveRoles(a, b, true, false);

        Assert.Equal(1, attacker.Id);
        Assert.Equal(2, defender.Id);
        Assert.False(both);
    }

    [Fact]
    public void ResolveRoles_MutualAttack_HigherMovementIsAttacker()
    {
        var a = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        var b = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        a.Movement = 8;
        b.Movement = 3;

        var (attacker, defender, both) = BattleEngagementResolver.ResolveRoles(a, b, true, true);

        Assert.Equal(1, attacker.Id);
        Assert.Equal(2, defender.Id);
        Assert.True(both);
    }

    [Fact]
    public void ResolveRoles_MutualAttack_EqualMovement_LowerIdIsAttacker()
    {
        var a = StrategyTestWorldBuilder.CreateTestUnit(2, 1, new Point3(0, 0));
        var b = StrategyTestWorldBuilder.CreateTestUnit(1, 2, new Point3(1, 0));
        a.Movement = 5;
        b.Movement = 5;

        var (attacker, defender, both) = BattleEngagementResolver.ResolveRoles(a, b, true, true);

        Assert.Equal(1, attacker.Id);
        Assert.Equal(2, defender.Id);
        Assert.True(both);
    }
}

public class TacticalBattleSimulatorTests
{
    [Fact]
    public void Assemble_IncludesAdjacentAlliesOnly()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(5, 5));
        attacker.Soldier = 100;
        attacker.Movement = 6;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(6, 5));
        defender.Soldier = 80;
        defender.Movement = 4;
        var adjacentAlly = StrategyTestWorldBuilder.CreateTestUnit(3, 2, new Point3(7, 5)); // dist 1
        adjacentAlly.Soldier = 50;
        var distantAlly = StrategyTestWorldBuilder.CreateTestUnit(4, 2, new Point3(8, 5)); // dist 2
        distantAlly.Soldier = 50;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        world.GameData.Units[adjacentAlly.Id] = adjacentAlly;
        world.GameData.Units[distantAlly.Id] = distantAlly;

        var field = BattleBattlefieldAssembler.Assemble(attacker, defender, world.GameData);

        Assert.Contains(field.DefenderUnits, u => u.Id == defender.Id);
        Assert.Contains(field.DefenderUnits, u => u.Id == adjacentAlly.Id);
        Assert.DoesNotContain(field.DefenderUnits, u => u.Id == distantAlly.Id);
    }

    [Fact]
    public void DetectSurround_WhenFourAdjacentOccupiedByAttacker()
    {
        var defender = StrategyTestWorldBuilder.CreateTestUnit(10, 2, new Point3(5, 5));
        defender.Soldier = 100;
        var n = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(5, 4));
        var e = StrategyTestWorldBuilder.CreateTestUnit(2, 1, new Point3(6, 5));
        var s = StrategyTestWorldBuilder.CreateTestUnit(3, 1, new Point3(5, 6));
        var w = StrategyTestWorldBuilder.CreateTestUnit(4, 1, new Point3(4, 5));
        n.Soldier = e.Soldier = s.Soldier = w.Soldier = 40;

        var gameData = new GameData
        {
            GameDate = new GameDate(1, 1, 1),
            Forces = new Dictionary<int, Force>
            {
                [1] = StrategyTestWorldBuilder.CreateTestForce(1),
                [2] = StrategyTestWorldBuilder.CreateTestForce(2)
            },
            Strongholds = [],
            Units = new Dictionary<int, Unit>
            {
                [defender.Id] = defender,
                [n.Id] = n,
                [e.Id] = e,
                [s.Id] = s,
                [w.Id] = w
            },
            Characters = [],
            SupplyConvoys = [],
            MessageCarriers = [],
            SubUnits = []
        };

        Assert.True(BattleBattlefieldAssembler.DetectSurround(defender, attackerForceId: 1, gameData));
    }

    [Fact]
    public void Resolve_ProducesNarrativeLogWithoutFactorModifiers()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 120;
        attacker.Movement = 7;
        attacker.Attack = 12;
        attacker.Defense = 10;
        attacker.Morale = 70;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 100;
        defender.Movement = 5;
        defender.Attack = 10;
        defender.Defense = 12;
        defender.Morale = 70;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        AttachSubUnits(world, attacker, [(StrategyTroopTypes.Ashigaru, 70), (StrategyTroopTypes.Cavalry, 50)]);
        AttachSubUnits(world, defender, [(StrategyTroopTypes.Ashigaru, 60), (StrategyTroopTypes.Archer, 40)]);

        var result = TacticalBattleSimulator.Resolve(attacker, defender, world.GameData, seed: 42);

        Assert.NotEmpty(result.LogEntries);
        Assert.Contains(result.LogEntries, e => e.Phase == "展开");
        Assert.Contains(result.LogEntries, e => e.Phase == "将令");
        Assert.Contains(result.LogEntries, e => e.Phase == "交锋");
        Assert.DoesNotContain(result.LogEntries, e => e.Phase is "因素" or "修正");
        Assert.True(result.Outcome.AttackerCasualties > 0 || result.Outcome.DefenderCasualties > 0);
        Assert.InRange(result.Outcome.AttackerWinRatePercent, 5, 95);
    }

    [Fact]
    public void Resolve_SameSeed_IsDeterministic()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 80;
        attacker.Movement = 6;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 80;
        defender.Movement = 6;
        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);

        var a = TacticalBattleSimulator.Resolve(attacker, defender, world.GameData, 99);
        // reset soldiers
        attacker.Soldier = 80;
        defender.Soldier = 80;
        var b = TacticalBattleSimulator.Resolve(attacker, defender, world.GameData, 99);

        Assert.Equal(a.Outcome.AttackerWon, b.Outcome.AttackerWon);
        Assert.Equal(a.Outcome.AttackerCasualties, b.Outcome.AttackerCasualties);
        Assert.Equal(a.Outcome.DefenderCasualties, b.Outcome.DefenderCasualties);
        Assert.Equal(a.LogEntries.Count, b.LogEntries.Count);
    }

    [Fact]
    public void Resolve_AssignsFormationSlotsAndScoresTargets()
    {
        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        attacker.Soldier = 120;
        attacker.Movement = 7;
        attacker.Attack = 12;
        attacker.Defense = 10;
        attacker.Morale = 70;
        var defender = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new Point3(1, 0));
        defender.Soldier = 100;
        defender.Movement = 5;
        defender.Attack = 10;
        defender.Defense = 12;
        defender.Morale = 70;

        var world = StrategyTestWorldBuilder.BuildAdjacentBattleWorld(attacker, defender);
        AttachSubUnits(world, attacker, [(StrategyTroopTypes.Cavalry, 50), (StrategyTroopTypes.Ashigaru, 70)]);
        AttachSubUnits(world, defender, [(StrategyTroopTypes.Archer, 40), (StrategyTroopTypes.Ashigaru, 60)]);

        var result = TacticalBattleSimulator.Resolve(attacker, defender, world.GameData, seed: 7);

        Assert.Contains(result.LogEntries, e => e.Phase == "布阵");
        Assert.Contains(result.LogEntries, e => e.Phase == "将令");
        Assert.Contains(result.LogEntries, e => e.Message.Contains("前军") || e.Message.Contains("后军") || e.Message.Contains("左翼") || e.Message.Contains("右翼"));
        Assert.DoesNotContain(result.LogEntries, e => e.Phase is "因素" or "修正");
    }

    [Fact]
    public void TargetScoring_CavalryPrefersRanged()
    {
        var scoreArcher = BattleTargetScoring.Score(
            40, 8, StrategyTroopTypes.Archer, BattleFormationSlot.Rear, false,
            BattleFormationSlot.FlankLeft, StrategyTroopTypes.Cavalry, BattleCommanderActionKind.Assault);
        var scoreAshigaru = BattleTargetScoring.Score(
            40, 8, StrategyTroopTypes.Ashigaru, BattleFormationSlot.Front, false,
            BattleFormationSlot.FlankLeft, StrategyTroopTypes.Cavalry, BattleCommanderActionKind.Assault);

        Assert.True(scoreArcher > scoreAshigaru);
    }

    [Fact]
    public void Commander_RetreatDirective_ChoosesWithdraw()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        unit.Directive = UnitDirective.Retreat;
        unit.Morale = 60;
        var decision = BattleCommanderActionRules.Decide(
            null, unit, null, isAttacker: true, isSurrounded: false, estimatedWinRatePercent: 70, new Random(1));
        Assert.Equal(BattleCommanderActionKind.Withdraw, decision.Action);
    }

    [Fact]
    public void Commander_HeavyLosses_ReDecideToWithdraw()
    {
        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(0, 0));
        unit.Morale = 70;
        var decision = BattleCommanderActionRules.Decide(
            null, unit, null, isAttacker: true, isSurrounded: false, estimatedWinRatePercent: 60,
            new Random(1), ownRemainingRatio: 0.30, enemyRemainingRatio: 0.90,
            previous: BattleCommanderActionKind.Assault);
        Assert.Equal(BattleCommanderActionKind.Withdraw, decision.Action);
    }

    private static void AttachSubUnits(GameWorld world, Unit unit, (byte TypeId, int Soldiers)[] parts)
    {
        unit.SubUnitIds.Clear();
        unit.Soldier = 0;
        var nextId = world.GameData.SubUnits.Count == 0 ? 1 : world.GameData.SubUnits.Keys.Max() + 1;
        foreach (var (typeId, soldiers) in parts)
        {
            var sub = new SubUnit
            {
                Id = nextId++,
                UnitId = unit.Id,
                ForceId = unit.ForceId,
                TypeId = typeId,
                Soldier = soldiers,
                Attack = unit.Attack > 0 ? unit.Attack : 10,
                Defense = unit.Defense > 0 ? unit.Defense : 10,
                Movement = typeId == StrategyTroopTypes.Cavalry ? 8 : 4
            };
            world.GameData.SubUnits[sub.Id] = sub;
            unit.SubUnitIds.Add(sub.Id);
            unit.Soldier += soldiers;
        }
    }
}
