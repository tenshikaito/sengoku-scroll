using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>策略剧本 JSON 加载器（M1-d）的集成测试。</summary>
public class StrategyScenarioLoaderTests
{
    [Fact]
    public void LoadFromFile_MiniKanto_HasFourForcesAndTenStrongholds()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");

        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;

        Assert.Equal("mini_kanto", world.Name);
        Assert.Equal(4, world.GameData.Forces.Count);
        Assert.Equal(10, world.GameData.Strongholds.Count);
        Assert.Equal(3, world.GameData.Units.Count);
        Assert.Equal(20, world.GameMapMasterData.TileMap.Width);
        Assert.Equal(20, world.GameMapMasterData.TileMap.Height);
        Assert.True(world.GameMapData.Roads.Count >= 12);
        Assert.Equal(1, world.GameMapMasterData.TileMap.GetRegion(new Common.Types.Point3(0, 0)));
        Assert.Equal(2, world.GameMapMasterData.TileMap.GetRegion(new Common.Types.Point3(10, 0)));
    }

    [Fact]
    public void LoadFromFile_MiniKanto_PathfindingWorksBetweenForces()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var odaUnit = world.GameData.Units[1];

        // 织田先锋 3000 兵、6000000 合粮
        Assert.Equal(3000, odaUnit.Soldier);
        Assert.Equal(6_000_000, odaUnit.Food);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        // 日推进后应扣日耗粮 9000 合（3000 兵 × 3 合/日）
        Assert.Equal(5_991_000, odaUnit.Food);
    }
}
