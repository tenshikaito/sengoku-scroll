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
        Assert.Equal(2, world.GameData.Units.Count);
        Assert.Equal(1560, world.GameData.GameDate.Year);
    }

    [Fact]
    public void LoadFromFile_MiniKanto_PathfindingWorksBetweenForces()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;

        using var ctx = StrategyTestWorldFactory.CreateFromWorld(world);
        var odaUnit = world.GameData.Units[1];

        // 织田先锋 100 兵、2000 合粮
        Assert.Equal(100, odaUnit.Soldier);
        Assert.Equal(2000, odaUnit.Food);

        ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);

        // 日推进后应扣日耗粮 200 合
        Assert.Equal(1800, odaUnit.Food);
    }
}
