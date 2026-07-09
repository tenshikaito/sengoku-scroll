using Microsoft.Extensions.Logging;
using SengokuScroll.Application.Commands;
using SengokuScroll.Application.Contexts;
using SengokuScroll.Application.Models;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Services.Pathfinding;
using static SengokuScroll.Domain.GameError;

namespace SengokuScroll.Application.CommandHandlers;

public partial class CharacterMoveCommandHandler(
    ILogger<CharacterMoveCommandHandler> logger,
    IPathfindingService pathfindingService)
    : CommandHandlerBase, IRequestHandler<CharacterMoveCommand>
{
    public GameResult Handle(CharacterMoveCommand cmd, IGameRequestContext context)
    {
        LogParams(cmd.CharacterId, cmd.Location);

        var gameContext = context.GameContext;
        var character = gameContext.GameWorldContext.GetCharacterOrDefault(cmd.CharacterId)!;

        if (!character)
            return CharacterError.CharacterNotFound;

        // ¼ÆËãÂ·¾¶
        var pathLocationList = pathfindingService.CalculatePath(character, cmd.Location);

        if (pathLocationList is null)
            return MovementError.CannotMoveToTile;

        character.ActionStatus = Character.CharacterActionStatus.Moving;
        character.ActionTarget.RoutePoints = [];

        foreach (var o in pathLocationList.Select(o => o.Location).Skip(1))
            character.ActionTarget.RoutePoints.Enqueue(o);

        return GameResult.Ok();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handle: CharacterId={CharacterId}, Location={Location}")]
    private partial void LogParams(int characterId, Point2 location);
}
