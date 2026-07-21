using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Abstraction;
using SengokuScroll.Domain.Rules;

namespace SengokuScroll.Domain.Services.Pathfinding;

public class PathfindingService(IGameContext context, MovementRules movementRules) : IPathfindingService
{
    public List<PathNode>? CalculatePath(IMovable movable, Point2 target)
        => CalculatePathFrom((Point2)movable.Location, target, movable);

    public List<PathNode>? CalculatePathFrom(Point2 start, Point2 target, IMovable movable)
        => CalculatePathFrom(start, target, movable, null);

    public List<PathNode>? CalculatePathFrom(
        Point2 start,
        Point2 target,
        IMovable movable,
        Func<Point2, bool>? isPathTileBlocked)
    {
        var openSet = new PriorityQueue<Point2, int>();
        var cameFrom = new Dictionary<Point2, Point2?>();

        var gScore = new Dictionary<Point2, int>();
        var stepCost = new Dictionary<Point2, int>();

        var visited = new HashSet<Point2>();

        openSet.Enqueue(start, 0);

        cameFrom[start] = null;
        gScore[start] = 0;
        stepCost[start] = 0;

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (visited.Contains(current))
                continue;

            visited.Add(current);

            if (current == target)
                return ReconstructPath(cameFrom, gScore, stepCost, current, start);

            foreach (var next in GetNeighbors(current))
            {
                if (context.GameWorldContext.GameWorld.GameMapMasterData.TileMap.IsOutOfBounds(next))
                    continue;

                var moveCost = movementRules.GetTileMovementApCost(movable, next);

                if (moveCost < 0)
                    continue;

                if (IsBlockedByOccupyingMilitaryUnit(movable, next, target, isPathTileBlocked))
                    continue;

                var tentativeG = gScore[current] + moveCost;

                if (!gScore.TryGetValue(next, out var oldG) || tentativeG < oldG)
                {
                    cameFrom[next] = current;
                    gScore[next] = tentativeG;
                    stepCost[next] = moveCost;

                    var f = tentativeG + Heuristic(next, target);
                    openSet.Enqueue(next, f);
                }
            }
        }

        return null;
    }

    private static List<PathNode> ReconstructPath(
        Dictionary<Point2, Point2?> cameFrom,
        Dictionary<Point2, int> gScore,
        Dictionary<Point2, int> stepCost,
        Point2 current,
        Point2 start)
    {
        var path = new List<PathNode>();

        while (true)
        {
            path.Add(new PathNode(
                current,
                stepCost.GetValueOrDefault(current),
                gScore[current]
            ));

            if (current == start)
                break;

            current = cameFrom[current]!.Value;
        }

        path.Reverse();
        return path;
    }

    private static int Heuristic(Point2 a, Point2 b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static IEnumerable<Point2> GetNeighbors(Point2 p)
    {
        yield return new Point2(p.X + 1, p.Y);
        yield return new Point2(p.X - 1, p.Y);
        yield return new Point2(p.X, p.Y + 1);
        yield return new Point2(p.X, p.Y - 1);
    }

    private bool IsBlockedByOccupyingMilitaryUnit(
        IMovable movable,
        Point2 location,
        Point2 pathTarget,
        Func<Point2, bool>? isPathTileBlocked = null)
    {
        // 业务：最终目标格可进入敌军（开战）；途中仍不可穿越
        if (location.X == pathTarget.X && location.Y == pathTarget.Y)
            return false;

        if (isPathTileBlocked is not null)
            return isPathTileBlocked(location);

        return movementRules.IsPathTileBlockedByMilitary(movable, location);
    }
}
