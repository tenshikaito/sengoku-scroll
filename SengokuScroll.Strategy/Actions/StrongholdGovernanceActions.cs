using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点政务方针设置与月度自动派任务。</summary>
public static class StrongholdGovernanceActions
{
    public static GameError? TrySetGovernancePriority(
        Stronghold stronghold,
        StrongholdGovernancePriority priority,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (!Enum.IsDefined(priority))
            return GameError.DataNotFound;

        stronghold.GovernancePriority = priority;
        return null;
    }

    /// <summary>每月 1 日：按方针向领主以外待命将领发布任务令。</summary>
    public static void ProcessMonthlyGovernanceAssignments(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        bool innerVassalSelfGoverned = false)
    {
        if (innerVassalSelfGoverned)
        {
            if (!StrongholdGovernanceRules.IsInnerVassalRealmStrongholdUnderPlayer(stronghold, meta, gameData))
                return;
        }
        else if (!StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData))
        {
            return;
        }

        var focus = innerVassalSelfGoverned
            ? StrongholdGovernanceEvaluator.EvaluateMonthlyFocus(stronghold, gameData, meta)
            : ResolveMonthlyFocus(stronghold, gameData, meta);
        if (focus == StrongholdGovernanceMonthlyFocus.Military)
            ProcessMilitaryAssignments(stronghold, gameData, meta, innerVassalSelfGoverned);
    }

    private static StrongholdGovernanceMonthlyFocus ResolveMonthlyFocus(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
        => stronghold.GovernancePriority switch
        {
            StrongholdGovernancePriority.Military => StrongholdGovernanceMonthlyFocus.Military,
            StrongholdGovernancePriority.Domestic => StrongholdGovernanceMonthlyFocus.Domestic,
            StrongholdGovernancePriority.Autonomous =>
                StrongholdGovernanceEvaluator.EvaluateMonthlyFocus(stronghold, gameData, meta),
            _ => StrongholdGovernanceMonthlyFocus.None,
        };

    private static void ProcessMilitaryAssignments(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        bool innerVassalSelfGoverned = false)
    {
        var assignable = StrongholdGovernanceRules
            .ListGovernanceAssignableGenerals(stronghold, gameData, meta)
            .ToList();
        if (assignable.Count == 0)
            return;

        var allowConscript = !ShouldBlockConscriptForAutonomous(stronghold, gameData, meta, innerVassalSelfGoverned);
        Character? conscriptGeneral = null;
        if (allowConscript)
        {
            conscriptGeneral = assignable.FirstOrDefault(c =>
                StrongholdRecruitTaskRules.TryValidateConscriptCapacity(stronghold, out _));
        }

        if (conscriptGeneral is not null)
        {
            _ = StrongholdRecruitTaskActions.TryAssignConscriptRecruitTask(
                stronghold,
                conscriptGeneral.Id,
                gameData,
                meta);
            assignable.Remove(conscriptGeneral);
        }

        if (assignable.Count == 0)
            return;

        var budget = CalculateAutoMercenaryBudget(stronghold);
        if (budget < RecruitConstants.MinMercenaryBudget)
            return;

        if (!StrongholdRecruitTaskRules.TryValidateStrongholdMercenaryBudget(stronghold, budget, out _))
            return;

        var mercenaryGeneral = assignable[0];
        _ = StrongholdRecruitTaskActions.TryAssignMercenaryRecruitTask(
            stronghold,
            mercenaryGeneral.Id,
            budget,
            gameData,
            meta);
    }

    private static bool ShouldBlockConscriptForAutonomous(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta,
        bool innerVassalSelfGoverned = false)
    {
        if (!innerVassalSelfGoverned
            && stronghold.GovernancePriority != StrongholdGovernancePriority.Autonomous)
        {
            return false;
        }

        return StrongholdGovernanceEvaluator.ShouldAvoidConscript(stronghold, gameData, meta);
    }

    private static int CalculateAutoMercenaryBudget(Stronghold stronghold)
    {
        var pool = stronghold.ForceActor.Money;
        if (pool <= 0)
            return 0;

        var fractionBudget = pool
                             * StrongholdGovernanceConstants.AutoMercenaryBudgetNumerator
                             / StrongholdGovernanceConstants.AutoMercenaryBudgetDenominator;
        var maxBudget = StrongholdGovernanceConstants.AutoMercenaryBudgetMaxKan * RecruitConstants.MoneyPerKan;
        return Math.Min(fractionBudget, maxBudget);
    }
}
