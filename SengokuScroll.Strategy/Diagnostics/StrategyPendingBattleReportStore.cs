using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>战报信使在途期间暂存完整战报，抵达当主后随事件投递给 UI。</summary>
public sealed class StrategyPendingBattleReportStore
{
    private readonly Dictionary<int, StrategyBattleResultDto> byMessengerId = new();

    public void Attach(int messengerId, StrategyBattleResultDto result)
        => byMessengerId[messengerId] = result;

    public StrategyBattleResultDto? Take(int messengerId)
        => byMessengerId.Remove(messengerId, out var result) ? result : null;

    public void PruneMissingCarriers(SengokuScroll.Domain.GameData data)
    {
        foreach (var id in byMessengerId.Keys.Where(id => !data.MessageCarriers.ContainsKey(id)).ToArray())
            byMessengerId.Remove(id);
    }

    public IReadOnlyDictionary<int, StrategyBattleResultDto> Snapshot()
        => new Dictionary<int, StrategyBattleResultDto>(byMessengerId);

    public void Restore(IReadOnlyDictionary<int, StrategyBattleResultDto> restored)
    {
        byMessengerId.Clear();
        foreach (var (messengerId, result) in restored)
            byMessengerId[messengerId] = result;
    }
}
