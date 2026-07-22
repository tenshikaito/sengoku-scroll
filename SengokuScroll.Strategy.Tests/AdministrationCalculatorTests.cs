using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Tests.Fixtures;

namespace SengokuScroll.Strategy.Tests;

public class AdministrationCalculatorTests
{
    [Fact]
    public void CalculateDistanceAdministrativeLoss_IsZeroAtCapital()
    {
        var (world, meta) = LoadMiniKanto(StrategyDifficulty.Normal);
        var gameData = world.GameData;
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            meta.PlayerForceId,
            gameData,
            meta);
        Assert.True(residenceId > 0);
        var capital = gameData.Strongholds[residenceId];

        Assert.Equal(0, AdministrationCalculator.CalculateDistanceAdministrativeLoss(capital, gameData, meta));
    }

    [Fact]
    public void CalculateCollectionEfficiencyBp_LowerWhenFarFromCapital()
    {
        var (world, meta) = LoadMiniKanto(StrategyDifficulty.Normal);
        var gameData = world.GameData;
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            meta.PlayerForceId,
            gameData,
            meta);
        var capital = gameData.Strongholds[residenceId];
        var remote = gameData.Strongholds.Values
            .Where(s => s.ForceId == meta.PlayerForceId && s.Id != residenceId)
            .OrderByDescending(s =>
                AdministrationCalculator.CalculateCapitalManhattanDistance(s, gameData, meta))
            .First();

        remote.Authority = capital.Authority;
        remote.Corruption = capital.Corruption;

        var atCapital = EconomyCalculator.CalculateCollectionEfficiencyBp(capital, gameData, meta);
        var farAway = EconomyCalculator.CalculateCollectionEfficiencyBp(remote, gameData, meta);

        Assert.True(farAway < atCapital, $"remote={farAway} capital={atCapital}");
    }

    private static (Domain.GameWorld World, StrategyScenarioMeta Meta) LoadMiniKanto(StrategyDifficulty difficulty)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = difficulty });
        return (loaded.World, meta);
    }
}
