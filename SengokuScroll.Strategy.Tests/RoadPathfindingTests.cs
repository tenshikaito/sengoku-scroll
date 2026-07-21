using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>道路对寻路 AP 消耗的影响。</summary>
public class RoadPathfindingTests
{
    [Fact]
    public void MiniKanto_RoadTrunk_LowersApCostOnHighwayCells()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);
        var pathfinding = ctx.Services.GetRequiredService<IPathfindingService>();
        var rules = ctx.Services.GetRequiredService<MovementRules>();

        var unit = ctx.World.GameData.Units[1];
        unit.Location = new Point3(2, 8);
        unit.Ap = 20;

        var route = pathfinding.CalculatePath(unit, new Point2(5, 8));

        Assert.NotNull(route);
        Assert.True(route!.Count >= 3);

        var roadStep = route.First(n => n.Location.X == 3 && n.Location.Y == 8);
        Assert.Equal(1, roadStep.StepCost);
        Assert.Equal(1, rules.GetTileMovementApCost(unit, roadStep.Location));
    }
}
