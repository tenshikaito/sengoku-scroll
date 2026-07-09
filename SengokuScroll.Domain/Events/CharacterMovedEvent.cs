using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Events;

public class UnitMovedEvent : IGameEvent
{
    public required int Id { get; set; }

    public required string Name { get; set; }

    public required Point2 From { get; set; }

    public required Point2 To { get; set; }
}
