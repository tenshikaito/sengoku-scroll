using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Domain.Extensions;

public static class CommonExtensions
{
    public static bool IsAdjacent(this IHasLocation o, Point3 other) => o.Location.IsAdjacent(other);

    public static bool IsAdjacent(this Point3 p, Point3 other)
        => ManhattanDistance(p, other) == 1;

    public static int ManhattanDistance(this Point3 p, Point3 other)
    {
        return Math.Abs(p.X - other.X)
             + Math.Abs(p.Y - other.Y)
             + Math.Abs(p.Z - other.Z);
    }

    public static bool IsFaceToFace(this IMovable movable, IMovable target)
    {
        var directionToTarget = GameMath.LocateAt4(movable.Location, target.Location);

        return movable.Direction == directionToTarget && target.Direction == GameMath.Opposite(directionToTarget);
    }
}
