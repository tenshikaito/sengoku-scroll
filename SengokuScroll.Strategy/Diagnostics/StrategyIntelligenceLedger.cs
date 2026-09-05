using SengokuScroll.Domain.Types;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// 各势力对远端据点市场的已知行情（延迟情报；M4-a 脚手架，M4-b 接信使/旅人传播）。
/// </summary>
public sealed class StrategyIntelligenceLedger
{
    // Only the latest received quote is consumed by trade AI. Preserve first-on-tie semantics.
    private readonly Dictionary<(int Force, int Stronghold), MarketPriceObservation> observations = [];

    public sealed record MarketPriceObservation(
        int ObserverForceId,
        int SubjectStrongholdId,
        int PriceMoneyPerGo,
        GameDate AsOfDate,
        GameDate ReceivedDate,
        string SourceType,
        int ReliabilityBp);

    public void Record(MarketPriceObservation observation)
    {
        var key = (observation.ObserverForceId, observation.SubjectStrongholdId);
        if (!observations.TryGetValue(key, out var previous)
            || observation.ReceivedDate.TotalDays > previous.ReceivedDate.TotalDays)
            observations[key] = observation;
    }

    /// <summary>取某势力对某据点最新一条粮价情报（按收到日）。</summary>
    public MarketPriceObservation? GetLatestPrice(
        int observerForceId,
        int subjectStrongholdId)
        => observations.GetValueOrDefault((observerForceId, subjectStrongholdId));

    public IReadOnlyList<MarketPriceObservation> Snapshot(int observerForceId)
        => SnapshotAll().Where(o => o.ObserverForceId == observerForceId).ToList();

    public IReadOnlyList<MarketPriceObservation> SnapshotAll()
        => observations.OrderBy(x => x.Key.Force).ThenBy(x => x.Key.Stronghold).Select(x => x.Value).ToList();

    public void Restore(IEnumerable<MarketPriceObservation> restored)
    {
        observations.Clear();
        foreach (var observation in restored) Record(observation);
    }

    public void Clear() => observations.Clear();
}
