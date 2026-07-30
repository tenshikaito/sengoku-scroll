using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>邻城价差扣运费后的套利挂价锚点（削峰填谷）。</summary>
public static class MarketRegionalArbitrageHelper
{
    /// <summary>
    /// 出口卖价下限：邻城中枢 − 运费后仍高于本城中枢时，取最优到岸卖价。
    /// 无机会时返回 0。
    /// </summary>
    public static int ResolveExportAskFloor(
        Stronghold origin,
        GameData gameData,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        var originRef = MarketMakerAiHelper.ResolveReferencePrice(origin, commodity);
        if (originRef <= 0)
            return 0;

        var best = 0;
        foreach (var other in gameData.Strongholds.Values)
        {
            if (other.Id == origin.Id)
                continue;
            if (!MarketRules.CanTrade(other, gameData))
                continue;
            if (!DiplomacyTradeRules.CanTradeForces(origin.ForceId, other.ForceId, gameData))
                continue;

            var distance = Manhattan(origin, other);
            if (distance <= 0 || distance > MarketConstants.RegionalArbitrageMaxDistanceTiles)
                continue;

            var otherRef = MarketMakerAiHelper.ResolveReferencePrice(other, commodity);
            if (otherRef <= 0)
                continue;

            var transportBp = distance * MarketConstants.RegionalTransportCostBpPerTile;
            var netSellAtOther = otherRef
                * (EconomyConstants.BasisPointsPer100Percent - transportBp)
                / EconomyConstants.BasisPointsPer100Percent;
            if (netSellAtOther <= originRef)
                continue;

            var edgeBp = (netSellAtOther - originRef) * EconomyConstants.BasisPointsPer100Percent / originRef;
            if (edgeBp < MarketConstants.RegionalArbitrageMinEdgeBp)
                continue;

            // 业务：本城挂卖可参考「运到邻城仍有利」的到岸价，略低于邻城中枢以吸引买盘
            var localAsk = Math.Max(originRef, netSellAtOther - 1);
            best = Math.Max(best, localAsk);
        }

        return best;
    }

    /// <summary>
    /// 进口买价上限：邻城更便宜且运回仍有利时，取最优到岸买价。
    /// 无机会时返回 0。
    /// </summary>
    public static int ResolveImportBidCeiling(
        Stronghold destination,
        GameData gameData,
        MarketCommodityType commodity = MarketCommodityType.Food)
    {
        var destRef = MarketMakerAiHelper.ResolveReferencePrice(destination, commodity);
        if (destRef <= 0)
            return 0;

        var best = 0;
        foreach (var other in gameData.Strongholds.Values)
        {
            if (other.Id == destination.Id)
                continue;
            if (!MarketRules.CanTrade(other, gameData))
                continue;
            if (!DiplomacyTradeRules.CanTradeForces(destination.ForceId, other.ForceId, gameData))
                continue;

            var distance = Manhattan(destination, other);
            if (distance <= 0 || distance > MarketConstants.RegionalArbitrageMaxDistanceTiles)
                continue;

            var otherRef = MarketMakerAiHelper.ResolveReferencePrice(other, commodity);
            if (otherRef <= 0)
                continue;

            var transportBp = distance * MarketConstants.RegionalTransportCostBpPerTile;
            var landedCost = otherRef
                * (EconomyConstants.BasisPointsPer100Percent + transportBp)
                / EconomyConstants.BasisPointsPer100Percent;
            if (landedCost >= destRef)
                continue;

            var edgeBp = (destRef - landedCost) * EconomyConstants.BasisPointsPer100Percent / destRef;
            if (edgeBp < MarketConstants.RegionalArbitrageMinEdgeBp)
                continue;

            // 业务：本城挂买可参考「从邻城运入仍有利」的到岸成本
            var localBid = Math.Min(destRef - 1, Math.Max(1, landedCost));
            best = best <= 0 ? localBid : Math.Min(best, localBid);
        }

        return best;
    }

    private static int Manhattan(Stronghold a, Stronghold b)
        => Math.Abs(a.Location.X - b.Location.X) + Math.Abs(a.Location.Y - b.Location.Y);
}
