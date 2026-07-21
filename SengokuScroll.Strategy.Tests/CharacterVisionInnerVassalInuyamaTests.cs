using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

/// <summary>
/// 内藩（犬山）视野：角色迷雾下据点不提供 sight，内藩部队仍共享视野；势力迷雾下据点 range2。
/// </summary>
public class CharacterVisionInnerVassalInuyamaTests
{
    private const int InuyamaX = 4;
    private const int InuyamaY = 4;
    private const int InuyamaStrongholdId = 2;
    private const int InnerVassalForceId = 3;
    private const int LordX = 2;
    private const int LordY = 8;
    private const int InnerVassalUnitId = 99;
    private const int InnerVassalUnitX = 8;
    private const int InnerVassalUnitY = 4;
    /// <summary>犬山据点 range2 最东格；(10,4) 仅内藩部队 range2 可达。</summary>
    private const int UnitOnlyVisibleX = 10;
    private const int UnitOnlyVisibleY = 4;

    [Fact]
    public void CharacterFog_InnerVassalInuyama_StrongholdDoesNotProvideSight()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario(
            "mini_kanto",
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Hard }).IsSuccess);

        var state = host.GetState().Value!;
        Assert.Equal("Character", state.StartOptions?.FogMode);

        var inuyama = state.Strongholds.Single(s => s.Id == InuyamaStrongholdId);
        Assert.Equal("犬山", inuyama.Name);
        Assert.Equal(InnerVassalForceId, inuyama.ForceId);
        Assert.Equal(InuyamaX, inuyama.X);
        Assert.Equal(InuyamaY, inuyama.Y);

        Assert.Equal(LordX, state.Lord.X);
        Assert.Equal(LordY, state.Lord.Y);

        foreach (var (x, y) in EnumerateManhattanDisc(InuyamaX, InuyamaY, 2))
        {
            if (IsWithinManhattan(LordX, LordY, x, y, 2))
                continue;

            Assert.False(
                IsCellVisible(state, x, y),
                $"Cell ({x},{y}) must not be visible from Inuyama stronghold under Character fog.");
        }

        Assert.False(IsCellVisible(state, InuyamaX + 2, InuyamaY));
        Assert.False(IsWithinManhattan(LordX, LordY, InuyamaX + 2, InuyamaY, 2));
    }

    [Fact]
    public void ForceFog_InnerVassalInuyama_StrongholdProvidesSight()
    {
        using var host = new StrategySimulationHost();
        Assert.True(host.LoadScenario(
            "mini_kanto",
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Normal }).IsSuccess);

        var state = host.GetState().Value!;
        Assert.Equal("Force", state.StartOptions?.FogMode);

        foreach (var (x, y) in EnumerateManhattanDisc(InuyamaX, InuyamaY, 2))
        {
            Assert.True(
                IsCellVisible(state, x, y),
                $"Cell ({x},{y}) should be visible via inner vassal Inuyama sight (range 2).");
        }

        Assert.True(IsCellVisible(state, InuyamaX + 2, InuyamaY));
        Assert.False(IsWithinManhattan(LordX, LordY, InuyamaX + 2, InuyamaY, 2));
    }

    [Fact]
    public void ForceFog_InnerVassalUnit_ExtendsVisionBeyondStrongholdSight()
    {
        var (world, meta, options) = LoadMiniKantoWithInnerVassalPatrolUnit(StrategyDifficulty.Normal);
        Assert.Equal(StrategyFogMode.Force, options.FogMode);

        var visible = new ForceVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((InuyamaX + 2, InuyamaY), visible);
        Assert.Contains((UnitOnlyVisibleX, UnitOnlyVisibleY), visible);
        Assert.False(IsWithinManhattan(InuyamaX, InuyamaY, UnitOnlyVisibleX, UnitOnlyVisibleY, 2));
    }

    [Fact]
    public void CharacterFog_InnerVassalUnit_ExtendsVisionBeyondStrongholdSight()
    {
        var (world, meta, options) = LoadMiniKantoWithInnerVassalPatrolUnit(StrategyDifficulty.Hard);
        Assert.Equal(StrategyFogMode.Character, options.FogMode);

        var visible = new CharacterVisionPolicy().ComputeVisibleTiles(
            world,
            meta,
            meta.PlayerForceId,
            options);

        Assert.Contains((InuyamaX + 2, InuyamaY), visible);
        Assert.Contains((UnitOnlyVisibleX, UnitOnlyVisibleY), visible);
        Assert.False(IsWithinManhattan(InuyamaX, InuyamaY, UnitOnlyVisibleX, UnitOnlyVisibleY, 2));
    }

    [Fact]
    public void CharacterFog_PlayerFieldUnit_DoesNotExtendVisionWhenLordAtHome()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Hard });
        var options = meta.StartOptions;

        var odaVanguard = loaded.World.GameData.Units[1];
        Assert.Equal(1, odaVanguard.ForceId);
        Assert.Equal(8, odaVanguard.Location.X);
        Assert.Equal(8, odaVanguard.Location.Y);

        var visible = new CharacterVisionPolicy().ComputeVisibleTiles(
            loaded.World,
            meta,
            meta.PlayerForceId,
            options);

        Assert.DoesNotContain((10, 8), visible);
        Assert.False(IsWithinManhattan(LordX, LordY, 10, 8, 2));
    }

    private static (GameWorld World, StrategyScenarioMeta Meta, GameStartOptions Options)
        LoadMiniKantoWithInnerVassalPatrolUnit(StrategyDifficulty difficulty)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var meta = StrategyScenarioLoader.ApplyLoadOptions(
            loaded.Meta,
            new StrategyLoadOptions { Difficulty = difficulty });
        var options = meta.StartOptions;

        var patrol = StrategyTestWorldBuilder.CreateTestUnit(
            InnerVassalUnitId,
            InnerVassalForceId,
            new Point3(InnerVassalUnitX, InnerVassalUnitY));
        patrol.Soldier = 500;
        loaded.World.GameData.Units[InnerVassalUnitId] = patrol;
        MapLocationActions.RegisterUnit(loaded.World, patrol);

        return (loaded.World, meta, options);
    }

    private static bool IsCellVisible(StrategyWorldStateDto state, int x, int y)
        => state.Visibility?.VisibleCells.Any(c => c.X == x && c.Y == y) == true;

    private static IEnumerable<(int X, int Y)> EnumerateManhattanDisc(int cx, int cy, int range)
    {
        for (var dy = -range; dy <= range; dy++)
        {
            for (var dx = -range; dx <= range; dx++)
            {
                if (Math.Abs(dx) + Math.Abs(dy) > range)
                    continue;

                yield return (cx + dx, cy + dy);
            }
        }
    }

    private static bool IsWithinManhattan(int ax, int ay, int bx, int by, int range)
        => Math.Abs(ax - bx) + Math.Abs(ay - by) <= range;
}
