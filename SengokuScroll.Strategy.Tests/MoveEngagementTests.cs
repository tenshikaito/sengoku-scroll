using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>移动接敌与 AI 接敌集成测试（同格开战）。</summary>
public class MoveEngagementTests
{
    [Fact]
    public void AdvanceDay_SameTileEnemyWithOccupy_ResolvesBattle()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        IsolateEnemyUnitsForMarchTest(ctx.World);
        var player = ctx.World.GameData.Units[1];
        TeleportUnit(ctx.World, ctx.World.GameData.Units[2], player.Location);

        var enemy = ctx.World.GameData.Units[2];
        enemy.Directive = UnitDirective.Occupy;

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var buffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        Assert.True(
            buffer.ResolvedBattles.Count > 0
            || ctx.World.GameData.MessageCarriers.Values.Any(m => m.Payload.Type == MessagePayloadType.BattleReport)
            || enemy.BattlefieldId > 0
            || player.BattlefieldId > 0
            || player.Stance == UnitStance.Attacking
            || enemy.Stance == UnitStance.Attacking);
    }

    [Fact]
    public void AdvanceDay_AiEnemySameTileAsPlayer_CreatesBattlefield()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        IsolateEnemyUnitsForMarchTest(ctx.World);
        var player = ctx.World.GameData.Units[1];
        TeleportUnit(ctx.World, ctx.World.GameData.Units[2], player.Location);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        var enemy = ctx.World.GameData.Units[2];
        Assert.True(
            enemy.BattlefieldId > 0
            || player.BattlefieldId > 0
            || ctx.World.GameData.Battlefields.Values.Any(b => !b.IsClosed)
            || ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>().ResolvedBattles.Count > 0);
    }

    private static void IsolateEnemyUnitsForMarchTest(GameWorld world)
    {
        // Hold commander personality fixed so this test isolates contact handling.
        foreach (var character in world.GameData.Characters.Values)
        { character.Personality.Action = 50; character.Personality.Courage = 50; character.Personality.Ambition = 50; }
        foreach (var unit in world.GameData.Units.Values.Where(u => u.ForceId != 1).ToList())
            TeleportUnit(world, unit, new Point3(9, 9));
    }

    private static void TeleportUnit(GameWorld world, Unit unit, Point3 location)
    {
        var tileMap = world.GameMapMasterData.TileMap;
        var oldIndex = tileMap.GetIndex(unit.Location);
        if (world.GameMapData.Units.TryGetValue(oldIndex, out var list))
        {
            list.Remove(unit.Id);
            if (list.Count == 0)
                world.GameMapData.Units.Remove(oldIndex);
        }

        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }
}
