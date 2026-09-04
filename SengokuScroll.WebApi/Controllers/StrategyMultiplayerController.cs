using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SengokuScroll.Strategy.Models;
using SengokuScroll.WebApi.Models;
using SengokuScroll.WebApi.Multiplayer;

namespace SengokuScroll.WebApi.Controllers;

[ApiController]
[Route("api/multiplayer/rooms")]
public sealed class StrategyMultiplayerController(
    StrategyMultiplayerRoomManager roomManager,
    IHubContext<StrategyRoomHub> hub) : ControllerBase
{
    [HttpGet]
    public IActionResult ListRooms() => Ok(roomManager.ListRooms());

    [HttpGet("/api/multiplayer/scenarios/{scenarioId}/forces")]
    public IActionResult ListPlayableForces(string scenarioId)
    {
        try
        {
            return Ok(roomManager.ListPlayableForces(scenarioId));
        }
        catch (StrategyMultiplayerException ex)
        {
            return BadRequest(new ApiErrorResponse(ex.Code));
        }
    }

    [HttpPost]
    public IActionResult CreateRoom([FromBody] StrategyMultiplayerCreateRoomRequest request)
    {
        try
        {
            return Ok(roomManager.CreateRoom(request));
        }
        catch (StrategyMultiplayerException ex)
        {
            return BadRequest(new ApiErrorResponse(ex.Code));
        }
    }

    [HttpGet("{roomId}")]
    public IActionResult GetRoom(string roomId)
        => roomManager.TryGetRoom(roomId, out var room)
            ? Ok(room.ToDto())
            : NotFound(new ApiErrorResponse("RoomNotFound"));

    [HttpPost("{roomId}/join")]
    public async Task<IActionResult> JoinRoom(
        string roomId,
        [FromBody] StrategyMultiplayerJoinRoomRequest request)
    {
        if (!roomManager.TryGetRoom(roomId, out var room))
            return NotFound(new ApiErrorResponse("RoomNotFound"));

        await room.Gate.WaitAsync(HttpContext.RequestAborted);
        try
        {
            var player = room.AddPlayer(request.PlayerName, request.ForceId);
            room.RefreshHumanControlledForces();
            var response = new StrategyMultiplayerRoomResponse
            {
                Room = room.ToDto(),
                Credentials = player.ToCredentials()
            };
            await BroadcastRoomChanged(room, "PlayerJoined");
            return Ok(response);
        }
        catch (StrategyMultiplayerException ex)
        {
            return BadRequest(new ApiErrorResponse(ex.Code));
        }
        finally
        {
            room.Gate.Release();
        }
    }

    [HttpPost("{roomId}/reconnect")]
    public async Task<IActionResult> Reconnect(
        string roomId,
        [FromBody] StrategyMultiplayerReconnectRequest request)
    {
        if (!roomManager.TryGetRoom(roomId, out var room))
            return NotFound(new ApiErrorResponse("RoomNotFound"));

        await room.Gate.WaitAsync(HttpContext.RequestAborted);
        try
        {
            if (!room.TryReconnect(request.PlayerId, request.PlayerToken, out var player))
                return Unauthorized(new ApiErrorResponse("InvalidRoomCredentials"));

            room.RefreshHumanControlledForces();
            await BroadcastRoomChanged(room, "PlayerReconnected");
            return Ok(new StrategyMultiplayerRoomResponse
            {
                Room = room.ToDto(),
                Credentials = player.ToCredentials()
            });
        }
        finally
        {
            room.Gate.Release();
        }
    }

    [HttpPost("{roomId}/leave")]
    public async Task<IActionResult> Leave(string roomId)
    {
        if (!roomManager.TryGetRoom(roomId, out var room))
            return NotFound(new ApiErrorResponse("RoomNotFound"));
        if (!TryReadPlayerToken(out var token))
            return Unauthorized(new ApiErrorResponse("MissingPlayerToken"));

        await room.Gate.WaitAsync(HttpContext.RequestAborted);
        var removeEmptyRoom = false;
        try
        {
            if (!room.TryAuthenticate(token, out var player))
                return Unauthorized(new ApiErrorResponse("InvalidRoomCredentials"));

            room.RemovePlayer(player);
            room.RefreshHumanControlledForces();
            var snapshot = room.ToDto();
            await BroadcastRoomChanged(room, "PlayerDisconnected");
            removeEmptyRoom = room.PlayerCount == 0;
            return Ok(snapshot);
        }
        finally
        {
            room.Gate.Release();
            if (removeEmptyRoom)
                roomManager.TryRemoveRoom(room.RoomId);
        }
    }

    [HttpPost("{roomId}/ready")]
    public async Task<IActionResult> Ready(
        string roomId,
        [FromBody] StrategyMultiplayerReadyRequest request)
    {
        if (!roomManager.TryGetRoom(roomId, out var room))
            return NotFound(new ApiErrorResponse("RoomNotFound"));
        if (!TryReadPlayerToken(out var token))
            return Unauthorized(new ApiErrorResponse("MissingPlayerToken"));

        await room.Gate.WaitAsync(HttpContext.RequestAborted);
        string? commandId = null;
        var commandReserved = false;
        var commandSucceeded = false;
        try
        {
            if (!room.TryAuthenticate(token, out var player))
                return Unauthorized(new ApiErrorResponse("InvalidRoomCredentials"));

            commandId = Request.Headers[StrategyMultiplayerHeaders.CommandId].FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(commandId) || commandId.Length > 100)
                return BadRequest(new ApiErrorResponse("MissingOrInvalidCommandId"));
            if (!room.TryReserveCommandId(commandId))
                return Conflict(new ApiErrorResponse("DuplicateCommand"));
            commandReserved = true;

            room.MarkConnected(player);
            room.SetReady(player, request.Ready);
            room.RefreshHumanControlledForces();

            var playerContext = room.Host.UsePlayerForce(player.ForceId);
            if (!playerContext.IsSuccess)
                return BadRequest(new ApiErrorResponse(playerContext.Error?.Code ?? "ForceContextFailed"));

            using (playerContext.Value)
            {
                var shouldAdvance = request.Ready && room.AreAllConnectedPlayersReady();
                StrategyAdvanceDayResponseDto advance;
                if (shouldAdvance)
                {
                    var result = room.Host.AdvanceDay();
                    if (!result.IsSuccess)
                        return BadRequest(new ApiErrorResponse(result.Error?.Code ?? "AdvanceFailed"));
                    advance = result.Value;
                    room.ResetReady();
                    room.MarkWorldChanged(started: true);
                }
                else
                {
                    var state = room.Host.GetState();
                    if (!state.IsSuccess)
                        return BadRequest(new ApiErrorResponse(state.Error?.Code ?? "StateFailed"));
                    advance = new StrategyAdvanceDayResponseDto
                    {
                        State = state.Value,
                        ResolvedBattles = [],
                        Events = [],
                        DaysAdvanced = 0,
                        DayDebugEntryCount = 0
                    };
                }

                await BroadcastRoomChanged(room, shouldAdvance ? "WorldAdvanced" : "ReadyChanged");
                Response.Headers[StrategyMultiplayerHeaders.WorldVersion] = room.WorldVersion.ToString();
                commandSucceeded = true;
                return Ok(new StrategyMultiplayerReadyResponse
                {
                    Room = room.ToDto(),
                    Advance = advance,
                    Advanced = shouldAdvance
                });
            }
        }
        finally
        {
            if (commandId is not null && commandReserved && !commandSucceeded)
                room.ReleaseCommandId(commandId);
            room.Gate.Release();
        }
    }

    private bool TryReadPlayerToken(out string token)
    {
        token = Request.Headers[StrategyMultiplayerHeaders.PlayerToken].FirstOrDefault()?.Trim() ?? string.Empty;
        return token.Length > 0;
    }

    private Task BroadcastRoomChanged(StrategyMultiplayerRoomSession room, string reason)
        => hub.Clients.Group(StrategyRoomHub.GroupName(room.RoomId)).SendAsync(
            "WorldChanged",
            new
            {
                roomId = room.RoomId,
                worldVersion = room.WorldVersion,
                reason
            },
            HttpContext.RequestAborted);
}
