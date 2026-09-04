using System.Diagnostics.CodeAnalysis;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>据点人事：将领调动、召回等。</summary>
public static class StrongholdPersonnelActions
{
    /// <summary>
    /// 将其它据点的待命将领召集至目标据点。
    /// 不可调动领主、代官或已有任务者。
    /// </summary>
    public static GameError? TryTransferCharacter(
        Stronghold target,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!TryValidateTransferCandidate(target, characterId, gameData, meta, out var character, out var error))
            return error;

        if (character.LocationStrongholdId == target.Id)
            return GameError.DomesticError.CharacterAlreadyAtStronghold;

        return ScheduleTransferTravel(character, target);
    }

    /// <summary>
    /// 从当前据点派遣待命将领至目标据点。
    /// 不可调动领主、代官或已有任务者。
    /// </summary>
    public static GameError? TryDispatchCharacter(
        Stronghold origin,
        Stronghold destination,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (origin.Id == destination.Id)
            return GameError.DomesticError.CharacterAlreadyAtStronghold;

        if (!TryValidateTransferCandidate(origin, characterId, gameData, meta, out var character, out var error))
            return error;

        if (character.LocationStrongholdId != origin.Id)
            return GameError.DomesticError.CharacterNotAtStronghold;

        if (!StrongholdDomesticRules.IsPlayerRealmStronghold(destination, meta, gameData))
            return GameError.DiplomacyError.NotSelfForce;

        if (destination.ForceId != origin.ForceId)
            return GameError.DiplomacyError.NotSelfForce;

        return ScheduleTransferTravel(character, destination);
    }

    /// <summary>校验将领是否可被召回（外派任务中）。</summary>
    public static bool HasRecallableTask(Character character)
    {
        if (character.IsDead)
            return false;

        if (character.RecruitTask is not null || character.RecruitAssignment is not null)
            return true;

        return character.ForceStatus == CharacterForceStatus.Task
            && character.ActionPlan == CharacterActionPlan.Task;
    }

    /// <summary>召回外派将领：结算任务并令其尽快回城。</summary>
    public static GameError? ApplyCharacterRecall(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!HasRecallableTask(character))
            return GameError.DomesticError.CharacterNotOnRecallableTask;

        var returnStrongholdId = ResolveRecallReturnStrongholdId(character, gameData, meta);

        if (character.RecruitAssignment is not null)
        {
            StrongholdRecruitTaskActions.AbortRecruitAssignmentForRecall(character, gameData);
        }
        else if (character.RecruitTask is not null)
        {
            returnStrongholdId = StrongholdRecruitTaskActions.SettleRecruitTaskOnRecall(
                character,
                gameData,
                meta);
        }
        else
        {
            ClearGovernanceTravelState(character);
        }

        if (returnStrongholdId > 0
            && gameData.Strongholds.TryGetValue(returnStrongholdId, out var returnStronghold)
            && !CharacterAiRules.IsAtStronghold(character, returnStronghold))
        {
            CharacterAiMovementHelper.ScheduleGovernanceTravelTask(character, returnStronghold);
        }
        else
        {
            character.ForceStatus = CharacterForceStatus.Idle;
            character.ActionPlan = CharacterActionPlan.Rest;
            character.ActionStatus = CharacterActionStatus.Waiting;
            character.ActionTarget.StrongholdId = 0;
            character.ActionTarget.RoutePoints.Clear();
        }

        return null;
    }

    /// <summary>解析将领当前所在地图格（用于信使投递）。</summary>
    public static Point3 ResolveCharacterDeliveryLocation(Character character, GameData gameData)
    {
        if (character.LocationType == CharacterLocationType.Stronghold
            && character.LocationStrongholdId > 0
            && gameData.Strongholds.TryGetValue(character.LocationStrongholdId, out var stronghold))
        {
            return stronghold.Location;
        }

        return character.Location;
    }

    private static bool TryValidateTransferCandidate(
        Stronghold commandStronghold,
        int characterId,
        GameData gameData,
        StrategyScenarioMeta meta,
        [NotNullWhen(true)] out Character? character,
        out GameError? error)
    {
        character = null;
        error = null;

        if (!StrongholdDomesticRules.IsPlayerRealmStronghold(commandStronghold, meta, gameData))
        {
            error = GameError.DiplomacyError.NotSelfForce;
            return false;
        }

        if (!StrongholdDomesticRules.CanLordCommandAtStronghold(meta, gameData, commandStronghold))
        {
            error = GameError.DomesticError.LordNotAtResidence;
            return false;
        }

        if (characterId <= 0 || !gameData.Characters.TryGetValue(characterId, out var resolved) || resolved.IsDead)
        {
            error = GameError.DataNotFound;
            return false;
        }

        character = resolved;

        if (character.ForceId != commandStronghold.ForceId)
        {
            error = GameError.DiplomacyError.NotSelfForce;
            return false;
        }

        var forceLordId = StrategyStrongholdLordHelper.ResolveForceLordCharacterId(
            commandStronghold.ForceId,
            meta,
            gameData);

        if (characterId == forceLordId)
        {
            error = GameError.DomesticError.CharacterIsForceLord;
            return false;
        }

        if (IsCharacterLordAnywhere(gameData, characterId, commandStronghold.ForceId))
        {
            error = GameError.DomesticError.CharacterIsStrongholdLord;
            return false;
        }

        if (IsCharacterMayorAnywhere(gameData, characterId, commandStronghold.ForceId))
        {
            error = GameError.DomesticError.CharacterIsStrongholdMayor;
            return false;
        }

        if (character.RecruitTask is not null
            || character.RecruitAssignment is not null
            || character.ForceStatus != CharacterForceStatus.Idle)
        {
            error = GameError.DomesticError.CharacterHasActiveTask;
            return false;
        }

        if (character.LocationType != CharacterLocationType.Stronghold
            || character.LocationStrongholdId <= 0)
        {
            error = GameError.DomesticError.CharacterNotAtStronghold;
            return false;
        }

        if (!gameData.Strongholds.TryGetValue(character.LocationStrongholdId, out var currentStronghold)
            || !StrongholdDomesticRules.IsPlayerRealmStronghold(currentStronghold, meta, gameData))
        {
            error = GameError.DomesticError.CharacterNotAtStronghold;
            return false;
        }

        return true;
    }

    private static GameError? ScheduleTransferTravel(Character character, Stronghold destination)
    {
        if (CharacterAiRules.IsAtStronghold(character, destination))
        {
            character.StrongholdId = destination.Id;
            character.ForceStatus = CharacterForceStatus.Idle;
            character.ActionPlan = CharacterActionPlan.Rest;
            character.ActionStatus = CharacterActionStatus.Waiting;
            return null;
        }

        CharacterAiMovementHelper.ScheduleGovernanceTravelTask(character, destination);
        return null;
    }

    private static int ResolveRecallReturnStrongholdId(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (character.RecruitTask is not null && character.RecruitTask.ReportStrongholdId > 0)
            return character.RecruitTask.ReportStrongholdId;

        if (character.RecruitAssignment is not null
            && gameData.Strongholds.TryGetValue(character.RecruitAssignment.StrongholdId, out var assignmentTarget))
        {
            return StrategyLordHelper.ResolveLordResidenceStrongholdId(
                assignmentTarget.ForceId,
                gameData,
                meta);
        }

        if (character.LocationStrongholdId > 0)
            return character.LocationStrongholdId;

        return StrategyLordHelper.ResolveLordResidenceStrongholdId(character.ForceId, gameData, meta);
    }

    private static void ClearGovernanceTravelState(Character character)
    {
        character.ForceStatus = CharacterForceStatus.Idle;
        character.ActionPlan = CharacterActionPlan.Rest;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.ActionTarget.StrongholdId = 0;
        character.ActionTarget.RoutePoints.Clear();
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

    private static bool IsCharacterMayorAnywhere(GameData gameData, int characterId, int forceId)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId == forceId && stronghold.LeaderId == characterId)
                return true;
        }

        return false;
    }
}
