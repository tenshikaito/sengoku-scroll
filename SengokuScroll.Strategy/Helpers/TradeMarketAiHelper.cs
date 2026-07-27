using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>跨据点粮价套利：低价据点采买、高价据点售出（做市商行商）。</summary>
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

        if (!MarketRules.CanTrade(origin, gameData) || !MarketRules.CanTrade(destination, gameData))
            return false;

        if (TransportUnitRules.HasActiveTradeTransport(gameData, origin.Id, destination.Id))
            return false;

        var buyQty = MarketCalculator.CalculateCivilianBuyQuantityGo(destination);
        if (buyQty < MarketConstants.GovernmentMinSellQuantityGo)
            return false;

        var sellQty = MarketCalculator.CalculateGovernmentSellQuantityGo(origin);
        if (sellQty < MarketConstants.GovernmentMinSellQuantityGo)
            return false;

        var originPrice = MarketMakerAiHelper.ResolveReferencePrice(origin);
        var destinationPrice = MarketMakerAiHelper.ResolveReferencePrice(destination);
        var minProfitableDestination = originPrice
            * (EconomyConstants.BasisPointsPer100Percent + MarketConstants.TradeMinProfitSpreadBp)
            / EconomyConstants.BasisPointsPer100Percent;

        return destinationPrice >= minProfitableDestination;
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
}
