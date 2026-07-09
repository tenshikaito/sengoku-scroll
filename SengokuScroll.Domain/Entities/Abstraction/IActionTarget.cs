using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Entities.Abstraction;

public interface IActionTarget
{
    Queue<Point2> RoutePoints { get; }
}
