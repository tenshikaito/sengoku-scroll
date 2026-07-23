using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Policies.GameStart;

namespace SengokuScroll.Strategy.Tests;

public class GameStartOptionsProfileTests
{
    [Fact]
    public void CharacterFog_AppliesControlAndVisionConstraints()
    {
        var options = new GameStartOptions
        {
            FogMode = StrategyFogMode.Character,
            IntelMode = StrategyIntelMode.ForceIntel,
            ControlMode = StrategyControlMode.FullDirect,
            AllySharedVision = true,
            CharacterSharedVision = true,
            ShowAllyIntel = false,
            InstantEventMessages = false,
            IntelDebugMode = true
        };

        var sanitized = GameStartOptionsProfile.ApplyAllConstraints(options);

        Assert.Equal(StrategyControlMode.DirectiveOnly, sanitized.ControlMode);
        Assert.False(sanitized.AllySharedVision);
        Assert.False(sanitized.CharacterSharedVision);
    }

    [Fact]
    public void Profile_InstantEvents_EasyAlwaysEnabled()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null);
        var profile = GameStartOptionsProfile.Create(options, StrategyDifficulty.Easy);
        Assert.True(profile.InstantEvents.ShouldPushInstantSummary());
    }

    [Fact]
    public void Profile_InstantEvents_CustomNormalRespectsFlag()
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null);
        var profile = GameStartOptionsProfile.Create(options, StrategyDifficulty.Normal);
        Assert.False(profile.InstantEvents.ShouldPushInstantSummary());

        var enabled = options with { InstantEventMessages = true };
        profile = GameStartOptionsProfile.Create(enabled, StrategyDifficulty.Custom);
        Assert.True(profile.InstantEvents.ShouldPushInstantSummary());
    }

    [Theory]
    [InlineData(StrategyFogMode.None, true)]
    [InlineData(StrategyFogMode.Force, false)]
    [InlineData(StrategyFogMode.Character, false)]
    public void Profile_FogDisabled_MatchesMode(StrategyFogMode mode, bool expected)
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null) with { FogMode = mode };
        var profile = GameStartOptionsProfile.Create(options);
        Assert.Equal(expected, profile.Fog.FogDisabled);
    }

    [Theory]
    [InlineData(StrategyIntelMode.Full)]
    [InlineData(StrategyIntelMode.ForceIntel)]
    public void Profile_IntelBehaviorFactory_MatchesMode(StrategyIntelMode mode)
    {
        var options = GameStartOptionsPresets.Resolve(StrategyDifficulty.Normal, null) with { IntelMode = mode };
        var profile = GameStartOptionsProfile.Create(options);
        Assert.Equal(mode, profile.Intel.Mode);
    }
}
