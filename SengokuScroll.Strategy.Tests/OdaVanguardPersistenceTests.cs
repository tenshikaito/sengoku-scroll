using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>诊断 mini_kanto 织田先锋是否在日推进后异常消失。</summary>
public class OdaVanguardPersistenceTests
{
    [Fact]
    public void AdvanceFourDays_OdaVanguard_ShouldRemainOnMap()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        using var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World);

        Assert.True(loaded.World.GameData.Units.ContainsKey(1));

        for (var day = 1; day <= 4; day++)
        {
            ctx.TimeController.AdvanceDay(ctx.World, ctx.Engine);
            var u1 = ctx.World.GameData.Units.GetValueOrDefault(1);
            var msg = u1 is null
                ? "removed"
                : $"soldiers={u1.Soldier} at ({u1.Location.X},{u1.Location.Y}) status={u1.Status} directive={u1.Directive}";
            Assert.True(u1 is not null && u1.Soldier > 0, $"Day {day}: unit 1 {msg}");
        }
    }
}
