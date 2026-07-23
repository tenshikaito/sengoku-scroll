using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Rules;

/// <summary>募兵/征兵任务校验与人数估算。</summary>
public static class StrongholdRecruitTaskRules
{
    public static bool CanAssignRecruitTaskAt(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
        => StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData);

    public static bool IsIdleGeneralAtStronghold(Character character, int strongholdId)
        => !character.IsDead
           && character.ForceStatus == CharacterForceStatus.Idle
           && character.RecruitTask is null
           && character.RecruitAssignment is null
           && character.LocationType == CharacterLocationType.Stronghold
           && character.LocationStrongholdId == strongholdId;

    public static IEnumerable<Character> ListAssignableGenerals(
        Stronghold stronghold,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            stronghold.ForceId,
            meta,
            gameData);

        return gameData.Characters.Values
            .Where(c => c.ForceId == stronghold.ForceId)
            .Where(c => c.Id != forceLordId)
            .Where(c => IsIdleGeneralAtStronghold(c, stronghold.Id))
            .OrderBy(c => c.Name);
    }

    public static bool TryValidateStrongholdMercenaryBudget(
        Stronghold stronghold,
        int budgetMoney,
        out GameError? error)
        => TryValidateMercenaryMoneyPool(stronghold.ForceActor.Money, budgetMoney, out error);

    public static bool TryValidatePersonalMercenaryBudget(
        Character character,
        int budgetMoney,
        out GameError? error)
        => TryValidateMercenaryMoneyPool(character.Money, budgetMoney, out error);

    /// <summary>领主/代官/当主在城内时可亲自执行据点军事/内政指令。</summary>
    public static bool CanExecutePersonalStrongholdCommands(
        Character character,
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (!StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData))
            return false;

        if (!IsIdleGeneralAtStronghold(character, stronghold.Id))
            return false;

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            stronghold.ForceId,
            meta,
            gameData);

        if (character.Id == forceLordId)
            return true;

        if (character.Id == stronghold.LordId)
            return true;

        return character.Id == stronghold.LeaderId;
    }

    private static bool TryValidateMercenaryMoneyPool(int availableMoney, int budgetMoney, out GameError? error)
    {
        error = null;
        if (budgetMoney < RecruitConstants.MinMercenaryBudget)
        {
            error = GameError.StrongholdError.InsufficientRecruitBudget;
            return false;
        }

        if (availableMoney < budgetMoney)
        {
            error = GameError.StrongholdError.InsufficientRecruitBudget;
            return false;
        }

        if (RecruitConstants.SoldiersAffordableByMoney(budgetMoney) <= 0)
        {
            error = GameError.StrongholdError.InsufficientRecruitBudget;
            return false;
        }

        return true;
    }

    public static bool TryValidateConscriptCapacity(Stronghold stronghold, out GameError? error)
    {
        error = null;
        if (stronghold.Population < RecruitConstants.PopulationCostPerSoldier)
        {
            error = GameError.StrongholdError.InsufficientRecruitResources;
            return false;
        }

        return true;
    }
}
