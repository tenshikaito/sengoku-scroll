namespace SengokuScroll.Strategy.Models;

/// <summary>挂单簿形态（诊断）；现价始终为最后成交价。</summary>
public enum StrategyMarketBookQuoteSide
{
    /// <summary>买卖两侧均无挂单。</summary>
    Empty = 0,

    /// <summary>仅有买盘（以买一为锚向下展示）。</summary>
    Bid = 1,

    /// <summary>仅有卖盘（以卖一为锚向上展示）。</summary>
    Ask = 2,

    /// <summary>买卖两侧均有挂单（中线取最近成交价）。</summary>
    Both = 3,
}

public readonly record struct StrategyMarketBookQuote(
    int QuotePriceMoneyPerGo,
    StrategyMarketBookQuoteSide Side,
    int BestBidPriceMoneyPerGo,
    int BestAskPriceMoneyPerGo);
