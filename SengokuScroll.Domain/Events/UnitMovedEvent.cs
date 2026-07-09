using SengokuScroll.Common.Types;

namespace SengokuScroll.Domain.Events;

public class CharacterMovedEvent : IGameEvent
{
    public required int CharacterId { get; set; }

    public required string CharacterName { get; set; }

    public required Point2 From { get; set; }

    public required Point2 To { get; set; }
}
