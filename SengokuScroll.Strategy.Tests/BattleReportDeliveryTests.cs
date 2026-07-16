using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>战报投递：简易当日解锁；标准须信使或同格。</summary>
public class BattleReportDeliveryTests
{
    [Fact]
    public void AdvanceDay_Easy_SameTileBattle_PlayerReceivesSameDayBattleReport()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var easyMeta = new StrategyScenarioMeta
        {
            PlayerForceId = loaded.Meta.PlayerForceId,
            Difficulty = StrategyDifficulty.Easy,
            LordName = loaded.Meta.LordName,
            LordUnitId = loaded.Meta.LordUnitId,
            LordStrongholdId = loaded.Meta.LordStrongholdId,
            ForceLordCharacterIds = loaded.Meta.ForceLordCharacterIds,
            Intel = loaded.Meta.Intel,
            RegionHarvestProfiles = loaded.Meta.RegionHarvestProfiles
        };

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, easyMeta);
        ForceSameTileEngage(ctx.World);

        var dayBuffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        dayBuffer.Clear();
        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.True(
            dayBuffer.Events.Any(e => e.Category == "BattleReportArrived" && e.BattleResult is not null)
            || dayBuffer.ResolvedBattles.Count > 0
            || ctx.World.GameData.Battlefields.Values.Any(b => !b.IsClosed)
            || ctx.World.GameData.Units.Values.Any(u => u.BattlefieldId > 0));
    }

    [Fact]
    public void AdvanceDay_Normal_SameTileBattle_NoImmediateFrontlineUnlock()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        ForceSameTileEngage(ctx.World);

        var dayBuffer = ctx.Services.GetRequiredService<StrategyDayOutcomeBuffer>();
        dayBuffer.Clear();
        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        Assert.DoesNotContain(
            dayBuffer.Events,
            e => e.Category == "BattleReportArrived"
                 && e.Message.Contains("当日前线战报", StringComparison.Ordinal));

        Assert.True(
            ctx.World.GameData.Messengers.Values.Any(m => m.PayloadType == MessengerPayloadType.BattleReport)
            || dayBuffer.Events.Any(e =>
                e.Category is "UnitDestroyed" or "UnitFledToStronghold" or "BattleReportArrived")
            || dayBuffer.ResolvedBattles.Count > 0
            || ctx.World.GameData.Battlefields.Values.Any(b => !b.IsClosed));
    }

    private static void ForceSameTileEngage(Domain.GameWorld world)
    {
        var player = world.GameData.Units.Values.First(u => u.ForceId == 1 && u.IsMilitary);
        var enemy = world.GameData.Units.Values.First(u => u.ForceId != 1 && u.IsMilitary);
        enemy.Directive = UnitDirective.Occupy;
        enemy.Morale = 80;
        player.Morale = 80;

        var tileMap = world.GameMapMasterData.TileMap;
        var old = tileMap.GetIndex(enemy.Location);
        if (world.GameMapData.Units.TryGetValue(old, out var list))
        {
            list.Remove(enemy.Id);
            if (list.Count == 0)
                world.GameMapData.Units.Remove(old);
        }

        enemy.Location = player.Location;
        MapLocationActions.RegisterUnit(world, enemy);
    }
}
