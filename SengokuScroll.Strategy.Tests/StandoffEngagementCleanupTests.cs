using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>对峙僵局清理与 AI 脱困。</summary>
public class StandoffEngagementCleanupTests
{
    [Fact]
    public void PruneOrphanEngagements_ClearsStandoffWhenOpponentLeftTile()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var a = world.GameData.Units[1];
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        world.GameData.Forces[2] = forceB;
        var b = StrategyTestWorldBuilder.CreateTestUnit(2, 2, a.Location);
        world.GameData.Units[2] = b;
        StrategyTestWorldBuilder.LinkEnemyForces(world.GameData.Forces[1], forceB);

        BattlefieldEngagementRules.EnterBattlefield(a, b.Id);
        BattlefieldEngagementRules.EnterBattlefield(b, a.Id);
        b.Location = new(9, 9);

        var registry = new StrategyFieldEngagementRegistry();
        registry.SetStandoffDays(a.Id, b.Id, 5);
        registry.PruneOrphanEngagements(world.GameData);

        Assert.Equal(UnitStatus.Waiting, a.Status);
        Assert.Equal(UnitStatus.Waiting, b.Status);
        Assert.Equal(0, a.ActionTarget.UnitId);
        Assert.Equal(0, registry.GetStandoffDays(a.Id, b.Id));
    }

    [Fact]
    public void TryResolveStandoffEngagement_LongStandoffLowWinRate_SwitchesToRetreat()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        var a = world.GameData.Units[1];
        a.Soldier = 500;
        a.Morale = 60;
        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        world.GameData.Forces[2] = forceB;
        var b = StrategyTestWorldBuilder.CreateTestUnit(2, 2, a.Location);
        b.Soldier = 5000;
        b.Morale = 90;
        world.GameData.Units[2] = b;
        StrategyTestWorldBuilder.LinkEnemyForces(world.GameData.Forces[1], forceB);

        BattlefieldEngagementRules.EnterBattlefield(a, b.Id);

        var registry = new StrategyFieldEngagementRegistry();
        registry.SetStandoffDays(a.Id, b.Id, BattleConstants.AiStandoffBreakRetreatDays);

        var decision = StrategyUnitAIRules.TryResolveStandoffEngagement(
            a,
            world.GameData,
            registry,
            world.GameMapMasterData);

        Assert.NotNull(decision);
        Assert.Equal("StandoffBreakRetreat", decision!.Value.Code);
        Assert.Equal(UnitDirective.Retreat, a.Directive);
        Assert.Equal(UnitStatus.Waiting, a.Status);
        Assert.Equal(0, registry.GetStandoffDays(a.Id, b.Id));
    }
}
