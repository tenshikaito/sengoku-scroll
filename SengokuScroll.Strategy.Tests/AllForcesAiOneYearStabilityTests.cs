using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

/// <summary>覆盖跨季节、跨年经济结算与全势力 AI 的长时稳定性。</summary>
public sealed class AllForcesAiOneYearStabilityTests
{
    [Fact]
    public void MiniKanto_AllForcesAi_RunsOneYear_WithoutInvalidCoreState()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = new StrategyScenarioMeta
        {
            PlayerForceId = loaded.Meta.PlayerForceId,
            AllForcesAiControlled = true,
            Difficulty = loaded.Meta.Difficulty,
            StartOptions = loaded.Meta.StartOptions,
            KnownStrongholdIds = loaded.Meta.KnownStrongholdIds,
            LordName = loaded.Meta.LordName,
            LordUnitId = loaded.Meta.LordUnitId,
            LordStrongholdId = loaded.Meta.LordStrongholdId,
            ForceLordCharacterIds = loaded.Meta.ForceLordCharacterIds,
            Intel = loaded.Meta.Intel,
            RegionHarvestProfiles = loaded.Meta.RegionHarvestProfiles
        };

        using var context = StrategyTestWorldFactory.CreateFromWorld(loaded.World, meta);
        StrategyAiBootstrapHelper.BootstrapAggressiveDirectives(context.World, meta);

        var initialDate = context.World.GameData.GameDate;
        for (var day = 0; day < 365; day++)
            context.TimeController.AdvanceDay(context.World, context.Engine);

        var data = context.World.GameData;
        Assert.Equal(initialDate.AddDays(365), data.GameDate);
        Assert.All(data.Strongholds, pair =>
        {
            Assert.Equal(pair.Key, pair.Value.Id);
            Assert.True(pair.Value.Population >= 0, $"据点 {pair.Key} 人口为负");
            Assert.True(pair.Value.ForceActor.Food >= 0, $"据点 {pair.Key} 官府粮食为负");
            Assert.True(pair.Value.ForceActor.Money >= 0, $"据点 {pair.Key} 官府金钱为负");
        });
        Assert.All(data.Units, pair =>
        {
            Assert.Equal(pair.Key, pair.Value.Id);
            Assert.True(pair.Value.Soldier >= 0, $"单位 {pair.Key} 兵力为负");
            Assert.True(pair.Value.Food >= 0, $"单位 {pair.Key} 粮食为负");
            Assert.True(pair.Value.Money >= 0, $"单位 {pair.Key} 金钱为负");
        });
    }
}
