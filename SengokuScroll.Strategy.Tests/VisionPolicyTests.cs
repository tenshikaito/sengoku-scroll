using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Tests;

public class VisionPolicyTests
{
    [Fact]
    public void EasyPreset_HasNoFogAndInstantMessages()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Easy, null);
        Assert.Equal(StrategyFogMode.None, options.FogMode);
        Assert.True(options.InstantEventMessages);
    }

    [Fact]
    public void NormalPreset_HasForceFogAndMaskedIntel()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null);
        Assert.Equal(StrategyFogMode.Force, options.FogMode);
        Assert.Equal(StrategyIntelMode.ForceIntel, options.IntelMode);
        Assert.False(options.InstantEventMessages);
    }

    [Fact]
    public void HardPreset_UsesCharacterFog()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Hard, null);
        Assert.Equal(StrategyFogMode.Character, options.FogMode);
    }

    [Fact]
    public void Parse_MapsLegacyLegendaryToHard()
    {
        Assert.Equal(StrategyDifficulty.Hard, StrategyDifficultyRules.Parse("Legendary"));
    }

    [Fact]
    public void ForceVisionPolicy_ExploresTilesAroundPlayerUnits()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        var ledger = new StrategyVisibilityLedger();
        ledger.Initialize(loaded.World, loaded.Meta);

        var state = ledger.GetOrCreate(loaded.Meta.PlayerForceId);
        Assert.NotEmpty(state.VisibleCells);
        Assert.True(state.IsExplored(loaded.World.GameData.Units[1].Location.X,
            loaded.World.GameData.Units[1].Location.Y,
            loaded.World.GameMapMasterData.TileMap.Width));
    }

    [Fact]
    public void MiniKanto_InitializesKakegawaAsKnownStronghold()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);
        Assert.Contains(6, loaded.Meta.KnownStrongholdIds);

        var ledger = new StrategyVisibilityLedger();
        ledger.Initialize(loaded.World, loaded.Meta);
        var state = ledger.GetOrCreate(loaded.Meta.PlayerForceId);
        Assert.Contains(6, state.KnownStrongholdIds);
    }

    [Fact]
    public void SightRange_UsesManhattanDistance()
    {
        var visible = new HashSet<(int X, int Y)>();
        StrategyVisionRules.AddSightBox(visible, new Common.Types.Point3(5, 5), 2, 20, 20);
        Assert.Contains((7, 5), visible);
        Assert.Contains((6, 6), visible);
        Assert.DoesNotContain((8, 5), visible);
        Assert.DoesNotContain((7, 7), visible);
    }

    [Fact]
    public void ForceVisionPolicy_StrongholdProvidesSight()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Maps", "mini_kanto.json");
        var loaded = StrategyScenarioLoader.LoadFromFile(path);

        var ledger = new StrategyVisibilityLedger();
        ledger.Initialize(loaded.World, loaded.Meta);
        var state = ledger.GetOrCreate(loaded.Meta.PlayerForceId);

        var kiyosu = loaded.World.GameData.Strongholds[1];
        Assert.Equal(2, kiyosu.Location.X);
        Assert.Equal(8, kiyosu.Location.Y);
        Assert.Contains((4, 8), state.VisibleCells);
        Assert.Contains((3, 9), state.VisibleCells);
        Assert.DoesNotContain((5, 8), state.VisibleCells);
    }

    [Fact]
    public void CombatRules_DoNotVaryByDifficulty()
    {
        Assert.Equal(
            StrategyDifficultyRules.DefeatResidualSoldierRatio(StrategyDifficulty.Easy),
            StrategyDifficultyRules.DefeatResidualSoldierRatio(StrategyDifficulty.Hard));
        Assert.Equal(
            StrategyDifficultyRules.PursuitDisengageChancePercent(StrategyDifficulty.Normal),
            StrategyDifficultyRules.PursuitDisengageChancePercent(StrategyDifficulty.Hard));
    }

    [Fact]
    public void InstantEventMessages_EasyOnlyViaPreset()
    {
        Assert.True(StrategyDifficultyRules.InstantEventMessages(StrategyDifficulty.Easy));
        Assert.False(StrategyDifficultyRules.InstantEventMessages(StrategyDifficulty.Normal));
    }
}
