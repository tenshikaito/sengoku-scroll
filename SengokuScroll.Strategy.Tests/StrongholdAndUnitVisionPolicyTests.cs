using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

/// <summary>
/// 势力迷雾 vs 角色迷雾：据点与单位（部队）视野来源对照。
/// </summary>
public class StrongholdAndUnitVisionPolicyTests
{
    private const int KiyosuX = 2;
    private const int KiyosuY = 8;
    private const int OdaVanguardUnitId = 1;
    private const int InuyamaX = 4;
    private const int InuyamaY = 4;
    private const int InnerVassalForceId = 3;
    private const int InnerVassalPatrolUnitId = 99;
    private const int InnerVassalPatrolX = 8;
    private const int InnerVassalPatrolY = 4;
    private const int UnitOnlyVisibleX = 10;
    private const int UnitOnlyVisibleY = 4;

    [Fact]
    public void ForceVisionPolicy_Stronghold_ProvidesSightAroundOwnStronghold()
    {
        var (world, meta, options) = LoadMiniKanto(StrategyDifficulty.Normal);
        var visible = new ForceVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((4, KiyosuY), visible);
        Assert.Contains((0, KiyosuY), visible);
        Assert.DoesNotContain((5, KiyosuY), visible);
    }

    [Fact]
    public void ForceVisionPolicy_Unit_ProvidesSightAroundOwnFieldArmy()
    {
        var (world, meta, options) = LoadMiniKanto(StrategyDifficulty.Normal);
        var vanguard = world.GameData.Units[OdaVanguardUnitId];

        var visible = new ForceVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Equal(8, vanguard.Location.X);
        Assert.Equal(8, vanguard.Location.Y);
        Assert.Contains((10, 8), visible);
        Assert.False(IsWithinManhattan(KiyosuX, KiyosuY, 10, 8, 2));
    }

    [Fact]
    public void CharacterVisionPolicy_Stronghold_DoesNotProvideSightWhenLordIsAwayFromHome()
    {
        var (world, meta, options) = LoadMiniKanto(StrategyDifficulty.Hard);
        PlaceLordOnMap(world, meta, new Point3(KiyosuX + 3, KiyosuY));

        var visible = new CharacterVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((KiyosuX + 3 + 2, KiyosuY), visible);
        Assert.DoesNotContain((0, KiyosuY), visible);
        Assert.False(IsWithinManhattan(KiyosuX + 3, KiyosuY, 0, KiyosuY, 2));
    }

    [Fact]
    public void ForceVisionPolicy_Stronghold_StillProvidesHomeSightWhenLordIsAway()
    {
        var (world, meta, options) = LoadMiniKanto(StrategyDifficulty.Normal);
        PlaceLordOnMap(world, meta, new Point3(KiyosuX + 3, KiyosuY));

        var visible = new ForceVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((0, KiyosuY), visible);
        Assert.False(IsWithinManhattan(KiyosuX + 3, KiyosuY, 0, KiyosuY, 2));
    }

    [Fact]
    public void CharacterVisionPolicy_Lord_ProvidesSightAroundCurrentLocation()
    {
        var (world, meta, options) = LoadMiniKanto(StrategyDifficulty.Hard);
        var lordLocation = StrategyLordHelper.ResolveLocation(world.GameData, meta);

        var visible = new CharacterVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Equal(KiyosuX, lordLocation.X);
        Assert.Equal(KiyosuY, lordLocation.Y);
        Assert.Contains((KiyosuX + 2, KiyosuY), visible);
        Assert.DoesNotContain((InuyamaX + 2, InuyamaY), visible);
        Assert.False(IsWithinManhattan(KiyosuX, KiyosuY, InuyamaX + 2, InuyamaY, 2));
    }

    [Fact]
    public void CharacterVisionPolicy_InnerRealmUnit_ProvidesSightBeyondLord()
    {
        var (world, meta, options) = LoadMiniKantoWithInnerVassalPatrol(StrategyDifficulty.Hard);

        var visible = new CharacterVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((UnitOnlyVisibleX, UnitOnlyVisibleY), visible);
        Assert.False(IsWithinManhattan(KiyosuX, KiyosuY, UnitOnlyVisibleX, UnitOnlyVisibleY, 2));
        Assert.False(IsWithinManhattan(InuyamaX, InuyamaY, UnitOnlyVisibleX, UnitOnlyVisibleY, 2));
    }

    [Fact]
    public void CharacterVisionPolicy_OwnFieldUnit_DoesNotProvideSightWhenLordAtHome()
    {
        var (world, meta, options) = LoadMiniKanto(StrategyDifficulty.Hard);
        var vanguard = world.GameData.Units[OdaVanguardUnitId];

        var visible = new CharacterVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.DoesNotContain((vanguard.Location.X + 2, vanguard.Location.Y), visible);
    }

    private static (GameWorld World, StrategyScenarioMeta Meta, GameStartOptions Options)
        LoadMiniKanto(StrategyDifficulty difficulty)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = difficulty });
        return (loaded.World, meta, meta.StartOptions);
    }

    private static (GameWorld World, StrategyScenarioMeta Meta, GameStartOptions Options)
        LoadMiniKantoWithInnerVassalPatrol(StrategyDifficulty difficulty)
    {
        var loaded = LoadMiniKanto(difficulty);
        var patrol = StrategyTestWorldBuilder.CreateTestUnit(
            InnerVassalPatrolUnitId,
            InnerVassalForceId,
            new Point3(InnerVassalPatrolX, InnerVassalPatrolY));
        patrol.Soldier = 500;
        loaded.World.GameData.Units[InnerVassalPatrolUnitId] = patrol;
        MapLocationActions.RegisterUnit(loaded.World, patrol);
        return loaded;
    }

    private static void PlaceLordOnMap(GameWorld world, StrategyScenarioMeta meta, Point3 location)
    {
        var lordCharacterId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            world.GameData);
        Assert.True(lordCharacterId > 0);
        var lord = world.GameData.Characters[lordCharacterId];
        lord.LocationType = CharacterLocationType.Map;
        lord.Location = location;
        lord.StrongholdId = 0;
        lord.LocationStrongholdId = 0;
    }

    private static bool IsWithinManhattan(int ax, int ay, int bx, int by, int range)
        => Math.Abs(ax - bx) + Math.Abs(ay - by) <= range;
}
