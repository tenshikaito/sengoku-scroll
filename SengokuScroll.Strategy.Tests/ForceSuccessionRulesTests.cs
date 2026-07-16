using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class ForceSuccessionRulesTests
{
    [Fact]
    public void HasActiveResistance_WithStrongholdOrUnit_ReturnsTrue()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var gameData = ctx.World.GameData;
        gameData.Units[1].Soldier = 500;

        Assert.True(ForceResistanceRules.HasActiveResistance(1, gameData));

        foreach (var sh in gameData.Strongholds.Values.ToList())
            gameData.Strongholds.Remove(sh.Id);

        foreach (var u in gameData.Units.Values.Where(u => u.ForceId == 1))
            u.Soldier = 0;

        Assert.False(ForceResistanceRules.HasActiveResistance(1, gameData));
    }

    [Fact]
    public void TryResolveAfterLordRemoved_KilledNoResistance_EliminatesForce()
    {
        var world = StrategyTestWorldBuilder.BuildMinimalWorld();
        world.GameData.Forces[2] = StrategyTestWorldBuilder.CreateTestForce(2);

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var gameData = ctx.World.GameData;
        var registry = ctx.Services.GetRequiredService<StrategyForceLordRegistry>();
        var events = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();

        foreach (var u in gameData.Units.Values.Where(u => u.ForceId == 2).ToList())
            gameData.Units.Remove(u.Id);

        var reason = ForceSuccessionRules.TryResolveAfterLordRemoved(
            2,
            conquerorForceId: 1,
            lordCaptured: false,
            lordKilled: true,
            gameData,
            ctx.World.GameMasterData,
            ctx.Services.GetRequiredService<Data.Models.StrategyScenarioMeta>(),
            registry,
            events);

        Assert.Equal(ForceSuccessionRules.LordRemovalReason.KilledNoResistance, reason);
        Assert.Contains(events.Events, e => e.Category == "ForceEliminated");
    }
}
