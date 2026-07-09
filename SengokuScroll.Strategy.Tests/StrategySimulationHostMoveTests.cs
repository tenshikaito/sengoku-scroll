using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Hosting;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>通过 StrategySimulationHost 复现 HTTP 层移动问题。</summary>
public class StrategySimulationHostMoveTests
{
    [Fact]
    public void Host_UnitAt1_2_Reaches3_2_InOneAdvanceDay()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");

        var world = GetWorld(host);
        Teleport(world, 1, new Point3(1, 2));
        var unit = world.GameData.Units[1];
        unit.Ap = 10;
        unit.Status = UnitStatus.Waiting;
        unit.ActionTarget.RoutePoints.Clear();

        host.OrderUnitMove(1, new Point2(3, 2));
        host.AdvanceDay();

        unit = world.GameData.Units[1];
        var trace = host.GetMovementTrace();

        Assert.Equal(3, unit.Location.X);
        Assert.Equal(2, unit.Location.Y);

        Assert.Contains(trace, e => e.Phase == "MoveDone" && e.To?.X == 3 && e.To?.Y == 2);
    }

    [Fact]
    public void Host_AfterLongMarchTo1_2_ThenOrder3_2_AdvancesTo3_2()
    {
        using var host = new StrategySimulationHost();
        host.LoadScenario("mini_kanto");
        host.OrderUnitMove(1, new Point2(1, 2));

        var world = GetWorld(host);
        for (var i = 0; i < 8; i++)
        {
            host.AdvanceDay();
            var u = world.GameData.Units[1];
            if (u.Location.X == 1 && u.Location.Y == 2)
                break;
        }

        var atStart = world.GameData.Units[1];
        Assert.Equal(1, atStart.Location.X);
        Assert.Equal(2, atStart.Location.Y);

        host.OrderUnitMove(1, new Point2(3, 2));
        atStart.Ap = 10;
        atStart.Status = UnitStatus.Moving;
        host.AdvanceDay();

        var afterDay1 = world.GameData.Units[1];
        Assert.Equal(3, afterDay1.Location.X);
        Assert.Equal(2, afterDay1.Location.Y);
        Assert.Equal(UnitStatus.Moving, afterDay1.Status);

        host.AdvanceDay();
        var afterDay2 = world.GameData.Units[1];
        Assert.Equal(3, afterDay2.Location.X);
        Assert.Equal(2, afterDay2.Location.Y);
        Assert.Equal(UnitStatus.Waiting, afterDay2.Status);
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
        world.GameMapData.Units.Remove(tileMap.GetIndex(unit.Location));
        unit.Location = location;
        MapLocationActions.RegisterUnit(world, unit);
    }
}
