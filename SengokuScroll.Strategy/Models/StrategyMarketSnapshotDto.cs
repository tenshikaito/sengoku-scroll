namespace SengokuScroll.Strategy.Models;

/// <summary>市场窗口快照（按需 API）。</summary>
public sealed record StrategyMarketSnapshotDto
{
    public required int StrongholdId { get; init; }

    public required string StrongholdName { get; init; }

    public required string Commodity { get; init; }

    public required bool IsOpen { get; init; }

    public required int LastClosePriceMoneyPerGo { get; init; }

    /// <summary>盘口中线现价（= 最后成交价，仅成交更新）。</summary>
    public required int SessionPriceMoneyPerGo { get; init; }

    /// <summary>挂单簿形态：Empty | Bid | Ask | Both（诊断用，不改现价）。</summary>
    public required string BookQuoteSide { get; init; }

    public required int BestBidPriceMoneyPerGo { get; init; }

    public required int BestAskPriceMoneyPerGo { get; init; }

    /// <summary>分割线同价挂单总量（合）。</summary>
    public required int CloseLevelQuantityGo { get; init; }

    public required IReadOnlyList<StrategyMarketDepthLevelDto> BidLevels { get; init; }

    public required IReadOnlyList<StrategyMarketDepthLevelDto> AskLevels { get; init; }

    public required IReadOnlyList<StrategyMarketDailyBarDto> DailyBars { get; init; }

    /// <summary>玩家（官府）当前商品挂单；非己方据点为空。</summary>
    public required IReadOnlyList<StrategyMarketOpenOrderDto> PlayerOpenOrders { get; init; }
}

public sealed record StrategyMarketDepthLevelDto
{
    public required int PriceMoneyPerGo { get; init; }

    public required int QuantityGo { get; init; }
}

public sealed record StrategyMarketDailyBarDto
{
    public required int Year { get; init; }

    public required int Month { get; init; }

    public required int Day { get; init; }

    public required int Open { get; init; }

    public required int High { get; init; }

    public required int Low { get; init; }

    public required int Close { get; init; }

    public required int VolumeGo { get; init; }

    public required int TurnoverMoney { get; init; }
}

public sealed record StrategyMarketOpenOrderDto
{
    public required int Id { get; init; }

    public required string Side { get; init; }

    public required int PriceMoneyPerGo { get; init; }

    public required int QuantityGo { get; init; }

    public required int OriginalQuantityGo { get; init; }

    public required int FilledQuantityGo { get; init; }

    /// <summary>Open | Partial</summary>
    public required string FillStatus { get; init; }

    public required int CreatedYear { get; init; }

    public required int CreatedMonth { get; init; }

    public required int CreatedDay { get; init; }
}
