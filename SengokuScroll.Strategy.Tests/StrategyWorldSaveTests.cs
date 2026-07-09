using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Persistence;

namespace SengokuScroll.Strategy.Tests;

/// <summary>存档捕获/恢复（M3-d）测试。</summary>
public class StrategyWorldSaveTests
{
    [Fact]
    public void CaptureAndApply_RestoresForceTreasuryAndUnitPosition()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var world = loaded.World;

        var unit = world.GameData.Units[1];
        unit.Location = new Common.Types.Point3(5, 5);
        unit.Soldier = 42;
        world.GameData.Forces[1].Money = 99999;

        var save = StrategyWorldSaveService.Capture(world, "mini_kanto", loaded.Meta.PlayerForceId);

        unit.Location = new Common.Types.Point3(0, 0);
        unit.Soldier = 1;
        world.GameData.Forces[1].Money = 0;

        StrategyWorldSaveService.Apply(save, world);

        Assert.Equal(99999, world.GameData.Forces[1].Money);
        Assert.Equal(42, world.GameData.Units[1].Soldier);
        Assert.Equal(5, world.GameData.Units[1].Location.X);
        Assert.Equal(5, world.GameData.Units[1].Location.Y);
    }
}
