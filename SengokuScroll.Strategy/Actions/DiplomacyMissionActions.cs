using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Character;

namespace SengokuScroll.Strategy.Actions;

/// <summary>外交使节任务派发与结算。</summary>
public static class DiplomacyMissionActions
{
    public static bool TryAssignMission(
        GameData gameData,
        StrategyScenarioMeta meta,
        int characterId,
        int targetForceId,
        string action,
        out GameError? error)
        => TryAssignMission(
            gameData,
            meta,
            characterId,
            targetForceId,
            action,
            peaceTerms: null,
            out error);

    public static bool TryAssignMission(
        GameData gameData,
        StrategyScenarioMeta meta,
        int characterId,
        int targetForceId,
        string action,
        PeaceSettlementTerms? peaceTerms,
        out GameError? error)
        => TryAssignMissionForForce(
            gameData,
            meta,
            meta.PlayerForceId,
            characterId,
            targetForceId,
            action,
            peaceTerms,
            out error);

    public static bool TryAssignMissionForForce(
        GameData gameData,
        StrategyScenarioMeta meta,
        int actingForceId,
        int characterId,
        int targetForceId,
        string action,
        out GameError? error)
        => TryAssignMissionForForce(
            gameData,
            meta,
            actingForceId,
            characterId,
            targetForceId,
            action,
            peaceTerms: null,
            out error);

    public static bool TryAssignMissionForForce(
        GameData gameData,
        StrategyScenarioMeta meta,
        int actingForceId,
        int characterId,
        int targetForceId,
        string action,
        PeaceSettlementTerms? peaceTerms,
        out GameError? error)
    {
        error = null;

        if (!DiplomacyMissionRules.TryParseAction(action, out var normalizedAction))
        {
            error = GameError.DiplomacyError.InvalidForce;
            return false;
        }

        if (!DiplomacyMissionRules.CanAssignMissionTarget(gameData, meta, actingForceId, targetForceId, normalizedAction, out error))
            return false;

        if (!TryResolveAssignableEnvoy(gameData, meta, actingForceId, characterId, out var character, out error))
            return false;

        var travelDays = DiplomacyMissionRules.EstimateTravelDays(
            gameData,
            meta,
            actingForceId,
            targetForceId);
        var successChance = DiplomacyMissionRules.EstimateSuccessChancePercent(
            character,
            gameData,
            actingForceId,
            targetForceId,
            normalizedAction);

        StrategyPeaceSettlementPreviewDto? peacePreview = null;
        if (normalizedAction == "Peace")
        {
            peaceTerms ??= new PeaceSettlementTerms();
            if (!PeaceSettlementRules.TryBuildPreview(
                    gameData,
                    actingForceId,
                    targetForceId,
                    peaceTerms,
                    successChance,
                    out peacePreview,
                    out error))
            {
                return false;
            }

            successChance = peacePreview.AcceptanceChancePercent;
        }

        character.DiplomacyMission = new CharacterDiplomacyMission
        {
            Action = normalizedAction,
            TargetForceId = targetForceId,
            RemainingDays = travelDays,
            SuccessChancePercent = successChance,
            PeaceTerms = normalizedAction == "Peace" ? peaceTerms : null,
        };
        character.ForceStatus = CharacterForceStatus.Task;
        character.ActionPlan = CharacterActionPlan.Task;
        character.ActionStatus = CharacterActionStatus.Acting;
        character.LastAiCheckDate = default;

        UpsertIntelTask(character, gameData, meta, normalizedAction, targetForceId, travelDays);
        return true;
    }

