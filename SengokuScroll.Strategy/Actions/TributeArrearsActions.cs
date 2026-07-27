using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Actions;

/// <summary>贡赋/钱纳运送不足时写入欠账（M4-c/d）。</summary>
public static class TributeArrearsActions
{
    /// <summary>贡纳/税赋运送不足时，记入宗主外交欠账或势力内部欠账。</summary>
    public static void AccrueShortfall(
        GameData gameData,
        Stronghold origin,
        int foodShortfallGo,
        int moneyShortfall)
    {
        if (foodShortfallGo <= 0 && moneyShortfall <= 0)
            return;

        if (!gameData.Forces.TryGetValue(origin.ForceId, out var originForce))
            return;

        var suzerainForceId = ResolveSuzerainForceId(originForce);
        if (suzerainForceId is int suzerainId and > 0 && suzerainId != origin.ForceId)
        {
            AccrueDiplomacyShortfall(originForce, suzerainId, foodShortfallGo, moneyShortfall);
            return;
        }

        originForce.InternalArrearsFoodGo += Math.Max(0, foodShortfallGo);
        originForce.InternalArrearsMoney += Math.Max(0, moneyShortfall);
    }

    /// <summary>运输 Unit 被毁时，将未送达的贡纳/税赋记为欠账。</summary>
    public static void AccrueUndeliveredConvoy(Unit transport, GameData gameData)
    {
        if (transport.TransportPurpose is not (TransportPurpose.Tribute or TransportPurpose.TaxMoney))
            return;

        if (!gameData.Strongholds.TryGetValue(transport.TransportOriginStrongholdId, out var origin))
            return;

        AccrueShortfall(gameData, origin, transport.Food, transport.Money);
    }

    private static void AccrueDiplomacyShortfall(
        Force subordinateForce,
        int suzerainForceId,
        int foodShortfallGo,
        int moneyShortfall)
    {
        var diplomacy = subordinateForce.Diplomacies
            .FirstOrDefault(d => d.TargetForceId == suzerainForceId);

        if (diplomacy is null)
            return;

        diplomacy.ArrearsFoodGo += Math.Max(0, foodShortfallGo);
        diplomacy.ArrearsMoney += Math.Max(0, moneyShortfall);
    }

    private static int? ResolveSuzerainForceId(Force originForce)
        => originForce.SuzerainForceId is int suzerainId and > 0 ? suzerainId : null;
}
