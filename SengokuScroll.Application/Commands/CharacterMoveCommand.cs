using SengokuScroll.Application.Models;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;

namespace SengokuScroll.Application.Commands;

public class CharacterMoveCommand : ICommand
{
    public required int CharacterId { get; set; }

    public required Point2 Location { get; set; }
}
