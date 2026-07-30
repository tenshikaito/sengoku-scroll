using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>
/// M3 §3.2 整局可玩验收：加载 → 移动 → 攻击 → 方针 → 月结 → 存档/读档。
/// 以 Host 集成测试代替手工试玩联调。
/// </summary>
public class M3PlaythroughAcceptanceTests
{
    [Fact]
    public void Host_MiniKanto_Playthrough_LoadMoveAttackDirectiveMonthlySaveRestore()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario(
            "mini_kanto",
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy }).IsSuccess);

        var state = host.GetState().Value!;
        Assert.Equal("织田信长", state.Lord.Name);
        Assert.True(state.Units.Count >= 1);
        Assert.True(state.Strongholds.Count >= 10);

        // 地图主数据：区域与地标非空（替代旧 tileRegionNames Mock）
        var map = host.GetMapMaster().Value!;
        Assert.Equal(20 * 20, map.RegionIds.Count);
        Assert.True(map.Landmarks.Count >= 1);
        Assert.Contains(map.Regions, r => !string.IsNullOrWhiteSpace(r.Name));

        var world = GetWorld(host);
        IsolateEnemyUnits(world);
        Teleport(world, 1, new Point3(1, 2));
        var unit = world.GameData.Units[1];
        unit.Ap = 5;
        unit.Status = UnitStatus.Waiting;
        unit.ActionTarget.RoutePoints.Clear();

        // 移动
        Assert.True(host.OrderUnitMove(1, new Point2(3, 2)).IsSuccess);
        for (var i = 0; i < 8; i++)
        {
            host.AdvanceDay();
            unit = world.GameData.Units[1];
            if (unit.Location.X == 3 && unit.Location.Y == 2)
                break;
        }

        Assert.Equal(3, unit.Location.X);
        Assert.Equal(2, unit.Location.Y);

        // 攻击（瞬间战）
        Teleport(world, 2, new Point3(4, 2));
        world.GameData.Units[1].Ap = 5;
        world.GameData.Units[1].Status = UnitStatus.Waiting;
        var preview = host.PreviewUnitAttack(1, new Point2(4, 2));
        Assert.True(preview.IsSuccess);
        Assert.InRange(preview.Value!.AttackerWinRatePercent, 5, 95);

        var battle = host.ExecuteInstantBattle(1, new Point2(4, 2));
        Assert.True(battle.IsSuccess);
        Assert.NotNull(battle.Value!.Result);

        // 方针（异格经信使）
        var directive = host.OrderUnitDirective(1, UnitDirective.Occupy);
        Assert.True(directive.IsSuccess);
        Assert.Contains(
            directive.Value!.Outcome,
            new[] { "CarrierDispatched", "AppliedImmediately" });

        // 推进至月结日（2 月 1 日）并发出 EconomyMonthly
        var sawMonthly = false;
        for (var i = 0; i < 40; i++)
        {
            var advance = host.AdvanceDay();
            Assert.True(advance.IsSuccess);
            if (advance.Value!.Events.Any(e => e.Category == "EconomyMonthly"))
            {
                sawMonthly = true;
                break;
            }

            if (EconomyRules.IsMonthlySettlementDay(world.GameData.GameDate)
                && world.GameData.GameDate is { Month: 2, Day: 1 })
            {
                // 事件可能在同一推进结果中
            }
        }

        Assert.True(sawMonthly, "应在推进中出现 EconomyMonthly 月结事件");

        // 存档 / 读档
        var save = host.CaptureSave();
        Assert.True(save.IsSuccess);
        Assert.Equal("mini_kanto", save.Value!.ScenarioId);

        var unitXBefore = world.GameData.Units[1].Location.X;
        var dateBefore = world.GameData.GameDate;

        var restored = host.RestoreSave(save.Value);
        Assert.True(restored.IsSuccess);

        var worldAfter = GetWorld(host);
        Assert.Equal(dateBefore.Year, worldAfter.GameData.GameDate.Year);
        Assert.Equal(dateBefore.Month, worldAfter.GameData.GameDate.Month);
        Assert.Equal(dateBefore.Day, worldAfter.GameData.GameDate.Day);
        Assert.Equal(unitXBefore, worldAfter.GameData.Units[1].Location.X);
        Assert.Equal("织田信长", restored.Value!.Lord.Name);
    }

    private static GameWorld GetWorld(StrategySimulationHost host)
    {
        var field = typeof(StrategySimulationHost).GetField(
            "simulation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scope = field!.GetValue(host)!;
        return (GameWorld)scope.GetType().GetProperty("World")!.GetValue(scope)!;
    }

    private static void Teleport(GameWorld world, int unitId, Point3 location)
    {
        var unit = world.GameData.Units[unitId];
        var tileMap = world.GameMapMasterData.TileMap;
        var idx = tileMap.GetIndex(unit.Location);
        if (world.GameMapData.Units.TryGetValue(idx, out var list))
        {
            list.Remove(unit.Id);
            if (list.Count == 0)
                world.GameMapData.Units.Remove(idx);
        }

        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }

    private static void IsolateEnemyUnits(GameWorld world)
    {
        foreach (var u in world.GameData.Units.Values.Where(u => u.ForceId != 1).ToList())
            Teleport(world, u.Id, new Point3(9, 9));
    }
}
