using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Policies.CharacterAi;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Rules;

/// <summary>角色 AI 决策类型（P1：Visit / TaskRun）。</summary>
public enum CharacterAiActionKind
{
    None,
    Rest,
    TaskRun,
    Visit
}

/// <summary>角色 AI 评估上下文。</summary>
public readonly record struct CharacterAiEvaluation(
    CharacterAiActionKind Kind,
    int Score,
    int TargetStrongholdId,
    string Reason);

/// <summary>
/// 角色自由行动 AI：综合体力、年龄、疾病、心情、任务状态与行动计划评估 Visit / TaskRun。
/// </summary>
public static class CharacterAiRules
{
    public const int LowHpThreshold = 35;
    public const int ElderAgeThreshold = 60;
    public const int VeryElderAgeThreshold = 70;
    public const int LowEmotionThreshold = 45;

    /// <summary>是否应跳过本日 AI（已在移动/行动或不可自主活动）。</summary>
    public static bool ShouldSkipDailyAi(Character character)
        => CharacterAiSkipBehaviorRegistry.ShouldSkipDailyAi(character);

    /// <summary>是否玩家当主（非全 AI 模式下不自主决策）。</summary>
    public static bool IsPlayerLord(Character character, StrategyScenarioMeta meta, GameData gameData)
    {
        var lordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            meta.PlayerForceId,
            meta,
            gameData);
        return lordId == character.Id;
    }

    /// <summary>根据角色属性与计划评估本日最优行动。</summary>
    public static CharacterAiEvaluation EvaluateDailyAction(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        GameDate gameDate)
    {
        var ctx = new CharacterAiScoringContext
        {
            Character = character,
            GameData = gameData,
            Meta = meta,
            Age = ComputeAge(character, gameDate),
        };

        return CharacterAiScoringBehaviorRegistry.EvaluateDailyAction(ctx);
    }

    public static int ComputeAge(Character character, GameDate gameDate)
    {
        var age = Math.Max(0, gameDate.Year - character.Birthday.Year);
        if (gameDate.Month < character.Birthday.Month
            || (gameDate.Month == character.Birthday.Month && gameDate.Day < character.Birthday.Day))
        {
            age--;
        }

        return Math.Max(0, age);
    }

    /// <summary>角色是否已在目标据点格（在城或同格地图）。</summary>
    public static bool IsAtStronghold(Character character, Stronghold stronghold)
    {
        if (character.LocationType == CharacterLocationType.Stronghold)
            return character.LocationStrongholdId == stronghold.Id;

        return character.Location.X == stronghold.Location.X
               && character.Location.Y == stronghold.Location.Y;
    }
}
