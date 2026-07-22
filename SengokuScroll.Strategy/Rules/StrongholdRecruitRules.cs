using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Rules;

/// <summary>征兵校验。</summary>
public static class StrongholdRecruitRules
{
    public const int PopulationPerSoldier = 2;
    public const int MoneyCostPerSoldier = 100;
    public const int FoodCostPerSoldier = 50;
    public const int MaxRecruitPerOrder = 500;

    public static bool CanRecruitAt(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
        => StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData);

    public static int CalculateMaxRecruitable(Stronghold stronghold)
    {
        var byMoney = stronghold.ForceActor.Money / MoneyCostPerSoldier;
        var byFood = stronghold.ForceActor.Food / FoodCostPerSoldier;
        var byPopulation = stronghold.Population / PopulationPerSoldier;
        return (int)Math.Min(MaxRecruitPerOrder, Math.Min(byMoney, Math.Min(byFood, byPopulation)));
    }
}
