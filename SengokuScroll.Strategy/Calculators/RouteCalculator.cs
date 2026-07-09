using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Services.Pathfinding;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>
/// 将寻路结果转为地图上逐日推进用的路径队列。
/// </summary>
public static class RouteCalculator
{
    /// <summary>
    /// 把寻路节点列表转为 <see cref="Queue{Point3}"/>（跳过起点，其余格按顺序入队）。
    /// </summary>
    /// <param name="path">寻路服务返回的路径；null 或空则返回空队列。</param>
    public static Queue<Point3> ToDailyRouteQueue(IReadOnlyList<PathNode>? path)
    {
        var queue = new Queue<Point3>();

        if (path is null || path.Count <= 1)
            return queue;

        for (var i = 1; i < path.Count; i++)
            queue.Enqueue(path[i].Location);

        return queue;
    }

    /// <summary>路径总步数（不含起点）。</summary>
    public static int CountSteps(IReadOnlyList<PathNode>? path)
        => path is null || path.Count <= 1 ? 0 : path.Count - 1;
}
