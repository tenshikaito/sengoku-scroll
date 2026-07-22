using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Policies.CharacterAi;

public interface ICharacterActionPlanScoringModifier
{
    CharacterActionPlan Plan { get; }

    int ApplyScore(List<string> reasons);
}

internal sealed class MeetActionPlanScoringModifier : ICharacterActionPlanScoringModifier
{
    public static readonly MeetActionPlanScoringModifier Instance = new();

    public CharacterActionPlan Plan => CharacterActionPlan.Meet;

    public int ApplyScore(List<string> reasons)
    {
        reasons.Add("MeetPlan");
        return 65;
    }
}

internal sealed class ReportActionPlanScoringModifier : ICharacterActionPlanScoringModifier
{
    public static readonly ReportActionPlanScoringModifier Instance = new();

    public CharacterActionPlan Plan => CharacterActionPlan.Report;

    public int ApplyScore(List<string> reasons)
    {
        reasons.Add("ReportPlan");
        return 60;
    }
}

public static class CharacterActionPlanScoringModifiers
{
    private static readonly ICharacterActionPlanScoringModifier[] Modifiers =
    [
        MeetActionPlanScoringModifier.Instance,
        ReportActionPlanScoringModifier.Instance,
    ];

    public static int ApplyVisitPlanScore(CharacterActionPlan plan, List<string> reasons)
        => Modifiers.FirstOrDefault(m => m.Plan == plan)?.ApplyScore(reasons) ?? 0;
}

public interface ICharacterActionPlanVisitTargetBehavior
{
    CharacterActionPlan Plan { get; }

    int? ResolveTargetStrongholdId(Character character, GameData gameData, StrategyScenarioMeta meta);
}

internal sealed class ReportActionPlanVisitTargetBehavior : ICharacterActionPlanVisitTargetBehavior
{
    public static readonly ReportActionPlanVisitTargetBehavior Instance = new();

    public CharacterActionPlan Plan => CharacterActionPlan.Report;

    public int? ResolveTargetStrongholdId(Character character, GameData gameData, StrategyScenarioMeta meta)
        => StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);
}

public static class CharacterAiTargetResolver
{
    private static readonly ICharacterActionPlanVisitTargetBehavior[] VisitTargetBehaviors =
    [
        ReportActionPlanVisitTargetBehavior.Instance,
    ];

    public static int ResolveTaskTargetStrongholdId(
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

    public static int ResolveVisitTargetStrongholdId(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var fromPlan = VisitTargetBehaviors
            .FirstOrDefault(b => b.Plan == character.ActionPlan)
            ?.ResolveTargetStrongholdId(character, gameData, meta);
        if (fromPlan is > 0)
            return fromPlan.Value;

        if (character.ActionTarget.StrongholdId > 0
            && gameData.Strongholds.TryGetValue(character.ActionTarget.StrongholdId, out var target)
            && target.ForceId == character.ForceId)
        {
            return target.Id;
        }

        return StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);
    }
}
