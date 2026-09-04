using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>
/// 跨据点粮价套利贸易队：仅由<strong>商家组织势力</strong>派出；武家官府只用补给/贡赋运输队。
/// </summary>
public static class TradeMarketAiHelper
{
    /// <summary>商户可派出贸易的余粮（合）：库存 − 营运储备。</summary>
    public static int CalculateMerchantSellQuantityGo(StrongholdActor merchant)
    {
        var surplus = merchant.Food - MarketConstants.MerchantFoodReserveGo;
        if (surplus < MarketConstants.GovernmentMinSellQuantityGo)
            return 0;

        return Math.Min(surplus, MarketConstants.MerchantMaxTotalSellFoodGo);
    }

    public static bool ShouldDispatchTrade(
        Stronghold origin,
        Stronghold destination,
        StrongholdActor merchant,
        GameData gameData,
        StrategyIntelligenceLedger? intelligenceLedger = null)
    {
        if (origin.Id == destination.Id)
            return false;

        // 业务：贸易队仅商户势力；武家不自动派 Trade
        if (!gameData.Forces.TryGetValue(merchant.ForceId, out var merchantForce)
            || merchantForce.Category != ForceCategory.Merchant)
        {
            return false;
        }

        if (!DiplomacyTradeRules.CanTradeForces(origin.ForceId, destination.ForceId, gameData))
            return false;

        if (!MarketRules.CanTrade(origin, gameData) || !MarketRules.CanTrade(destination, gameData))
            return false;

        if (TransportUnitRules.HasActiveTradeTransport(gameData, origin.Id, destination.Id))
            return false;

        var buyQty = MarketCalculator.CalculateCivilianBuyQuantityGo(destination);
        if (buyQty < MarketConstants.GovernmentMinSellQuantityGo)
            return false;

        var sellQty = CalculateMerchantSellQuantityGo(merchant);
        if (sellQty < MarketConstants.GovernmentMinSellQuantityGo)
            return false;

        var originPrice = MarketMakerAiHelper.ResolveReferencePrice(origin);
        var destinationPrice = ResolveKnownDestinationPrice(
            merchant.ForceId,
            destination,
            gameData,
            intelligenceLedger);
        if (destinationPrice <= 0)
            return false;
        var distance = Math.Abs(origin.Location.X - destination.Location.X)
                       + Math.Abs(origin.Location.Y - destination.Location.Y);
        var transportBp = Math.Max(0, distance) * MarketConstants.RegionalTransportCostBpPerTile;
        var minProfitableDestination = originPrice
            * (EconomyConstants.BasisPointsPer100Percent
               + MarketConstants.TradeMinProfitSpreadBp
               + transportBp)
            / EconomyConstants.BasisPointsPer100Percent;

        return destinationPrice >= minProfitableDestination;
    }

    private static int ResolveKnownDestinationPrice(
        int observerForceId,
        Stronghold destination,
        GameData gameData,
        StrategyIntelligenceLedger? intelligenceLedger)
    {
        // 兼容纯规则单测/调用方；正式日更始终传入情报账本。
        if (intelligenceLedger is null)
            return MarketMakerAiHelper.ResolveReferencePrice(destination);

        var observation = intelligenceLedger.GetLatestPrice(observerForceId, destination.Id);
        if (observation is null)
            return 0;

        var ageDays = Math.Max(0, gameData.GameDate.TotalDays - observation.AsOfDate.TotalDays);
        if (ageDays > MarketConstants.TradeIntelMaxAgeDays)
            return 0;

        return observation.PriceMoneyPerGo;
    }

    public static int CalculateTradeCargoGo(StrongholdActor merchant, Stronghold destination)
    {
        var sellQty = CalculateMerchantSellQuantityGo(merchant);
        var buyQty = MarketCalculator.CalculateCivilianBuyQuantityGo(destination);
        return Math.Min(
            Math.Min(sellQty, buyQty),
            LogisticsConstants.DefaultConvoyCargoGo);
    }

    /// <summary>路径寻路用本城武家势力 Id（通行/ZOC）；队属商家组织另记。</summary>
    public static int ResolvePathForceId(Stronghold origin, GameData gameData)
        => origin.ForceId;
}
