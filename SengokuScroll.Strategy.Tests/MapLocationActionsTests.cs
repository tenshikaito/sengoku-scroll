using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Tests;

/// <summary><see cref="MapLocationActions"/> 与单位移动集成测试。</summary>
public class MapLocationActionsTests
{
    [Fact]
    public void SetUnitLocation_UpdatesTileIndex()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var world = ctx.World;
        var unit = world.GameData.Units[1];
        var worldContext = ctx.Services.GetRequiredService<Domain.Contexts.IGameWorldContext>();
        var tileMap = world.GameMapMasterData.TileMap;

        var indexOrigin = tileMap.GetIndex(new Point3(0, 0));
        var indexTarget = tileMap.GetIndex(new Point3(2, 0));

        Assert.Equal(1, world.GameMapData.Units[indexOrigin]);

        MapLocationActions.SetUnitLocation(worldContext, unit, new Point3(2, 0));

        Assert.False(world.GameMapData.Units.ContainsKey(indexOrigin));
        Assert.Equal(1, world.GameMapData.Units[indexTarget]);
        Assert.Equal(new Point3(2, 0), unit.Location);
        Assert.Same(unit, worldContext.GetUnitOrDefault(new Point2(2, 0)));
    }

    [Fact]
    public void AdvanceDay_KeepsUnitTileIndexInSync()
    {
        using var ctx = StrategyTestWorldFactory.Create();
        var world = ctx.World;
        var unit = world.GameData.Units[1];
        var tileMap = world.GameMapMasterData.TileMap;
        var worldContext = ctx.Services.GetRequiredService<Domain.Contexts.IGameWorldContext>();

        unit.Status = UnitStatus.Moving;
        unit.ActionTarget.RoutePoints.Enqueue(new Point2(1, 0));
        unit.Ap = 10;
        unit.IsReadyToMove = true;

        var indexOrigin = tileMap.GetIndex(new Point3(0, 0));
        var indexTarget = tileMap.GetIndex(new Point3(1, 0));

        ctx.TimeController.AdvanceDay(world, ctx.Engine);

        Assert.Equal(new Point3(1, 0), unit.Location);
        Assert.False(world.GameMapData.Units.ContainsKey(indexOrigin));
        Assert.Equal(1, world.GameMapData.Units[indexTarget]);
        Assert.Same(unit, worldContext.GetUnitOrDefault(new Point2(1, 0)));
    }
}
