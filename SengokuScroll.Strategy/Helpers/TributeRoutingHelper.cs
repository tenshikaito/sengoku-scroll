using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 解析据点月度贡纳目的地：内藩领地→内藩居城；内藩居城/直辖/有领主据点→宗主当主居城。
/// </summary>
public static class TributeRoutingHelper
{
    /// <summary>
    /// 若该据点应派出贡纳运输队，返回目标据点 Id；否则 null。
    /// </summary>
    public static int? ResolveTributeDestinationStrongholdId(
        Stronghold origin,
        GameData gameData,
        StrategyScenarioMeta meta)
    {
        if (!gameData.Forces.TryGetValue(origin.ForceId, out var originForce))
            return null;

        var originResidenceId = StrategyLordHelper.ResolveLordResidenceStrongholdId(
            origin.ForceId,
            gameData,
            meta);

        if (origin.Id == originResidenceId)
        {
            var suzerainRootId = ResolveSuzerainRootForceId(originForce, gameData);
            if (suzerainRootId is null || suzerainRootId == origin.ForceId)
                return null;

            return StrategyLordHelper.ResolveLordResidenceStrongholdId(
                suzerainRootId.Value,
                gameData,
                meta);
        }

        return originResidenceId;
    }

    /// <summary>沿宗主链上溯至独立根势力 Id。</summary>
    public static int ResolveRealmRootForceId(int forceId, GameData gameData)
    {
        var visited = new HashSet<int>();
        var currentId = forceId;

        while (true)
        {
            if (!visited.Add(currentId))
                break;

            if (!gameData.Forces.TryGetValue(currentId, out var force))
                break;

            if (force.Status != Force.ForceStatus.InnerVassal
                || force.SuzerainForceId is not int suzerainId
                || suzerainId <= 0)
            {
                return currentId;
            }

            currentId = suzerainId;
        }

        return forceId;
    }

    private static int? ResolveSuzerainRootForceId(Force force, GameData gameData)
    {
        if (force.Status != Force.ForceStatus.InnerVassal
            || force.SuzerainForceId is not int suzerainId
            || suzerainId <= 0)
        {
            return null;
        }

        return ResolveRealmRootForceId(suzerainId, gameData);
    }
}
