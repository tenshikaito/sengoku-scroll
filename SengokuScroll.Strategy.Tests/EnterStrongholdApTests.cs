using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>进入非己方据点 AP 消耗（默认 +1，应可在 AP 上限内入城）。</summary>
public class EnterStrongholdApTests
{
    [Fact]
    public void DefaultConfig_EnterEnemyStrongholdTile_CostsTerrainPlusOne()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var rules = new MovementRules(ctx.Services.GetRequiredService<IGameContext>());
        var config = ctx.Services.GetRequiredService<GameRuleConfig>();

        Assert.Equal(1, config.EnterStrongholdAp);

        var castle = new Point3(5, 4);
        ctx.World.GameData.Strongholds[99] = StrategyTestWorldBuilder.CreateTestStronghold(99, 2, castle);
        MapLocationActions.RegisterStronghold(ctx.World, ctx.World.GameData.Strongholds[99]);

        var unit = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new Point3(4, 4));
        unit.Ap = 5;
        ctx.World.GameData.Units[1] = unit;

        var forceB = StrategyTestWorldBuilder.CreateTestForce(2);
        StrategyTestWorldBuilder.LinkEnemyForces(ctx.World.GameData.Forces[1], forceB);
        ctx.World.GameData.Forces[2] = forceB;

        var cost = rules.GetTileMovementApCost(unit, castle);
        Assert.Equal(3, cost); // 平地 2 + 入城 1
        Assert.True(rules.CheckMoveToTileAp(unit, castle).IsSuccess);
    }
}
