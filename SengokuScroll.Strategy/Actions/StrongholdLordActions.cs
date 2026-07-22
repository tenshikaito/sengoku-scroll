using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点领主任命与直辖切换。</summary>
public static class StrongholdLordActions
{
    /// <summary>
    /// 任命领主或设为直辖：<paramref name="characterId"/> 为当主 Id 时表示直辖（LordId=0）。
    /// 任命将领时将其所属据点改为目标并寻路前往；原城代职务自动解除，原领主据点改为直辖。
    /// </summary>
    public static GameError? TryAppointLord(
        Stronghold target,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta,
        IGameWorldContext worldContext,
        IPathfindingService pathfinding,
        StrategyForceLordRegistry? lordRegistry = null)
    {
        if (!StrongholdDomesticRules.IsPlayerRealmStronghold(target, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, target))
            return GameError.DomesticError.LordNotAtResidence;

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            target.ForceId,
            meta,
            gameData,
            lordRegistry);
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            target.ForceId,
            gameData,
            meta);

        if (target.Id == residenceId)
            return GameError.StrongholdError.CannotAppointLordToResidence;

        if (characterId <= 0 || characterId == forceLordId)
        {
            target.LordId = 0;
            return null;
        }

        if (!gameData.Characters.TryGetValue(characterId, out var character) || character.IsDead)
            return GameError.DataNotFound;

        if (character.ForceId != target.ForceId)
            return GameError.DiplomacyError.NotSelfForce;

        if (characterId == forceLordId)
        {
            target.LordId = 0;
            return null;
        }

        if (residenceId <= 0
            || !IsCharacterAtStronghold(character, residenceId))
        {
            return GameError.DomesticError.CharacterNotAtResidence;
        }

        ReleaseOtherGovernanceRoles(gameData, characterId, target.ForceId, target, appointAsLord: true);
        target.LordId = characterId;

        if (CharacterAiRules.IsAtStronghold(character, target))
        {
            StrategyStrongholdLordHelper.EnsureLordResidence(target, character);
            character.ForceStatus = CharacterForceStatus.Idle;
            character.ActionPlan = CharacterActionPlan.Rest;
            character.ActionStatus = CharacterActionStatus.Waiting;
            return null;
        }

        CharacterAiMovementHelper.ScheduleGovernanceTravelTask(character, target);
        return null;
    }

    /// <summary>任命据点代官；将领须先在当主居城，随后前往目标据点赴任。已担任领主者不可兼任代官。</summary>
    public static GameError? TryAppointMayor(
        Stronghold target,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta,
        IGameWorldContext worldContext,
        IPathfindingService pathfinding,
        StrategyForceLordRegistry? lordRegistry = null)
    {
        if (!StrongholdDomesticRules.IsPlayerRealmStronghold(target, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, target))
            return GameError.DomesticError.LordNotAtResidence;

        if (characterId <= 0)
            return GameError.DataNotFound;

        if (!gameData.Characters.TryGetValue(characterId, out var character) || character.IsDead)
            return GameError.DataNotFound;

        if (character.ForceId != target.ForceId)
            return GameError.DiplomacyError.NotSelfForce;

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            target.ForceId,
            meta,
            gameData,
            lordRegistry);
        if (characterId == forceLordId)
            return GameError.DomesticError.CharacterIsForceLord;

        if (IsCharacterLordAnywhere(gameData, characterId, target.ForceId))
            return GameError.DomesticError.CharacterIsStrongholdLord;

        if (target.LordId == characterId)
            return GameError.DomesticError.CharacterIsStrongholdLord;

        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            target.ForceId,
            gameData,
            meta);

        if (residenceId <= 0
            || !IsCharacterAtStronghold(character, residenceId))
        {
            return GameError.DomesticError.CharacterNotAtResidence;
        }

        ReleaseOtherGovernanceRoles(gameData, characterId, target.ForceId, target, appointAsLord: false);
        target.LeaderId = characterId;

        if (CharacterAiRules.IsAtStronghold(character, target))
        {
            character.ForceStatus = CharacterForceStatus.Idle;
            character.ActionPlan = CharacterActionPlan.Rest;
            character.ActionStatus = CharacterActionStatus.Waiting;
            return null;
        }

        CharacterAiMovementHelper.ScheduleGovernanceTravelTask(character, target);
        return null;
    }

    /// <summary>解除角色在本势力其它据点的城主/城代职务；城代转任领主时允许，领主不可转任城代。</summary>
    private static void ReleaseOtherGovernanceRoles(
        GameData gameData,
        int characterId,
        int forceId,
        Stronghold target,
        bool appointAsLord)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != forceId)
                continue;

            if (stronghold.Id == target.Id)
            {
                if (appointAsLord && stronghold.LeaderId == characterId)
                    stronghold.LeaderId = 0;
                else if (!appointAsLord && stronghold.LordId == characterId)
                    stronghold.LordId = 0;
                continue;
            }

            if (stronghold.LordId == characterId)
                stronghold.LordId = 0;
            if (stronghold.LeaderId == characterId)
                stronghold.LeaderId = 0;
        }
    }

    private static bool IsCharacterLordAnywhere(GameData gameData, int characterId, int forceId)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId == forceId && stronghold.LordId == characterId)
                return true;
        }

        return false;
    }

    private static bool IsCharacterAtStronghold(Character character, int strongholdId)
    {
        if (character.LocationType == CharacterLocationType.Stronghold)
        {
            var at = character.LocationStrongholdId > 0
                ? character.LocationStrongholdId
                : character.StrongholdId;
            return at == strongholdId;
        }

        return false;
    }
}
