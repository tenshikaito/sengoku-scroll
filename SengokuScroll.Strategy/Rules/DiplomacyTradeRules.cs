using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;

namespace SengokuScroll.Strategy.Rules;

/// <summary>势力间贸易/通行外交判定（M4-d）。</summary>
public static class DiplomacyTradeRules
{
    /// <summary>两势力是否允许开展贸易（同势力或双方非敌对）。</summary>
    public static bool CanTradeForces(int forceAId, int forceBId, GameData gameData)
    {
        // 业务：同势力内部贸易始终允许
        if (forceAId == forceBId)
            return true;

        // 业务：外交关系为敌对时禁止贸易
        return !TransportRules.IsHostileForcePublic(forceAId, forceBId, gameData);
    }

    /// <summary>两势力是否为同盟关系。</summary>
    public static bool AreAllied(int forceAId, int forceBId, GameData gameData)
    {
        if (!gameData.Forces.TryGetValue(forceAId, out var forceA))
            return false;

        return forceA.Diplomacies.Any(d =>
            d.TargetForceId == forceBId && d.Relation == Diplomacy.DiplomacyRelation.Allied);
    }
}
