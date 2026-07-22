using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>当主被俘/阵亡后的势力继承与灭亡判定。</summary>
public static class ForceSuccessionRules
{
    /// <summary>当主离任后的势力结局类型。</summary>
    public enum LordRemovalReason
    {
        /// <summary>当主被俘并投降，势力灭亡。</summary>
        CapturedAndSurrendered,

        /// <summary>当主阵亡且无人抵抗，势力投降灭亡。</summary>
        KilledNoResistance,

        /// <summary>当主阵亡但继承人继位，势力继续抵抗。</summary>
        KilledWithSuccession
    }

    /// <summary>
    /// 当主被俘且投降 → 势力灭亡；
    /// 当主阵亡 → 仍有抵抗则继承，否则无人抵抗则势力投降（领地移交）。
    /// </summary>
    public static LordRemovalReason? TryResolveAfterLordRemoved(
        int forceId,
        int conquerorForceId,
        bool lordCaptured,
        bool lordKilled,
        GameData gameData,
        GameMasterData gameMasterData,
        StrategyScenarioMeta meta,
        StrategyForceLordRegistry lordRegistry,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer,
        int removedLordCharacterId = 0)
    {
        // 业务：当主被俘即视为势力投降灭亡，无需再判继承
        if (lordCaptured)
        {
            ApplyForceElimination(forceId, conquerorForceId, gameData, gameMasterData, dayOutcomeBuffer,
                "当主被俘投降，势力就此灭亡。");
            return LordRemovalReason.CapturedAndSurrendered;
        }

        if (!lordKilled)
            return null;

        // 业务：当主阵亡但仍有城或部队抵抗时，尝试指定继承人继位
        if (ForceResistanceRules.HasActiveResistance(forceId, gameData))
        {
            if (removedLordCharacterId > 0)
                StrongholdGovernanceRules.ReleaseGovernanceRoles(forceId, removedLordCharacterId, gameData);

            if (TryAppointSuccessor(forceId, gameData, meta, lordRegistry, dayOutcomeBuffer))
                return LordRemovalReason.KilledWithSuccession;

            return null;
        }

        // 业务：当主阵亡且无人愿意继续抵抗，势力整体投降
        ApplyForceElimination(forceId, conquerorForceId, gameData, gameMasterData, dayOutcomeBuffer,
            "当主阵亡且无人愿意继续抵抗，势力投降。");
        return LordRemovalReason.KilledNoResistance;
    }

    /// <summary>执行势力灭亡：领地移交征服者、残部溃散并记录事件。</summary>
    public static void ApplyForceElimination(
        int forceId,
        int conquerorForceId,
        GameData gameData,
        GameMasterData gameMasterData,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer,
        string reason)
    {
        var forceName = gameData.Forces.TryGetValue(forceId, out var force) ? force.Name : $"势力{forceId}";
        var conquerorName = gameData.Forces.TryGetValue(conquerorForceId, out var conqueror)
            ? conqueror.Name
            : $"势力{conquerorForceId}";

        foreach (var stronghold in gameData.Strongholds.Values.Where(s => s.ForceId == forceId).ToList())
            StrongholdCaptureActions.TransferStrongholdOwnership(
                stronghold,
                conquerorForceId,
                gameData,
                gameMasterData);

        foreach (var unit in gameData.Units.Values.Where(u => u.ForceId == forceId && u.IsMilitary).ToList())
        {
            unit.Soldier = 0;
            unit.Morale = 0;
            unit.Directive = UnitDirective.Retreat;
            unit.Stance = UnitStance.Normal;
        }

        dayOutcomeBuffer?.AddEvent(new StrategyEventDto
        {
            Category = "ForceEliminated",
            Brief = $"💀 {forceName} 灭亡",
            Message = $"{forceName}：{reason} 领地与残部归属 {conquerorName}。"
        });
    }

    /// <summary>从势力内选拔继承人并更新当主注册表。</summary>
    private static bool TryAppointSuccessor(
        int forceId,
        GameData gameData,
        StrategyScenarioMeta meta,
        StrategyForceLordRegistry lordRegistry,
        StrategyDayOutcomeBuffer? dayOutcomeBuffer)
    {
        if (!gameData.Forces.TryGetValue(forceId, out var force))
            return false;

        var successorId = ResolveSuccessorCharacterId(force, gameData);
        // 业务：无有效继承人（未指定、已阵亡或不在本势力）则继承失败
        if (successorId <= 0
            || !gameData.Characters.TryGetValue(successorId, out var successor)
            || successor.IsDead)
        {
            return false;
        }

        lordRegistry.SetLordCharacterId(forceId, successorId);

        // 业务：新当主不可兼任外臣领主或代官；原据点改回直辖/待任命
        StrongholdGovernanceRules.ReleaseGovernanceRoles(forceId, successorId, gameData);

        var forceName = force.Name;
        dayOutcomeBuffer?.AddEvent(new StrategyEventDto
        {
            Category = "LordSuccession",
            Brief = $"👑 {forceName} 继承",
            Message = $"{forceName} 当主阵亡，{successor.Name} 继任当主，势力继续抵抗。"
        });

        return true;
    }

    /// <summary>解析继承人：优先剧本指定继承人，否则按官职与声望最高者。</summary>
    private static int ResolveSuccessorCharacterId(Force force, GameData gameData)
    {
        // 业务：剧本预指定继承人且存活、仍属本势力时优先继位
        if (force.Successor is int designated
            && gameData.Characters.TryGetValue(designated, out var designatedChar)
            && !designatedChar.IsDead
            && designatedChar.ForceId == force.Id)
        {
            return designated;
        }

        // 业务：无指定继承人时，按官职（Position）与声望（Popular）降序选首位
        return gameData.Characters.Values
            .Where(c => c.ForceId == force.Id && !c.IsDead)
            .OrderByDescending(c => c.Position)
            .ThenByDescending(c => c.Popular)
            .Select(c => c.Id)
            .FirstOrDefault();
    }
}
