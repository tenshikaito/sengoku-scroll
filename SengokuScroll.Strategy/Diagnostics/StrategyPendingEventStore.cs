using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>战略报告信使在途期间暂存事件详情，抵达当主后投递给 UI。</summary>
public sealed class StrategyPendingEventStore
{
    private readonly Dictionary<int, StrategyEventDto> byMessengerId = new();

    public void Attach(int messengerId, StrategyEventDto evt)
        => byMessengerId[messengerId] = evt;

    public StrategyEventDto? Take(int messengerId)
        => byMessengerId.Remove(messengerId, out var evt) ? evt : null;
}
