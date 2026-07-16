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

    /// <summary>Market 设施或商业值达标方可交易（M4-d）。</summary>
    public static bool CanTrade(Stronghold stronghold)
        => EconomyFacilityRules.HasMarket(stronghold)
           || stronghold.CommerceValue >= EconomyConstants.CommerceValuePerShopSlot;

    /// <summary>判断挂单是否为买单（求购侧）。</summary>
    public static bool IsBuyOrder(MarketOrder order)
        => string.Equals(order.Side, BuySide, StringComparison.OrdinalIgnoreCase);

    /// <summary>判断挂单是否为卖单（出售侧）。</summary>
    public static bool IsSellOrder(MarketOrder order)
        => string.Equals(order.Side, SellSide, StringComparison.OrdinalIgnoreCase);
}
