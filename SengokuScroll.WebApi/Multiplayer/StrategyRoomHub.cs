using Microsoft.AspNetCore.SignalR;

namespace SengokuScroll.WebApi.Multiplayer;

public sealed class StrategyRoomHub(StrategyMultiplayerRoomManager roomManager) : Hub
{
    public static string GroupName(string roomId) => $"strategy-room:{roomId.ToUpperInvariant()}";

    public async Task JoinRoom(string roomId, string playerToken)
    {
        if (!roomManager.TryGetRoom(roomId, out var room))
        {
            throw new HubException("InvalidRoomCredentials");
        }

        await room.Gate.WaitAsync(Context.ConnectionAborted);
        try
        {
            if (!room.TryAuthenticate(playerToken, out var player))
                throw new HubException("InvalidRoomCredentials");
            room.MarkConnected(player);
            room.RefreshHumanControlledForces();
        }
        finally
        {
            room.Gate.Release();
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(room.RoomId));
        await Clients.Caller.SendAsync(
            "RoomJoined",
            new { roomId = room.RoomId, worldVersion = room.WorldVersion },
            Context.ConnectionAborted);
    }

    // Presence is renewed by authenticated HTTP activity. A single socket closing
    // must not disconnect a player whose other tab/connection is still active.
}
