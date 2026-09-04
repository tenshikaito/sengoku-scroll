using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// 各势力对远端据点市场的已知行情（延迟情报；M4-a 脚手架，M4-b 接信使/旅人传播）。
/// </summary>
public sealed class StrategyIntelligenceLedger
{
    private readonly List<MarketPriceObservation> observations = [];

    public sealed record MarketPriceObservation(
        int ObserverForceId,
        int SubjectStrongholdId,
        int PriceMoneyPerGo,
        GameDate AsOfDate,
        GameDate ReceivedDate,
        string SourceType,
        int ReliabilityBp);

    public void Record(MarketPriceObservation observation)
        => observations.Add(observation);

    /// <summary>取某势力对某据点最新一条粮价情报（按收到日）。</summary>
    public MarketPriceObservation? GetLatestPrice(
        int observerForceId,
        int subjectStrongholdId)
        => observations
            .Where(o => o.ObserverForceId == observerForceId
                        && o.SubjectStrongholdId == subjectStrongholdId)
            .OrderByDescending(o => o.ReceivedDate.Year)
            .ThenByDescending(o => o.ReceivedDate.Month)
            .ThenByDescending(o => o.ReceivedDate.Day)
            .FirstOrDefault();

    public IReadOnlyList<MarketPriceObservation> Snapshot(int observerForceId)
        => observations.Where(o => o.ObserverForceId == observerForceId).ToList();

    public IReadOnlyList<MarketPriceObservation> SnapshotAll()
        => observations.ToList();

    public void Restore(IEnumerable<MarketPriceObservation> restored)
    {
        observations.Clear();
        observations.AddRange(restored);
    }

    public void Clear() => observations.Clear();
}
