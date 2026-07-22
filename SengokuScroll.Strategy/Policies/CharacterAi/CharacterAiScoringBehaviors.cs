using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Policies.CharacterAi;

public sealed class CharacterAiScoringContext
{
    public required Character Character { get; init; }

    public required GameData GameData { get; init; }

    public required StrategyScenarioMeta Meta { get; init; }

    public required int Age { get; init; }
}

public interface ICharacterAiScoringBehavior
{
    CharacterAiActionKind Kind { get; }

    CharacterAiEvaluation Score(CharacterAiScoringContext ctx);
}

internal sealed class RestCharacterAiScoringBehavior : ICharacterAiScoringBehavior
{
    public static readonly RestCharacterAiScoringBehavior Instance = new();

    public CharacterAiActionKind Kind => CharacterAiActionKind.Rest;

    public CharacterAiEvaluation Score(CharacterAiScoringContext ctx)
    {
        var character = ctx.Character;
        var score = 0;
        var reasons = new List<string>();

        if (character.IsSick)
        {
            score += 90;
            reasons.Add("Sick");
        }

        if (character.Hp < CharacterAiRules.LowHpThreshold)
        {
            score += 70 + (CharacterAiRules.LowHpThreshold - character.Hp);
            reasons.Add("LowHp");
        }
        else if (character.ActionPlan == CharacterActionPlan.Rest && character.Hp < 70)
        {
            score += 40 + (70 - character.Hp);
            reasons.Add("RestPlan");
        }

        if (ctx.Age >= CharacterAiRules.VeryElderAgeThreshold)
        {
            score += 25;
            reasons.Add("VeryElder");
        }
        else if (ctx.Age >= CharacterAiRules.ElderAgeThreshold && character.Hp < 55)
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
}

internal sealed class TaskRunCharacterAiScoringBehavior : ICharacterAiScoringBehavior
{
    public static readonly TaskRunCharacterAiScoringBehavior Instance = new();

    public CharacterAiActionKind Kind => CharacterAiActionKind.TaskRun;

    public CharacterAiEvaluation Score(CharacterAiScoringContext ctx)
    {
        var character = ctx.Character;
        if (character.ForceStatus != CharacterForceStatus.Task
            && character.ActionPlan != CharacterActionPlan.Task)
        {
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoTask");
        }

        if (character.IsSick || character.Hp < CharacterAiRules.LowHpThreshold)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "TooWeakForTask");

        var targetId = CharacterAiTargetResolver.ResolveTaskTargetStrongholdId(
            character,
            ctx.GameData,
            ctx.Meta);
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
}

internal sealed class VisitCharacterAiScoringBehavior : ICharacterAiScoringBehavior
{
    public static readonly VisitCharacterAiScoringBehavior Instance = new();

    public CharacterAiActionKind Kind => CharacterAiActionKind.Visit;

    public CharacterAiEvaluation Score(CharacterAiScoringContext ctx)
    {
        var character = ctx.Character;
        if (character.IsSick || character.Hp < CharacterAiRules.LowHpThreshold)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "TooWeakForVisit");

        if (ctx.Age >= CharacterAiRules.VeryElderAgeThreshold
            && character.ActionPlan != CharacterActionPlan.Meet)
        {
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "ElderAvoidTravel");
        }

        var score = 0;
        var reasons = new List<string>();
        score += CharacterActionPlanScoringModifiers.ApplyVisitPlanScore(character.ActionPlan, reasons);

        if (character.Emotion < CharacterAiRules.LowEmotionThreshold
            && character.ForceStatus == CharacterForceStatus.Idle)
        {
            score += 30 + (CharacterAiRules.LowEmotionThreshold - character.Emotion) / 2;
            reasons.Add("LowEmotion");
        }

        if (score <= 0)
            return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoVisit");

        var targetId = CharacterAiTargetResolver.ResolveVisitTargetStrongholdId(
            character,
            ctx.GameData,
            ctx.Meta);
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
}

public static class CharacterAiScoringBehaviorRegistry
{
    private static readonly ICharacterAiScoringBehavior[] Behaviors =
    [
        RestCharacterAiScoringBehavior.Instance,
        TaskRunCharacterAiScoringBehavior.Instance,
        VisitCharacterAiScoringBehavior.Instance,
    ];

    public static CharacterAiEvaluation EvaluateDailyAction(CharacterAiScoringContext ctx)
    {
        var scores = Behaviors.Select(b => b.Score(ctx)).ToList();
        var rest = scores[0];
        var task = scores[1];
        var visit = scores[2];

        if (rest.Score >= task.Score && rest.Score >= visit.Score && rest.Score > 0)
            return rest;

        if (task.Score >= visit.Score && task.Score > 0)
            return task;

        if (visit.Score > 0)
            return visit;

        return new CharacterAiEvaluation(CharacterAiActionKind.None, 0, 0, "NoAction");
    }
}
