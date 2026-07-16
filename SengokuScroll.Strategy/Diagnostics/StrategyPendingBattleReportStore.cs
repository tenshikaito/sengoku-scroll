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
}
