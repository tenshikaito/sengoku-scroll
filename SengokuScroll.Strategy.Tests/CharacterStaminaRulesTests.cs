using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Tests.Fixtures;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class CharacterStaminaRulesTests
{
    private static string MiniKantoPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json"));

    [Fact]
    public void ApplyCommandFatigue_ReducesHpButNotBelowOneForYoungHealthy()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var general = ctx.World.GameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId && c.Name == "林秀贞");

        general.Hp = 70;
        general.IsSick = false;
        general.ForceStatus = CharacterForceStatus.Task;
        general.ActionStatus = CharacterActionStatus.Acting;
        general.Birthday = new Domain.Types.GameDate(loaded.World.GameData.GameDate.Year - 30, 1, 1);

        CharacterStaminaRules.ApplyCommandFatigue(general, ctx.World.GameData);

        Assert.Equal(66, general.Hp);
        Assert.False(general.IsSick);
    }

    [Fact]
    public void ApplyCommandFatigue_LowHpCanBecomeSick()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var general = ctx.World.GameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId && c.Name == "林秀贞");

        general.Hp = 62;
        general.IsSick = false;
        general.ForceStatus = CharacterForceStatus.Task;
        general.ActionStatus = CharacterActionStatus.Acting;
        general.Birthday = new Domain.Types.GameDate(loaded.World.GameData.GameDate.Year - 30, 1, 1);

        for (var i = 0; i < 20 && !general.IsSick; i++)
            CharacterStaminaRules.ApplyCommandFatigue(general, ctx.World.GameData);

        Assert.True(general.IsSick || general.Hp < CharacterStaminaRules.SickHpThreshold);
    }

    [Fact]
    public void ApplyCommandFatigue_ElderSickAtZeroHp_Dies()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var general = ctx.World.GameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId && c.Name == "林秀贞");

        general.Hp = 4;
        general.IsSick = true;
        general.ForceStatus = CharacterForceStatus.Task;
        general.ActionStatus = CharacterActionStatus.Acting;
        general.Birthday = new Domain.Types.GameDate(loaded.World.GameData.GameDate.Year - 65, 1, 1);

        CharacterStaminaRules.ApplyCommandFatigue(general, ctx.World.GameData);

        Assert.True(general.IsDead);
        Assert.Equal(0, general.Hp);
    }

    [Fact]
    public void EffectiveStats_HalvesAttributesWhenSick()
    {
        var loaded = StrategyScenarioLoader.LoadFromFile(MiniKantoPath);
        var ctx = StrategyTestWorldFactory.CreateFromWorld(loaded.World, loaded.Meta);
        var general = ctx.World.GameData.Characters.Values.First(c =>
            c.ForceId == loaded.Meta.PlayerForceId && c.Name == "林秀贞");

        general.Leadership = 80;
        general.Proficiency.Military.Level = 6;
        general.IsSick = true;

        Assert.Equal(40, SengokuScroll.Strategy.Helpers.CharacterEffectiveStats.Leadership(general));
        Assert.Equal(3, SengokuScroll.Strategy.Helpers.CharacterEffectiveStats.SkillLevel(
            general.Proficiency.Military.Level,
            general));
    }
}
