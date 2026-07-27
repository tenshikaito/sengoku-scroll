using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Actions;

/// <summary>跨据点贸易运输 Unit 到港交割（M4-c）。</summary>
public static class TradeEconomyActions
{
    /// <summary>
    /// 贸易队抵达买方据点：粮入市民库存，货款记入出发据点官府（贸易收入）。
    /// </summary>
    /// <returns>成交总额（文）。</returns>
    public static int CompleteTradeArrival(
        Unit transport,
        Stronghold origin,
        Stronghold destination,
        GameData gameData)
    {
        var food = transport.Food;
        if (food <= 0)
        {
            transport.Food = 0;
            transport.Money = 0;
            return 0;
        }

        var price = destination.Market.LastClosePriceMoneyPerGo > 0
            ? destination.Market.LastClosePriceMoneyPerGo
            : MarketConstants.DefaultPriceMoneyPerGo;

        var revenue = food * price;
        destination.CivilianActor.Food += food;
        origin.ForceActor.Money += revenue;

        transport.Food = 0;
        transport.Money = 0;

        if (gameData.Forces.TryGetValue(origin.ForceId, out var force))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);

        return revenue;
    }
}
