using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Tests;

public class CharacterAiRulesTests
{
    private static StrategyLoadedScenario LoadMiniKanto()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SengokuScroll.Strategy", "Maps", "mini_kanto.json");
        return StrategyScenarioLoader.LoadFromFile(Path.GetFullPath(path));
    }

    [Fact]
    public void EvaluateDailyAction_LowHpAndSick_PrefersRest()
    {
        var loaded = LoadMiniKanto();
        var character = loaded.World.GameData.Characters.Values
            .First(c => c.ForceId == loaded.Meta.PlayerForceId && c.Id != loaded.Meta.ForceLordCharacterIds[loaded.Meta.PlayerForceId]);
        character.Hp = 20;
        character.IsSick = true;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.LocationType = CharacterLocationType.Stronghold;

        var evaluation = CharacterAiRules.EvaluateDailyAction(
            character,
            loaded.World.GameData,
            loaded.Meta,
            loaded.World.GameData.GameDate);

        Assert.Equal(CharacterAiActionKind.Rest, evaluation.Kind);
        Assert.True(evaluation.Score > 0);
    }

    [Fact]
    public void EvaluateDailyAction_TaskStatus_PrefersTaskRunWhenHealthy()
    {
        var loaded = LoadMiniKanto();
        var character = loaded.World.GameData.Characters.Values
            .First(c => c.ForceId == loaded.Meta.PlayerForceId && c.Id != loaded.Meta.ForceLordCharacterIds[loaded.Meta.PlayerForceId]);
        character.Hp = 80;
        character.IsSick = false;
        character.ForceStatus = CharacterForceStatus.Task;
        character.ActionPlan = CharacterActionPlan.Task;
        character.ActionStatus = CharacterActionStatus.Waiting;

        var target = loaded.World.GameData.Strongholds.Values
            .First(s => s.ForceId == character.ForceId && s.Id != character.StrongholdId);
        character.ActionTarget.StrongholdId = target.Id;

        var evaluation = CharacterAiRules.EvaluateDailyAction(
            character,
            loaded.World.GameData,
            loaded.Meta,
            loaded.World.GameData.GameDate);

        Assert.Equal(CharacterAiActionKind.TaskRun, evaluation.Kind);
        Assert.Equal(target.Id, evaluation.TargetStrongholdId);
    }

    [Fact]
    public void EvaluateDailyAction_LowEmotion_PrefersVisit()
    {
        var loaded = LoadMiniKanto();
        var character = loaded.World.GameData.Characters.Values
            .First(c => c.ForceId == loaded.Meta.PlayerForceId && c.Id != loaded.Meta.ForceLordCharacterIds[loaded.Meta.PlayerForceId]);
        character.Hp = 70;
        character.Emotion = 30;
        character.IsSick = false;
        character.ForceStatus = CharacterForceStatus.Idle;
        character.ActionPlan = CharacterActionPlan.Rest;
        character.ActionStatus = CharacterActionStatus.Waiting;

        var evaluation = CharacterAiRules.EvaluateDailyAction(
            character,
            loaded.World.GameData,
            loaded.Meta,
            loaded.World.GameData.GameDate);

        Assert.Equal(CharacterAiActionKind.Visit, evaluation.Kind);
    }
}
