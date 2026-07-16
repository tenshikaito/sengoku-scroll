using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>城下野战击溃守军后，攻方应进入据点格继续攻城/占城。</summary>
public class GarrisonFieldBattleAftermathTests
{
    [Fact]
    public void FieldBattleWin_AgainstGarrisonOnTile_WinnerEntersStrongholdCell()
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
        attacker.Morale = 85;
        attacker.Directive = UnitDirective.Occupy;
        attacker.SiegeMode = UnitSiegeMode.Assault;
        attacker.ActionTarget.StrongholdId = stronghold.Id;

        var garrison = StrategyTestWorldBuilder.CreateTestUnit(11, 2, castle);
        garrison.Soldier = 800;
        garrison.Morale = 60;
        garrison.Directive = UnitDirective.Support;
        garrison.Stance = UnitStance.Hold;
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
        var outcome = new InstantBattleOutcome(
            AttackerWon: true,
            AttackerWinRatePercent: 70,
            AttackerCasualties: 200,
            DefenderCasualties: 800,
            ResolutionSeed: 1,
            ResolutionRoll: 10,
            AttackerSoldiersBefore: 2500,
            DefenderSoldiersBefore: 800);

        aftermath.Apply(attacker, garrison, outcome, BattleEngagementKind.Siege);

        Assert.True(attacker.Location.IsSameTile(castle));
        Assert.Equal(1, stronghold.ForceId);
    }
}
