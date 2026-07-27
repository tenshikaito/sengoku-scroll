using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public class ForceVisionConvoyAndLordTests
{
    [Fact]
    public void ForceVisionPolicy_MovingConvoy_ProvidesSightAroundLocation()
    {
        var (world, meta) = LoadMiniKanto(StrategyDifficulty.Normal);
        var options = meta.StartOptions with { CharacterSharedVision = false };

        var convoyX = 6;
        var convoyY = 6;
        var transport = StrategyTestWorldBuilder.CreateTestTransportUnit(
            9001,
            meta.PlayerForceId,
            new Point3(convoyX, convoyY));
        transport.TransportOriginStrongholdId = 1;
        transport.Ap = 4;
        transport.Movement = 4;
        StrategyTestWorldBuilder.RegisterTransportUnit(world, transport);

        var visible = new ForceVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((convoyX + 1, convoyY), visible);
    }

    [Fact]
    public void ForceVisionPolicy_WithCharacterSharedVisionOff_StillUsesPlayerLordOnMap()
    {
        var (world, meta) = LoadMiniKanto(StrategyDifficulty.Normal);
        var options = meta.StartOptions with { CharacterSharedVision = false };
        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            world.GameData);
        Assert.True(lordId > 0);
        var lord = world.GameData.Characters[lordId];
        lord.LocationType = Character.CharacterLocationType.Map;
        lord.Location = new Point3(9, 9);

        var visible = new ForceVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((10, 9), visible);
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
