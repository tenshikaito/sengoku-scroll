using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary>军事单位移动节奏：约 2 日最多 3 格。</summary>
public class MovementPaceTests
{
    [Fact]
    public void AdvanceTwoDays_OnPlain_MovesAtMostThreeTiles()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var unit = ctx.World.GameData.Units[1];
        unit.Location = new Point3(0, 0);
        MapLocationActions.RegisterUnit(ctx.World, unit);
        unit.Status = UnitStatus.Moving;
        unit.Ap = 5;
        unit.ActionTarget.RoutePoints.Clear();
        for (var x = 1; x <= 4; x++)
            unit.ActionTarget.RoutePoints.Enqueue(new Point2(x, 0));

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
        var afterDay1 = unit.Location.X;

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
        var afterDay2 = unit.Location.X;

        var moved = afterDay2 - 0;
        Assert.InRange(moved, 1, 3);
        Assert.True(afterDay1 >= 1);
        Assert.True(afterDay2 >= afterDay1);
    }

    [Fact]
    public void LoadMiniKanto_ClampsUnitMovementToFive()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);

        foreach (var unit in loaded.World.GameData.Units.Values)
            Assert.True(unit.Movement <= 5);
    }
}
