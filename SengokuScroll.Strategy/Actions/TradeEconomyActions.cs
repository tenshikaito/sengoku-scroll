using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Actions;

/// <summary>跨据点贸易运输队到港交割（M4-c）。</summary>
public static class TradeEconomyActions
{
    /// <summary>
    /// 贸易队抵达买方据点：粮入市民库存，货款记入出发据点官府（贸易收入）。
    /// </summary>
    /// <returns>成交总额（文）。</returns>
    public static int CompleteTradeArrival(
        SupplyConvoy convoy,
        Stronghold origin,
        Stronghold destination,
        GameData gameData)
    {
        var food = convoy.CargoFoodGo;
        if (food <= 0)
        {
            convoy.CargoFoodGo = 0;
            convoy.CargoMoney = 0;
            return 0;
        }

        var price = destination.Market.LastClosePriceMoneyPerGo > 0
            ? destination.Market.LastClosePriceMoneyPerGo
            : MarketConstants.DefaultPriceMoneyPerGo;

        var revenue = food * price;
        // 业务：贸易收入记入出发据点官府，粮入买方市民库存
        destination.CivilianActor.Food += food;
        origin.ForceActor.Money += revenue;

        convoy.CargoFoodGo = 0;
        convoy.CargoMoney = 0;

        if (gameData.Forces.TryGetValue(origin.ForceId, out var force))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);

        return revenue;
    }
}
