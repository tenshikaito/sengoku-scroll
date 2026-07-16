using SengokuScroll.Domain;
using SengokuScroll.Domain.Systems;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>
/// 带日推进 debug 的系统链引擎：按 Order 逐个执行 System 并记录起止。
/// </summary>
public sealed class StrategyDebugGameEngine : IGameEngine
{
    private readonly IReadOnlyList<IGameSystem> systems;
    private readonly IStrategyDayDebugLog debugLog;

    public StrategyDebugGameEngine(IEnumerable<IGameSystem> systems, IStrategyDayDebugLog debugLog)
    {
        this.systems = systems.OrderBy(s => s.Order).ToList();
        this.debugLog = debugLog;
    }

    public void NextTime()
    {
        foreach (var system in systems)
        {
            var name = system.GetType().Name;
            if (debugLog.IsEnabled)
            {
                debugLog.LogSystemStart(name, system.Order);
                system.Update();
                debugLog.LogSystemEnd(name, system.Order);
            }
            else
            {
                system.Update();
            }
        }
    }
}

/// <summary>无 debug 包装的标准策略引擎。</summary>
public sealed class StrategyGameEngineCore : IGameEngine
{
    private readonly IReadOnlyList<IGameSystem> systems;

    public StrategyGameEngineCore(IEnumerable<IGameSystem> systems)
        => this.systems = systems.OrderBy(s => s.Order).ToList();

    public void NextTime()
    {
        foreach (var system in systems)
            system.Update();
    }
}
