namespace SengokuScroll.Strategy.Time;

/// <summary>策略模式半回合时间推进状态。</summary>
public enum StrategyTimeState
{
    /// <summary>时间暂停，不自动推进。</summary>
    Paused,

    /// <summary>时间推进中（联机时由主机控制）。</summary>
    Running
}
