using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Policies.BattlefieldDto;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>自由决策方针下，每月自动派任务时的侧重方向。</summary>
public enum StrongholdGovernanceMonthlyFocus
{
    None = 0,
    Military = 1,
    Domestic = 2,
}

/// <summary>
/// 评估据点需求与代官/领主能力性格，供自由决策方针在每月初选择任务侧重。
/// </summary>
public static class StrongholdGovernanceEvaluator
{
    /// <summary>据点子需求与官员倾向综合后的月度侧重。</summary>
    public static StrongholdGovernanceMonthlyFocus EvaluateMonthlyFocus(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var needs = EvaluateStrongholdNeeds(stronghold, gameData, meta);
        var official = ResolveGovernanceOfficial(stronghold, gameData);
        var bias = official is null
            ? OfficialGovernanceBias.Empty
            : EvaluateOfficialBias(official);

        var militaryScore = needs.MilitaryScore + bias.MilitaryBias;
        var domesticScore = needs.DomesticScore + bias.DomesticBias;

        if (needs.BlockConscript)
            militaryScore = Math.Max(0, militaryScore - 20);

        if (militaryScore <= 0 && domesticScore <= 0)
            return StrongholdGovernanceMonthlyFocus.None;

        return militaryScore >= domesticScore
            ? StrongholdGovernanceMonthlyFocus.Military
            : StrongholdGovernanceMonthlyFocus.Domestic;
    }

    /// <summary>代官优先；无代官时用据点领主（非当主直辖展示名）。</summary>
    public static Character? ResolveGovernanceOfficial(Stronghold stronghold, GameData gameData)
    {
        foreach (var officialId in new[] { stronghold.LeaderId, stronghold.LordId })
        {
            if (officialId <= 0)
                continue;

            if (gameData.Characters.TryGetValue(officialId, out var official) && !official.IsDead)
                return official;
        }

        return null;
    }

    private static StrongholdNeeds EvaluateStrongholdNeeds(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var military = 0;
        var domestic = 0;

        var garrison = Math.Max(0, stronghold.ForceActor.Soldier);
        var population = Math.Max(0, stronghold.Population);
        var expectedGarrison = Math.Max(200, population / 40);
        if (garrison < expectedGarrison)
            military += Math.Min(35, (expectedGarrison - garrison) / 20);

        if (garrison < 300)
            military += 10;

        if (stronghold.ForceActor.Morale < 55)
            military += 8;

        if (stronghold.ForceActor.Training < 55)
            military += 8;

        if (IsUnderSiegeThreat(stronghold, gameData))
            military += 35;

        if (stronghold.ForceActor.Money >= RecruitConstants.MinMercenaryBudget * 10
            && garrison < expectedGarrison)
        {
            military += 8;
        }

        var popular = stronghold.CivilianActor.PopularFeelings;
        if (popular < 45)
            domestic += 22;

        if (stronghold.Stability < 45)
            domestic += 22;

        var adminEfficiency = AdministrationCalculator.CalculateAdministrativeEfficiencyPercent(
            stronghold,
            gameData,
            meta);
        if (adminEfficiency < 60)
            domestic += 12;

        if (stronghold.Corruption > 35)
            domestic += 10;

        var foodDays = population > 0
            ? stronghold.ForceActor.Food / Math.Max(1, population / 100)
            : int.MaxValue;
        if (foodDays < 30)
            domestic += 18;

        if (stronghold.ForceActor.Money < RecruitConstants.MoneyPerKan * 5)
            military = Math.Max(0, military - 12);

        var blockConscript = popular < 35 || stronghold.Stability < 35;

        return new StrongholdNeeds(military, domestic, blockConscript);
    }

    /// <summary>民心/治安过低时不应征兵。</summary>
    public static bool ShouldAvoidConscript(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
        => EvaluateStrongholdNeeds(stronghold, gameData, meta).BlockConscript;

    private static OfficialGovernanceBias EvaluateOfficialBias(Character official)
    {
        var military = 0;
        var domestic = 0;

        military += official.Power / 6;
        military += official.Leadership / 8;
        domestic += official.Politics / 6;
        domestic += official.Charm / 10;

        var personality = official.Personality;
        military += personality.Courage / 12;
        military += personality.Ambition / 15;
        domestic += (100 - personality.Action) / 18;
        domestic += personality.Principle / 20;

        military += official.Proficiency.Military.Level / 12;
        domestic += Math.Max(
            official.Proficiency.Agriculture.Level,
            official.Proficiency.Commerce.Level) / 14;

        return new OfficialGovernanceBias(
            Math.Clamp(military, 0, 35),
            Math.Clamp(domestic, 0, 35));
    }

    private static bool IsUnderSiegeThreat(Stronghold stronghold, GameData gameData)
        => StrategyWorldStateDtoSiegeThreatResolver.Resolve(stronghold, gameData) is not null;

    private readonly record struct StrongholdNeeds(int MilitaryScore, int DomesticScore, bool BlockConscript);

    private readonly record struct OfficialGovernanceBias(int MilitaryBias, int DomesticBias)
    {
        public static OfficialGovernanceBias Empty { get; } = new(0, 0);
    }
}
