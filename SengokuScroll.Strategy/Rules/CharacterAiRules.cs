using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
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
    {
        if (character.IsDead)
            return true;

        if (character.ForceStatus is CharacterForceStatus.Prisoner or CharacterForceStatus.UnitAction)
            return true;

        if (character.ActionStatus is CharacterActionStatus.Moving
            or CharacterActionStatus.Acting
            or CharacterActionStatus.Resting)
        {
            return true;
        }

        if (character.LocationType == CharacterLocationType.Unit)
            return true;

        return false;
    }

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
        var age = ComputeAge(character, gameDate);
        var restScore = ScoreRest(character, age);
        var taskScore = ScoreTaskRun(character, gameData, meta);
        var visitScore = ScoreVisit(character, gameData, meta, age);

        if (restScore.Score >= taskScore.Score && restScore.Score >= visitScore.Score && restScore.Score > 0)
            return restScore;

        if (taskScore.Score >= visitScore.Score && taskScore.Score > 0)
            return taskScore;

        if (visitScore.Score > 0)
            return visitScore;

        return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoAction");
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

    private static CharacterAiEvaluation ScoreRest(Character character, int age)
    {
        var score = 0;
        var reasons = new List<string>();

        if (character.IsSick)
        {
            score += 90;
            reasons.Add("Sick");
        }

        if (character.Hp < LowHpThreshold)
        {
            score += 70 + (LowHpThreshold - character.Hp);
            reasons.Add("LowHp");
        }
        else if (character.ActionPlan == CharacterActionPlan.Rest && character.Hp < 70)
        {
            score += 40 + (70 - character.Hp);
            reasons.Add("RestPlan");
        }

        if (age >= VeryElderAgeThreshold)
        {
            score += 25;
            reasons.Add("VeryElder");
        }
        else if (age >= ElderAgeThreshold && character.Hp < 55)
        {
            score += 20;
            reasons.Add("ElderLowHp");
        }

        if (character.Emotion < 25 && character.Hp < 50)
        {
            score += 15;
            reasons.Add("LowMood");
        }

        if (score <= 0)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoRest");

        return new CharacterAiEvaluation(
            CharacterAiActionKind.Rest,
            score,
            character.LocationStrongholdId > 0 ? character.LocationStrongholdId : character.StrongholdId,
            string.Join("+", reasons));
    }

    private static CharacterAiEvaluation ScoreTaskRun(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (character.ForceStatus != CharacterForceStatus.Task
            && character.ActionPlan != CharacterActionPlan.Task)
        {
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoTask");
        }

        if (character.IsSick || character.Hp < LowHpThreshold)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "TooWeakForTask");

        var targetId = ResolveTaskTargetStrongholdId(character, gameData, meta);
        if (targetId <= 0)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoTaskTarget");

        var score = character.ForceStatus == CharacterForceStatus.Task ? 80 : 55;
        if (character.Ap < 3)
            score -= 15;

        return new CharacterAiEvaluation(
            CharacterAiActionKind.TaskRun,
            score,
            targetId,
            "Task");
    }

    private static CharacterAiEvaluation ScoreVisit(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        int age)
    {
        if (character.IsSick || character.Hp < LowHpThreshold)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "TooWeakForVisit");

        if (age >= VeryElderAgeThreshold && character.ActionPlan != CharacterActionPlan.Meet)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "ElderAvoidTravel");

        var score = 0;
        var reasons = new List<string>();

        if (character.ActionPlan == CharacterActionPlan.Meet)
        {
            score += 65;
            reasons.Add("MeetPlan");
        }
        else if (character.ActionPlan == CharacterActionPlan.Report)
        {
            score += 60;
            reasons.Add("ReportPlan");
        }

        if (character.Emotion < LowEmotionThreshold && character.ForceStatus == CharacterForceStatus.Idle)
        {
            score += 30 + (LowEmotionThreshold - character.Emotion) / 2;
            reasons.Add("LowEmotion");
        }

        if (score <= 0)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoVisit");

        var targetId = ResolveVisitTargetStrongholdId(character, gameData, meta);
        if (targetId <= 0)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoVisitTarget");

        if (character.Ap < 2)
            score -= 10;

        return new CharacterAiEvaluation(
            CharacterAiActionKind.Visit,
            score,
            targetId,
            string.Join("+", reasons));
    }

    private static int ResolveTaskTargetStrongholdId(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (character.ActionTarget.StrongholdId > 0
            && gameData.Strongholds.TryGetValue(character.ActionTarget.StrongholdId, out var fromTarget)
            && fromTarget.ForceId == character.ForceId)
        {
            return fromTarget.Id;
        }

        return StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);
    }

    private static int ResolveVisitTargetStrongholdId(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (character.ActionPlan == CharacterActionPlan.Report)
        {
            return StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);
        }

        if (character.ActionTarget.StrongholdId > 0
            && gameData.Strongholds.TryGetValue(character.ActionTarget.StrongholdId, out var target)
            && target.ForceId == character.ForceId)
        {
            return target.Id;
        }

        return StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);
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