    public static void ProcessDailyMission(
        Character character,
        GameData gameData,
        GameMasterData gameMasterData,
        StrategyScenarioMeta meta)
    {
        var mission = character.DiplomacyMission;
        if (mission is null)
            return;

        mission.RemainingDays--;
        UpdateIntelTaskRemaining(character, gameData, meta, mission);

        if (mission.RemainingDays > 0)
            return;

        var playerForceId = character.ForceId;
        var succeeded = DiplomacyMissionRules.RollMissionSuccess(mission, gameData, character.Id);
        if (succeeded && mission.Action == "Peace")
        {
            PeaceSettlementActions.TryExecute(
                gameData,
                gameMasterData,
                playerForceId,
                mission.TargetForceId,
                mission.PeaceTerms ?? new PeaceSettlementTerms(),
                out _);
        }
        else if (succeeded && ForceDiplomacyActions.TrySetRelation(
                     gameData,
                     playerForceId,
                     mission.TargetForceId,
                     DiplomacyMissionRules.ResolveTargetRelation(mission.Action),
                     out _))
        {
            // 业务：成功则写入目标关系
        }

        ClearMissionState(character);
    }

    private static bool TryResolveAssignableEnvoy(
        GameData gameData,
        StrategyScenarioMeta meta,
        int playerForceId,
        int characterId,
        out Character character,
        out GameError? error)
    {
        character = null!;
        error = null;

        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            playerForceId,
            gameData,
            meta);
        if (residenceId <= 0)
        {
            error = GameError.DomesticError.LordNotAtResidence;
            return false;
        }

        if (characterId <= 0 || !gameData.Characters.TryGetValue(characterId, out var resolved) || resolved.IsDead)
        {
            error = GameError.DataNotFound;
            return false;
        }

        if (resolved.ForceId != playerForceId)
        {
            error = GameError.DiplomacyError.NotSelfForce;
            return false;
        }

        if (!DiplomacyMissionRules.IsIdleEnvoyAtStronghold(resolved, residenceId))
        {
            error = resolved.DiplomacyMission is not null
                    || resolved.RecruitTask is not null
                    || resolved.RecruitAssignment is not null
                    || resolved.ForceStatus != CharacterForceStatus.Idle
                ? GameError.DomesticError.CharacterHasActiveTask
                : GameError.DomesticError.CharacterNotAtResidence;
            return false;
        }

        character = resolved;
        return true;
    }

    private static void UpsertIntelTask(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        string action,
        int targetForceId,
        int travelDays)
    {
        var targetName = gameData.Forces.TryGetValue(targetForceId, out var target)
            ? target.Name
            : $"势力#{targetForceId}";

        character.IntelTasks.RemoveAll(t => t.TaskCategory == "Force" && t.Name == ResolveMissionLabel(action));
        character.IntelTasks.Add(new CharacterIntelTask
        {
            TaskCategory = "Force",
            Name = ResolveMissionLabel(action),
            Target = targetName,
            Status = "出使中",
            Remaining = $"{travelDays}日",
        });
    }

    private static void UpdateIntelTaskRemaining(
        Character character,
        GameData gameData,
        StrategyScenarioMeta meta,
        CharacterDiplomacyMission mission)
    {
        var task = character.IntelTasks.FirstOrDefault(t =>
            t.TaskCategory == "Force" && t.Name == ResolveMissionLabel(mission.Action));
        if (task is null)
            return;

        task.Remaining = mission.RemainingDays > 0 ? $"{mission.RemainingDays}日" : "—";
        task.Status = mission.RemainingDays > 0 ? "出使中" : "返程";
    }

    private static void ClearMissionState(Character character)
    {
        character.DiplomacyMission = null;
        character.ForceStatus = CharacterForceStatus.Idle;
        character.ActionPlan = CharacterActionPlan.Rest;
        character.ActionStatus = CharacterActionStatus.Waiting;
        character.ActionTarget.StrongholdId = 0;
        character.ActionTarget.RoutePoints.Clear();
        character.IntelTasks.RemoveAll(t =>
            t.TaskCategory == "Force"
            && t.Name is "同盟" or "宣战" or "议和");
    }

    private static string ResolveMissionLabel(string action)
        => action switch
        {
            "Ally" => "同盟",
            "War" => "宣战",
            _ => "议和",
        };
}
