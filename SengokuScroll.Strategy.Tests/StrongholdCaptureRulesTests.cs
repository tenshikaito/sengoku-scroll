using SengokuScroll.Common.Types;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

public class StrongholdCaptureRulesTests
{
    [Fact]
    public void CanTransferOwnership_RequiresSiegeOrderAndBrokenDefense()
    {
        var castle = new Point3(3, 3);
        using var ctx = StrategyTestWorldFactory.Create();

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        stronghold.ForceActor.Soldier = 1500;
        stronghold.ForceActor.Morale = 80;

        var attacker = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        attacker.Directive = UnitDirective.Occupy;
        attacker.Soldier = 2000;

        ctx.World.GameData.Strongholds[10] = stronghold;
        ctx.World.GameData.Units[1] = attacker;

        Assert.False(StrongholdCaptureRules.CanTransferOwnership(
            attacker, stronghold, ctx.World.GameData, out var reason1));
        Assert.Equal("no_siege_order", reason1);

        attacker.SiegeMode = UnitSiegeMode.Assault;
        attacker.ActionTarget.StrongholdId = stronghold.Id;

        Assert.False(StrongholdCaptureRules.CanTransferOwnership(
            attacker, stronghold, ctx.World.GameData, out var reason2));
        Assert.Equal("defense_intact", reason2);

        stronghold.ForceActor.Soldier = 0;
        stronghold.ForceActor.Morale = 80;

        Assert.True(StrongholdCaptureRules.IsStrongholdDefenseBroken(stronghold, ctx.World.GameData));
        Assert.True(StrongholdCaptureRules.CanTransferOwnership(
            attacker, stronghold, ctx.World.GameData, out _));

        stronghold.ForceActor.Soldier = 1500;
        stronghold.ForceActor.Morale = 0;

        Assert.True(StrongholdCaptureRules.CanTransferOwnership(
            attacker, stronghold, ctx.World.GameData, out _));
    }
}
