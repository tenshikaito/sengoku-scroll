using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Time;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests.Fixtures;

/// <summary>mini_kanto 今川主力攻三河凑集成测试场景搭建。</summary>
public static class MiniKantoSiegeScenarioHelper
{
    public const int OdaVanguardId = 1;
    public const int ImagawaVanguardId = 2;
    public const int ImagawaMainId = 21;
    public const int MikawaMinatoStrongholdId = 7;
    public const int KiyosuStrongholdId = 1;
    public const int KakegawaStrongholdId = 6;
    public const int ImagawaForceId = 2;
    public const int OdaForceId = 1;

    public static string MapPath =>
        Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");

    public static (GameWorld World, StrategyScenarioMeta Meta) Load()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MapPath);
        return (loaded.World, loaded.Meta);
    }

    /// <summary>
    /// 按测试设定摆放单位：今川先锋在织田先锋右侧，今川主力在三河凑 x+3，方针进攻三河凑。
    /// </summary>
    public static StrategyTestContext CreateImagawaMainVsMikawaContext(int maxTilesMovedPerDay = 1)
    {
        var (world, meta) = Load();
        ConfigureImagawaMainVsMikawa(world);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(world, meta);
        ctx.Services.GetRequiredService<GameRuleConfig>().MaxTilesMovedPerDay = maxTilesMovedPerDay;
        return ctx;
    }

    public static void ConfigureImagawaMainVsMikawa(GameWorld world)
    {
        var mikawa = world.GameData.Strongholds[MikawaMinatoStrongholdId];
        var odaVanguard = world.GameData.Units[OdaVanguardId];
        var imagawaVanguard = world.GameData.Units[ImagawaVanguardId];
        var imagawaMain = world.GameData.Units[ImagawaMainId];

        TeleportUnit(world, odaVanguard, new Point3(4, 4));
        TeleportUnit(world, imagawaVanguard, new Point3(5, 4));
        TeleportUnit(world, imagawaMain, new Point3(mikawa.Location.X + 3, mikawa.Location.Y));

        FreezeUnitForScenario(odaVanguard);
        FreezeUnitForScenario(imagawaVanguard);

        imagawaMain.Directive = UnitDirective.Occupy;
        imagawaMain.DirectiveTargetId = MikawaMinatoStrongholdId;
        imagawaMain.ActionTarget.StrongholdId = 0;
        imagawaMain.ActionTarget.UnitId = 0;
        imagawaMain.ActionTarget.RoutePoints.Clear();
        imagawaMain.SiegeMode = UnitSiegeMode.None;
        imagawaMain.Stance = UnitStance.Normal;
        imagawaMain.Status = UnitStatus.Waiting;
        imagawaMain.BattlefieldId = 0;
    }

    public static void AdvanceDay(StrategyTestContext ctx)
        => ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

    public static void AdvanceDays(StrategyTestContext ctx, int days)
    {
        for (var i = 0; i < days; i++)
            AdvanceDay(ctx);
    }

    /// <summary>
    /// 实机对齐：双今川满编、无 DirectiveTargetId，仅摆放起始位置。
    /// </summary>
    public static StrategyTestContext CreateLiveImagawaVsMikawaContext(int maxTilesMovedPerDay = 1)
    {
        var (world, meta) = Load();
        ConfigureLiveImagawaVsMikawa(world);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(world, meta);
        ctx.Services.GetRequiredService<GameRuleConfig>().MaxTilesMovedPerDay = maxTilesMovedPerDay;
        return ctx;
    }

    public static void ConfigureLiveImagawaVsMikawa(GameWorld world)
    {
        var mikawa = world.GameData.Strongholds[MikawaMinatoStrongholdId];
        var odaVanguard = world.GameData.Units[OdaVanguardId];
        var imagawaVanguard = world.GameData.Units[ImagawaVanguardId];
        var imagawaMain = world.GameData.Units[ImagawaMainId];

        TeleportUnit(world, odaVanguard, new Point3(4, 4));
        TeleportUnit(world, imagawaVanguard, new Point3(5, 4));
        TeleportUnit(world, imagawaMain, new Point3(mikawa.Location.X + 3, mikawa.Location.Y));

        foreach (var unit in new[] { imagawaVanguard, imagawaMain })
        {
            unit.DirectiveTargetId = 0;
            unit.ActionTarget.StrongholdId = 0;
            unit.ActionTarget.UnitId = 0;
            unit.ActionTarget.RoutePoints.Clear();
            unit.SiegeMode = UnitSiegeMode.None;
            unit.Stance = UnitStance.Normal;
            unit.Status = UnitStatus.Waiting;
            unit.BattlefieldId = 0;
        }
    }

    public static Unit ImagawaVanguard(GameData data) => data.Units[ImagawaVanguardId];

    public static Unit ImagawaMain(GameData data) => data.Units[ImagawaMainId];

    public static Stronghold MikawaMinato(GameData data) => data.Strongholds[MikawaMinatoStrongholdId];

    public static Stronghold Kiyosu(GameData data) => data.Strongholds[KiyosuStrongholdId];

    public static Stronghold Kakegawa(GameData data) => data.Strongholds[KakegawaStrongholdId];

    public static StrategyWorldStateDto ToDto(StrategyTestContext ctx, StrategyScenarioMeta meta)
        => StrategyWorldStateMapper.ToDto(ctx.World, "mini_kanto", meta);

    public static bool HasBattleReportMessenger(GameData data, int forceId)
        => data.Messengers.Values.Any(m =>
            m.ForceId == forceId && m.PayloadType == MessengerPayloadType.BattleReport);

    public static bool HasStrategicReportMessenger(GameData data, int forceId)
        => data.Messengers.Values.Any(m =>
            m.ForceId == forceId && m.PayloadType == MessengerPayloadType.StrategicReport);

    public static bool HasBattleReportMessengerToward(
        GameData data,
        int forceId,
        Point3 destination)
        => data.Messengers.Values.Any(m =>
            m.ForceId == forceId
            && m.PayloadType == MessengerPayloadType.BattleReport
            && m.RoutePoints.Count > 0
            && m.RoutePoints.Last().X == destination.X
            && m.RoutePoints.Last().Y == destination.Y);

    public static StrategyBattlefieldStateDto? OpenSiegeBattlefieldAt(StrategyWorldStateDto dto, int x, int y)
        => dto.Battlefields.FirstOrDefault(b => b.X == x && b.Y == y && b.Kind == "Siege");

    /// <summary>前端交战格不绘制单军图标：开放战场格上的单位不计入「地图可见单军」。</summary>
    public static IReadOnlyList<StrategyUnitStateDto> VisibleMapUnits(
        StrategyWorldStateDto dto,
        int x,
        int y)
    {
        var hidden = dto.Battlefields
            .Where(b => b.X == x && b.Y == y)
            .SelectMany(b => b.UnitIds)
            .ToHashSet();
        return dto.Units.Where(u => !hidden.Contains(u.Id)).ToList();
    }

    private static void FreezeUnitForScenario(Unit unit)
    {
        // 业务：测试隔离——邻格对峙的先锋不参与接敌，避免干扰主力攻城时间线
        unit.Soldier = 0;
        unit.Directive = UnitDirective.Move;
        unit.Status = UnitStatus.Waiting;
        unit.ActionTarget.RoutePoints.Clear();
        unit.ActionTarget.UnitId = 0;
        unit.SiegeMode = UnitSiegeMode.None;
        unit.BattlefieldId = 0;
    }

    public static void TeleportUnit(GameWorld world, Unit unit, Point3 location)
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
