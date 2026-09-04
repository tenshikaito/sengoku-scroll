using Microsoft.AspNetCore.SignalR;

namespace SengokuScroll.WebApi.Multiplayer;

public sealed class StrategyRoomHub(StrategyMultiplayerRoomManager roomManager) : Hub
{
    public static string GroupName(string roomId) => $"strategy-room:{roomId.ToUpperInvariant()}";

    public async Task JoinRoom(string roomId, string playerToken)
    {
        if (!roomManager.TryGetRoom(roomId, out var room)
            || !room.TryAuthenticate(playerToken, out var player))
        {
            throw new HubException("InvalidRoomCredentials");
        }

        await room.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            room.MarkConnected(player);
            room.RefreshHumanControlledForces();
        }
        finally
        {
            room.Gate.Release();
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(room.RoomId));
        Context.Items[nameof(StrategyMultiplayerRoomSession)] = room;
        Context.Items[nameof(StrategyMultiplayerPlayer)] = player;
        await Clients.Caller.SendAsync(
            "RoomJoined",
            new { roomId = room.RoomId, worldVersion = room.WorldVersion },
            Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue(nameof(StrategyMultiplayerRoomSession), out var roomValue)
            && roomValue is StrategyMultiplayerRoomSession room
            && Context.Items.TryGetValue(nameof(StrategyMultiplayerPlayer), out var playerValue)
            && playerValue is StrategyMultiplayerPlayer player)
        {
            await room.Gate.WaitAsync();
            try
            {
                room.MarkDisconnected(player);
                room.RefreshHumanControlledForces();
            }
            finally
            {
                room.Gate.Release();
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
