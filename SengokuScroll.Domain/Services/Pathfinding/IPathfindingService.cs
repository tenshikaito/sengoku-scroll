using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Domain.Services.Pathfinding;

public interface IPathfindingService
{
    List<PathNode>? CalculatePath(IMovable movable, Point2 target);

    /** 从任意起点寻路（用于路径中继预览与拼接）。 */
    List<PathNode>? CalculatePathFrom(Point2 start, Point2 target, IMovable movable);
}
