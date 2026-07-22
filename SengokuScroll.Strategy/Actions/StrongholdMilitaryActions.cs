using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点军事指令（征兵等）。</summary>
public static class StrongholdMilitaryActions
{
    public static bool TryRecruit(
        Stronghold stronghold,
        int soldiers,
        out string? error)
    {
        error = null;

        if (soldiers <= 0)
        {
            error = "InvalidRecruitCount";
            return false;
        }

        var max = StrongholdRecruitRules.CalculateMaxRecruitable(stronghold);
        if (soldiers > max)
        {
            error = "InsufficientRecruitResources";
            return false;
        }

        var moneyCost = soldiers * StrongholdRecruitRules.MoneyCostPerSoldier;
        var foodCost = soldiers * StrongholdRecruitRules.FoodCostPerSoldier;
        var populationCost = soldiers * StrongholdRecruitRules.PopulationPerSoldier;

        stronghold.ForceActor.Money -= moneyCost;
        stronghold.ForceActor.Food -= foodCost;
        stronghold.ForceActor.Soldier += soldiers;
        stronghold.Population -= populationCost;

        var feelings = stronghold.CivilianActor.PopularFeelings;
        stronghold.CivilianActor.PopularFeelings = (byte)Math.Max(
            0,
            feelings - Math.Min(MarketConstants.PopularFeelingsTaxIncreaseMaxPenalty, soldiers / 50));

        return true;
    }
}
