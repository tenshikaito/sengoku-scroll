using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class UnitDestructionRulesTests
{
    [Fact]
    public void ResolveAnnihilatedUnit_RemovesZeroSoldierUnitFromWorld()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var gameContext = ctx.Services.GetRequiredService<IGameContext>();

        var winner = StrategyTestWorldBuilder.CreateTestUnit(1, 1, new(2, 2));
        winner.Soldier = 2000;
        winner.Food = 10_000;

        var loser = StrategyTestWorldBuilder.CreateTestUnit(2, 2, new(3, 2));
        loser.Soldier = 0;
        loser.Food = 5000;
        loser.Money = 1000;

        ctx.World.GameData.Units[1] = winner;
        ctx.World.GameData.Units[2] = loser;
        MapLocationActions.RegisterUnit(ctx.World, winner);
        MapLocationActions.RegisterUnit(ctx.World, loser);

        var meta = ctx.Services.GetRequiredService<SengokuScroll.Strategy.Data.Models.StrategyScenarioMeta>();
        var outcome = UnitDestructionRules.ResolveAnnihilatedUnit(
            gameContext.GameWorldContext,
            loser,
            winner,
            ctx.World.GameData,
            meta,
            null);

        Assert.True(outcome.UnitRemoved);
        Assert.False(ctx.World.GameData.Units.ContainsKey(2));
        Assert.True(winner.Food > 10_000);
    }
}
