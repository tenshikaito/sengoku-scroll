using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using static SengokuScroll.Domain.Entities.Character;
using static SengokuScroll.Domain.Entities.Diplomacy;
using static SengokuScroll.Domain.Entities.Force;

namespace SengokuScroll.Strategy.Rules;

/// <summary>外交使节任务成功率、行程与可派遣将领校验。</summary>
public static class DiplomacyMissionRules
{
    public const int DefaultTravelDays = 7;

    /// <summary>估算使节抵达目标并完成交涉所需日数。</summary>
    public static int EstimateTravelDays(
        GameData gameData,
        StrategyScenarioMeta meta,
        int playerForceId,
        int targetForceId)
    {
        var playerResidenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            playerForceId,
            gameData,
            meta);
        var targetResidenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            targetForceId,
            gameData,
            meta);

        if (playerResidenceId <= 0
            || targetResidenceId <= 0
            || !gameData.Strongholds.TryGetValue(playerResidenceId, out var playerResidence)
            || !gameData.Strongholds.TryGetValue(targetResidenceId, out var targetResidence))
        {
            return DefaultTravelDays;
        }

        var distance = Math.Abs(playerResidence.Location.X - targetResidence.Location.X)
            + Math.Abs(playerResidence.Location.Y - targetResidence.Location.Y);

        // 业务：默认 7 日；距离每 3 格再加 1 日
        return Math.Max(DefaultTravelDays, DefaultTravelDays + distance / 3);
    }

    /// <summary>估算外交使节任务成功率（5–95）。</summary>
    public static int EstimateSuccessChancePercent(
        Character envoy,
        GameData gameData,
        int playerForceId,
        int targetForceId,
        string action)
    {
        var baseChance = envoy.Politics * 0.4
            + envoy.Charm * 0.3
            + envoy.Leadership * 0.1;

        var relation = ResolveRelation(gameData, playerForceId, targetForceId);
        var modifier = NormalizeAction(action) switch
        {
            "War" when relation == DiplomacyRelation.Allied => -25,
            "War" when relation == DiplomacyRelation.Enemy => -10,
            "Ally" when relation == DiplomacyRelation.Enemy => -20,
            "Ally" when relation == DiplomacyRelation.Allied => -30,
            "Peace" when relation == DiplomacyRelation.Neutral => -5,
            _ => 0,
        };

        var relationshipValue = ResolveRelationshipValue(gameData, playerForceId, targetForceId);
        var valueModifier = relationshipValue / 5.0;

        var total = baseChance + modifier + valueModifier;
        return (int)Math.Clamp(Math.Round(total), 5, 95);
    }

    public static IEnumerable<Character> ListAssignableEnvoys(
        GameData gameData,
        StrategyScenarioMeta meta,
        int playerForceId)
    {
        var residenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            playerForceId,
            gameData,
            meta);
        if (residenceId <= 0)
            yield break;

        foreach (var character in gameData.Characters.Values
                     .Where(c => c.ForceId == playerForceId)
                     .Where(c => IsIdleEnvoyAtStronghold(c, residenceId))
                     .OrderBy(c => c.Name))
        {
            yield return character;
        }
    }

    public static bool IsIdleEnvoyAtStronghold(Character character, int strongholdId)
        => !character.IsDead
           && character.ForceStatus == CharacterForceStatus.Idle
           && character.RecruitTask is null
           && character.RecruitAssignment is null
           && character.DiplomacyMission is null
           && character.LocationType == CharacterLocationType.Stronghold
           && character.LocationStrongholdId == strongholdId;

    public static bool CanAssignMissionTarget(
        GameData gameData,
        StrategyScenarioMeta meta,
        int playerForceId,
        int targetForceId,
        string? action,
        out GameError? error)
    {
        error = null;
        if (playerForceId == targetForceId)
        {
            error = GameError.DiplomacyError.SelfForce;
            return false;
        }

        if (!gameData.Forces.TryGetValue(targetForceId, out var target))
        {
            error = GameError.DiplomacyError.InvalidForce;
            return false;
        }

        // 业务：不可对本家内藩派遣外交使节
        if (target.Status == ForceStatus.InnerVassal && target.SuzerainForceId == playerForceId)
        {
            error = GameError.DiplomacyError.InvalidForce;
            return false;
        }

        if (target.Status != ForceStatus.Independence && target.Status != ForceStatus.OuterVassal)
        {
            error = GameError.DiplomacyError.InvalidForce;
            return false;
        }

        if (TryParseAction(action ?? string.Empty, out var normalizedAction)
            && !IsMissionAllowedForDiplomacyStatus(
                normalizedAction,
                ResolveRelation(gameData, playerForceId, targetForceId),
                out error))
        {
            return false;
        }

        if (normalizedAction == "War"
            && gameData.Forces.TryGetValue(playerForceId, out var actingForce)
            && actingForce.Diplomacies.Any(d => d.TargetForceId == targetForceId && d.IsTruce))
        {
            error = GameError.DiplomacyError.InvalidForce;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 可否派遣使节仅取决于外交状态（Neutral/Allied/Enemy），与 Relationship、Trust 等亲疏/信赖数值无关。
    /// </summary>
    public static bool IsMissionAllowedForDiplomacyStatus(
        string normalizedAction,
        DiplomacyRelation status,
        out GameError? error)
    {
        error = null;
        switch (normalizedAction)
        {
            case "War" when status == DiplomacyRelation.Enemy:
                error = GameError.DiplomacyError.EnemyForce;
                return false;
            case "Ally" when status == DiplomacyRelation.Allied:
                error = GameError.DiplomacyError.AllyForce;
                return false;
            case "Peace" when status != DiplomacyRelation.Enemy:
                error = GameError.DiplomacyError.NotEnemyForce;
                return false;
            default:
                return true;
        }
    }

    public static bool TryParseAction(string action, out string normalizedAction)
    {
        normalizedAction = NormalizeAction(action);
        return normalizedAction is "Ally" or "War" or "Peace";
    }

    public static DiplomacyRelation ResolveTargetRelation(string normalizedAction)
        => normalizedAction switch
        {
            "Ally" => DiplomacyRelation.Allied,
            "War" => DiplomacyRelation.Enemy,
            _ => DiplomacyRelation.Neutral,
        };

    public static int ComputeMissionRoll(GameData gameData, int characterId)
        => DeterministicHash.Combine(
            gameData.SimulationSeed,
            characterId,
            gameData.GameDate.Year,
            gameData.GameDate.Month,
            gameData.GameDate.Day) % 100;

    public static bool RollMissionSuccess(CharacterDiplomacyMission mission, GameData gameData, int characterId)
        => ComputeMissionRoll(gameData, characterId) < mission.SuccessChancePercent;

    private static DiplomacyRelation ResolveRelation(GameData gameData, int forceId, int targetForceId)
    {
        if (!gameData.Forces.TryGetValue(forceId, out var force))
            return DiplomacyRelation.Neutral;

        return force.Diplomacies.FirstOrDefault(d => d.TargetForceId == targetForceId)?.Relation
            ?? DiplomacyRelation.Neutral;
    }

    private static int ResolveRelationshipValue(GameData gameData, int forceId, int targetForceId)
    {
        if (!gameData.Forces.TryGetValue(forceId, out var force))
            return 0;

        return force.Diplomacies.FirstOrDefault(d => d.TargetForceId == targetForceId)?.Relationship ?? 0;
    }

    private static string NormalizeAction(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return string.Empty;

        return action.Trim() switch
        {
            var value when value.Equals("Ally", StringComparison.OrdinalIgnoreCase) => "Ally",
            var value when value.Equals("War", StringComparison.OrdinalIgnoreCase) => "War",
            var value when value.Equals("Peace", StringComparison.OrdinalIgnoreCase) => "Peace",
            _ => string.Empty,
        };
    }
}
