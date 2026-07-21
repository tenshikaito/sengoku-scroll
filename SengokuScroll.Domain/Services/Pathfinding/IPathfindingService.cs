using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Domain.Services.Pathfinding;

public interface IPathfindingService
{
    List<PathNode>? CalculatePath(IMovable movable, Point2 target);

    List<PathNode>? CalculatePathFrom(Point2 start, Point2 target, IMovable movable);

    /** 可选自定义途经格阻挡判定（用于战争迷雾下的路径预览等）。 */
    List<PathNode>? CalculatePathFrom(
        Point2 start,
        Point2 target,
        IMovable movable,
        Func<Point2, bool>? isPathTileBlocked);
}
