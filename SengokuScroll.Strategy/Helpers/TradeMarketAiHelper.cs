using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>跨据点粮价套利：同势力或同盟派遣 Trade 运输队（M4-c/d）。</summary>
public static class TradeMarketAiHelper
{
    public static bool ShouldDispatchTrade(
        Stronghold origin,
        Stronghold destination,
        GameData gameData)
    {
        if (origin.Id == destination.Id)
            return false;

        if (!DiplomacyTradeRules.CanTradeForces(origin.ForceId, destination.ForceId, gameData))
            return false;

        if (!MarketRules.CanTrade(origin) || !MarketRules.CanTrade(destination))
            return false;

        if (HasActiveTradeConvoy(gameData.SupplyConvoys, origin.Id, destination.Id))
            return false;

        var buyQty = MarketCalculator.CalculateCivilianBuyQuantityGo(destination);
        if (buyQty < MarketConstants.GovernmentMinSellQuantityGo)
            return false;

        var sellQty = MarketCalculator.CalculateGovernmentSellQuantityGo(origin);
        if (sellQty < MarketConstants.GovernmentMinSellQuantityGo)
            return false;

        var sellPrice = MarketCalculator.CalculateGovernmentSellPrice(origin);
        var buyLimit = MarketCalculator.CalculateCivilianBuyLimitPrice(destination);
        var minBuy = sellPrice * (EconomyConstants.BasisPointsPer100Percent + MarketConstants.TradeMinProfitSpreadBp)
                       / EconomyConstants.BasisPointsPer100Percent;

        return buyLimit >= minBuy;
    }

    public static int CalculateTradeCargoGo(Stronghold origin, Stronghold destination)
    {
        var sellQty = MarketCalculator.CalculateGovernmentSellQuantityGo(origin);
        var buyQty = MarketCalculator.CalculateCivilianBuyQuantityGo(destination);
        return Math.Min(
            Math.Min(sellQty, buyQty),
            LogisticsConstants.DefaultConvoyCargoGo);
    }

    public static int ResolvePathForceId(Stronghold origin, GameData gameData)
        => origin.ForceId;

    public static bool HasActiveTradeConvoy(
        IReadOnlyDictionary<int, SupplyConvoy> convoys,
        int originStrongholdId,
        int destinationStrongholdId)
        => convoys.Values.Any(c =>
            c.Purpose == TransportPurpose.Trade
            && !c.IsReturningToOrigin
            && c.OriginStrongholdId == originStrongholdId
            && c.TargetStrongholdId == destinationStrongholdId
            && c.Status is SupplyConvoyStatus.Moving
                or SupplyConvoyStatus.Arrived
                or SupplyConvoyStatus.Deceived);
}
