using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>主动出城的守城单位战败后，残部应撤回城内而非整建制歼灭。</summary>
public class GarrisonDefeatRetreatTests
{
    [Fact]
    public void FieldGarrisonDefeat_AbsorbsResidualIntoCity_NotAnnihilated()
    {
        var castle = new Point3(5, 4);
        var adjacent = new Point3(4, 4);

        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);
        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        stronghold.ForceActor.Soldier = 0;

        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, adjacent);
        attacker.Soldier = 2500;

        var garrison = StrategyTestWorldBuilder.CreateTestUnit(11, 2, castle);
        garrison.Soldier = 800;
        garrison.Directive = UnitDirective.Support;
        garrison.ActionTarget.StrongholdId = stronghold.Id;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[1] = attacker;
        world.GameData.Units[11] = garrison;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, attacker);
        MapLocationActions.RegisterUnit(world, garrison);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var aftermath = ctx.Services.GetRequiredService<BattleAftermathHelper>();

        garrison.Soldier = 0;
        var outcome = BattleCasualtyRules.CapOutcome(
            new InstantBattleOutcome(
                AttackerWon: true,
                AttackerWinRatePercent: 70,
                AttackerCasualties: 200,
                DefenderCasualties: 800,
                ResolutionSeed: 42,
                ResolutionRoll: 10,
                AttackerSoldiersBefore: 2500,
                DefenderSoldiersBefore: 800),
            StrategyDifficulty.Normal);

        aftermath.Apply(attacker, garrison, outcome, BattleEngagementKind.Siege);

        Assert.DoesNotContain(world.GameData.Units.Values, u => u.Id == 11);
        Assert.True(stronghold.ForceActor.Soldier >= 400, "败退残部应至少保留约 50% 撤回城内");
        Assert.True(stronghold.ForceActor.Soldier < 800);
    }
}
