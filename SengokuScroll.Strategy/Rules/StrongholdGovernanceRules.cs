using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Rules;

/// <summary>据点领主与代官职务的任免约束与离任清理。</summary>
public static class StrongholdGovernanceRules
{
    /// <summary>当主可否对本家直属据点发布方针（不含旗下内藩）。</summary>
    public static bool CanPlayerConfigureGovernancePolicy(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
        => StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, meta, gameData);

    /// <summary>旗下内藩势力据点（宗主为玩家当主势力）。</summary>
    public static bool IsInnerVassalRealmStrongholdUnderPlayer(
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(stronghold.ForceId, out var force))
            return false;

        return force.Status == Force.ForceStatus.InnerVassal
               && force.SuzerainForceId == meta.PlayerForceId;
    }

    /// <summary>领主以外、城内待命、可接任务令的将领。</summary>
    public static IEnumerable<Character> ListGovernanceAssignableGenerals(
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
            .Where(c => c.Id != forceLordId && c.Id != stronghold.LordId)
            .Where(c => StrongholdRecruitTaskRules.IsIdleGeneralAtStronghold(c, stronghold.Id))
            .OrderByDescending(c => c.Charm)
            .ThenBy(c => c.Name);
    }

    /// <summary>解除角色在本势力全部据点的领主/代官职务（离任、阵亡、继位时调用）。</summary>
    public static void ReleaseGovernanceRoles(int forceId, int characterId, GameData gameData)
    {
        if (characterId <= 0)
            return;

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != forceId)
                continue;

            if (stronghold.LordId == characterId)
                stronghold.LordId = 0;

            if (stronghold.LeaderId == characterId)
                stronghold.LeaderId = 0;
        }
    }
}
