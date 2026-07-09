using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Services.Pathfinding;

public readonly struct PathNode(Point2 location, int stepCost, int totalCost)
{
    public Point2 Location { get; } = location;

    public int StepCost { get; } = stepCost;

    public int TotalCost { get; } = totalCost;

    public override string ToString()
        => $"{Location} Step:{StepCost} Total:{TotalCost}";
}