using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>劝降战后：降方离场、胜方站城自动强攻。</summary>
public class BattleSurrenderAftermathTests
{
    [Fact]
    public void ApplySurrender_RemovesLoserFromMap_AndArmsAssaultOnEnemyStronghold()
    {
        var castle = new Point3(5, 4);
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Units.Remove(1);

        var forceA = world.GameData.Forces[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(forceA, forceB);

        var stronghold = StrategyTestWorldBuilder.CreateTestStronghold(10, 2, castle);
        stronghold.ForceActor.Soldier = 0;
        stronghold.ForceActor.Morale = 0;

        var winner = StrategyTestWorldBuilder.CreateTestUnit(1, 1, castle);
        winner.Soldier = 2000;
        winner.Directive = UnitDirective.Occupy;

        var loser = StrategyTestWorldBuilder.CreateTestUnit(2, 2, castle);
        loser.Soldier = 64;
        loser.Morale = 0;
        loser.Name = "三河凑守军";
        loser.Directive = UnitDirective.Support;

        world.GameData.Forces[2] = forceB;
        world.GameData.Units[1] = winner;
        world.GameData.Units[2] = loser;
        world.GameData.Strongholds[10] = stronghold;
        MapLocationActions.RegisterUnit(world, winner);
        MapLocationActions.RegisterUnit(world, loser);
        MapLocationActions.RegisterStronghold(world, stronghold);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var helper = ctx.Services.GetRequiredService<BattleAftermathHelper>();

        helper.ApplySurrender(winner, loser, BattleEngagementKind.Siege);

        Assert.False(ctx.World.GameData.Units.ContainsKey(2));
        Assert.True(winner.Soldier > 2000);
        Assert.Equal(UnitSiegeMode.None, winner.SiegeMode);
        Assert.Equal(0, winner.ActionTarget.StrongholdId);
        Assert.Equal(10, winner.DirectiveTargetId);
        Assert.Equal(UnitDirective.Occupy, winner.Directive);
        Assert.Equal(1, stronghold.ForceId);
        Assert.Equal(0, winner.BattlefieldId);
    }
}
