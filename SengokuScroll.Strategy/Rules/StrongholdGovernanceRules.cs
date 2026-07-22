using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Strategy.Rules;

/// <summary>据点领主与代官职务的任免约束与离任清理。</summary>
public static class StrongholdGovernanceRules
{
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
