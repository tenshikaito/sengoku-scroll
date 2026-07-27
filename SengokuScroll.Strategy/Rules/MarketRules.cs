using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Types;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Rules;

/// <summary>市场挂单与撮合判定（M4-b）。</summary>
public static class MarketRules
{
    /// <summary>买单方向标识（求购商品）。</summary>
    public const string BuySide = "Buy";

    /// <summary>卖单方向标识（出售商品）。</summary>
    public const string SellSide = "Sell";

    /// <summary>据点是否已建成 Market 设施（UI 与市场按钮唯一门槛）。</summary>
    public static bool HasMarketFacility(Stronghold stronghold)
        => EconomyFacilityRules.HasMarket(stronghold);

    /// <summary>市场是否开放：有 Market 设施且未处于围城封锁。</summary>
    public static bool IsMarketOpen(Stronghold stronghold, GameData gameData)
        => HasMarketFacility(stronghold)
           && !GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, gameData);

    /// <summary>可否开展撮合/贸易；围城时关市。</summary>
    public static bool CanTrade(Stronghold stronghold, GameData? gameData = null)
    {
        if (!HasMarketFacility(stronghold))
            return false;

        return gameData is null
            || !GarrisonBehaviorRules.IsStrongholdBlockaded(stronghold, gameData);
    }

    /// <summary>判断挂单是否为买单（求购侧）。</summary>
    public static bool IsBuyOrder(MarketOrder order)
        => string.Equals(order.Side, BuySide, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断挂单是否为卖单（出售侧）。</summary>
    public static bool IsSellOrder(MarketOrder order)
        => string.Equals(order.Side, SellSide, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断 side 字符串是否为买单。</summary>
    public static bool IsBuySide(string side)
        => string.Equals(side, BuySide, StringComparison.OrdinalIgnoreCase);

    /// <summary>玩家/官府限价挂单（含锁定资源或挂单日）；AI 刷新时不得覆盖。</summary>
    public static bool IsPlayerRestingOrder(MarketOrder order)
        => order.MoneyCommitted
           || order.InventoryCommitted
           || order.CreatedYear > 0;
}
