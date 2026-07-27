using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>?? StrategySimulationHost ?? HTTP ??????/summary>
public class StrategySimulationHostMoveTests
{
    [Fact]
    public void Host_UnitAt1_2_Reaches3_2_InTwoAdvanceDays()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto", new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy });

        var world = GetWorld(host);
        foreach (var u in world.GameData.Units.Values.Where(u => u.ForceId != 1).ToList())
            Teleport(world, u.Id, new Point3(9, 9));

        Teleport(world, 1, new Point3(1, 2));
        var unit = world.GameData.Units[1];
        unit.Ap = 5;
        unit.Status = UnitStatus.Waiting;
        unit.ActionTarget.RoutePoints.Clear();

        host.OrderUnitMove(1, new Point2(3, 2));
        for (var i = 0; i < 6; i++)
        {
            host.AdvanceDay();
            unit = world.GameData.Units[1];
            if (unit.Location.X == 3 && unit.Location.Y == 2)
                break;
        }

        var trace = host.GetMovementTrace();

        Assert.Equal(3, unit.Location.X);
        Assert.Equal(2, unit.Location.Y);

        Assert.Contains(trace, e => e.Phase == "MoveDone" && e.To?.X == 3 && e.To?.Y == 2);
    }

    [Fact]
    public void Host_AfterLongMarchTo1_2_ThenOrder3_2_AdvancesTo3_2()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto", new StrategyLoadOptions { Difficulty = StrategyDifficulty.Easy });

        var world = GetWorld(host);
        Teleport(world, 1, new Point3(1, 2));

        var atStart = world.GameData.Units[1];
        Assert.Equal(1, atStart.Location.X);
        Assert.Equal(2, atStart.Location.Y);

        // ????????????? mini_kanto ????????????????
        foreach (var u in world.GameData.Units.Values.Where(u => u.IsMilitary && u.ForceId != 1).ToList())
            Teleport(world, u.Id, new Point3(9, 9));
        atStart.Stance = UnitStance.Normal;
        atStart.ActionTarget.UnitId = 0;
        atStart.Status = UnitStatus.Waiting;
        atStart.Ap = 5;

        host.OrderUnitMove(1, new Point2(3, 2));
        for (var i = 0; i < 6; i++)
        {
            host.AdvanceDay();
            if (world.GameData.Units[1].Location.X == 3 && world.GameData.Units[1].Location.Y == 2)
                break;
        }

        var afterMarch = world.GameData.Units[1];
        Assert.Equal(3, afterMarch.Location.X);
        Assert.Equal(2, afterMarch.Location.Y);
    }

    private static GameWorld GetWorld(StrategySimulationHost host)
    {
        var field = typeof(StrategySimulationHost).GetField("simulation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var scope = field!.GetValue(host)!;
        return (GameWorld)scope.GetType().GetProperty("World")!.GetValue(scope)!;
    }

    private static void Teleport(GameWorld world, int unitId, Point3 location)
    {
        var unit = world.GameData.Units[unitId];
        var tileMap = world.GameMapMasterData.TileMap;
        var __idx = tileMap.GetIndex(unit.Location); if (world.GameMapData.Units.TryGetValue(__idx, out var __list)) { __list.Remove(unit.Id); if (__list.Count==0) world.GameMapData.Units.Remove(__idx); }
        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }
}
