using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>计算某观察势力当日可见格集。</summary>
public interface IVisionPolicy
{
    HashSet<(int X, int Y)> ComputeVisibleTiles(
        GameWorld world,
        StrategyScenarioMeta meta,
        int observerForceId,
        GameStartOptions options);
}
