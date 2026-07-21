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

/// <summary>下达角色地图移动命令：寻路后写入路径队列并置 Moving 状态（RPG/策略共用）。</summary>
public partial class CharacterMoveCommandHandler(
    ILogger<CharacterMoveCommandHandler> logger,
    IPathfindingService pathfindingService)
    : CommandHandlerBase, IRequestHandler<CharacterMoveCommand>
{
    /// <summary>校验角色存在、寻路至目标格，将路径入队（首格跳过，由日推进逐步 dequeue）。</summary>
    public GameResult Handle(CharacterMoveCommand cmd, IGameRequestContext context)
    {
        LogParams(cmd.CharacterId, cmd.Location);

        var gameContext = context.GameContext;
        var character = gameContext.GameWorldContext.GetCharacterOrDefault(cmd.CharacterId)!;

        if (!character)
            return CharacterError.CharacterNotFound;

        // 业务：寻路失败时不改 ActionStatus，避免卡在 Moving 且无路径
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
