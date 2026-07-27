using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>市场快照诊断日志（Development 下输出到 Debug）。</summary>
public static class MarketSnapshotDiagnostics
{
    public static string FormatSummary(StrategyMarketSnapshotDto snapshot, int uiDepthCount = 5)
    {
        var askRaw = FormatLevels(snapshot.AskLevels);
        var bidRaw = FormatLevels(snapshot.BidLevels);
        var askUi = FormatLevels(UiVisibleAskLevels(snapshot.AskLevels, uiDepthCount));
        var bidUi = FormatLevels(UiVisibleBidLevels(snapshot.BidLevels, uiDepthCount));

        return string.Join(
            " | ",
            $"sh={snapshot.StrongholdId} {snapshot.StrongholdName}",
            $"quote={snapshot.SessionPriceMoneyPerGo}",
            $"side={snapshot.BookQuoteSide}",
            $"bestBid={snapshot.BestBidPriceMoneyPerGo}",
            $"bestAsk={snapshot.BestAskPriceMoneyPerGo}",
            $"closeQty={snapshot.CloseLevelQuantityGo}",
            $"askRaw=[{askRaw}]",
            $"askUi({uiDepthCount})=[{askUi}]",
            $"bidRaw=[{bidRaw}]",
            $"bidUi({uiDepthCount})=[{bidUi}]");
    }

    public static IReadOnlyList<StrategyMarketDepthLevelDto> UiVisibleAskLevels(
        IReadOnlyList<StrategyMarketDepthLevelDto> levels,
        int count)
    {
        var active = levels.Where(l => l.PriceMoneyPerGo > 0).ToList();
        if (active.Count <= count)
            return active;

        return active.Skip(active.Count - count).ToList();
    }

    public static IReadOnlyList<StrategyMarketDepthLevelDto> UiVisibleBidLevels(
        IReadOnlyList<StrategyMarketDepthLevelDto> levels,
        int count)
    {
        return levels.Where(l => l.PriceMoneyPerGo > 0).Take(count).ToList();
    }

    private static string FormatLevels(IReadOnlyList<StrategyMarketDepthLevelDto> levels)
        => string.Join(
            ",",
            levels
                .Where(l => l.PriceMoneyPerGo > 0)
                .Select(l => $"{l.PriceMoneyPerGo}@{l.QuantityGo}"));
}
