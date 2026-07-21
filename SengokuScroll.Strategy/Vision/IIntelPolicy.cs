using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>按情报档位处理单位 DTO 字段。</summary>
public interface IIntelPolicy
{
    StrategyUnitStateDto ApplyUnitIntelMask(
        StrategyUnitStateDto unit,
        GameWorld world,
        StrategyScenarioMeta meta,
        int playerForceId,
        HashSet<(int X, int Y)> visibleCells);
}
