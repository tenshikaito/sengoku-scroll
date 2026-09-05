using System.Net;
using System.Net.Http.Json;
using SengokuScroll.Strategy.Models;
using SengokuScroll.WebApi.Models;
using SengokuScroll.WebApi.Multiplayer;

namespace SengokuScroll.WebApi.Tests;

public sealed class StrategyMultiplayerApiTests : IClassFixture<StrategyWebApplicationFactory>
{
    private readonly HttpClient client;

    public StrategyMultiplayerApiTests(StrategyWebApplicationFactory factory)
        => client = factory.CreateClient();

    [Fact]
    public async Task CreateJoinAndState_KeepPlayerForceContextsIsolated()
    {
        var host = await CreateRoom();
        var guestForce = host.Room.Forces.First(force => !force.Occupied);
        var guestResponse = await client.PostAsJsonAsync(
            $"/api/multiplayer/rooms/{host.Room.RoomId}/join",
            new StrategyMultiplayerJoinRoomRequest
            {
                PlayerName = "guest",
                ForceId = guestForce.ForceId
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, guestResponse.StatusCode);
        var guest = await guestResponse.Content.ReadFromJsonAsync<StrategyMultiplayerRoomResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(guest);

        var hostState = await GetState(host.Room.RoomId, host.Credentials.PlayerToken);
        var guestState = await GetState(host.Room.RoomId, guest.Credentials.PlayerToken);

        Assert.Equal(host.Credentials.ForceId, hostState.PlayerForceId);
        Assert.Equal(guest.Credentials.ForceId, guestState.PlayerForceId);
        Assert.NotEqual(hostState.PlayerForceId, guestState.PlayerForceId);
    }

    [Fact]
    public async Task Join_RejectsAlreadyOccupiedForce()
    {
        var host = await CreateRoom();
        var response = await client.PostAsJsonAsync(
            $"/api/multiplayer/rooms/{host.Room.RoomId}/join",
            new StrategyMultiplayerJoinRoomRequest
            {
                PlayerName = "intruder",
                ForceId = host.Credentials.ForceId
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(
            TestContext.Current.CancellationToken);
        Assert.Equal("ForceAlreadyOccupied", error!.ErrorCode);
    }

    [Fact]
    public async Task StrategyCommand_RequiresTokenAndRejectsDuplicateCommandId()
    {
        var host = await CreateRoom();

        var noToken = await client.GetAsync("/api/strategy/state", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, noToken.StatusCode);

        var missingTokenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/strategy/state");
        missingTokenRequest.Headers.Add(StrategyMultiplayerHeaders.RoomId, host.Room.RoomId);
        var missingToken = await client.SendAsync(missingTokenRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, missingToken.StatusCode);

        var state = await GetState(host.Room.RoomId, host.Credentials.PlayerToken);
        var target = state.Units.First(unit => unit.ForceId == host.Credentials.ForceId);
        var commandId = Guid.NewGuid().ToString("N");
        var first = await SendRoomJson(
            HttpMethod.Post,
            $"/api/strategy/units/{target.Id}/directive",
            host.Room.RoomId,
            host.Credentials.PlayerToken,
            new SetUnitDirectiveRequest { Directive = "Retreat" },
            commandId);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var duplicate = await SendRoomJson(
            HttpMethod.Post,
            $"/api/strategy/units/{target.Id}/directive",
            host.Room.RoomId,
            host.Credentials.PlayerToken,
            new SetUnitDirectiveRequest { Directive = "Retreat" },
            commandId);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task StrategyCommand_CannotControlAnotherForcesUnit()
    {
        var host = await CreateRoom();
        var state = await GetState(host.Room.RoomId, host.Credentials.PlayerToken);
        var enemyUnit = state.Units.First(unit => unit.ForceId != host.Credentials.ForceId);

        var response = await SendRoomJson(
            HttpMethod.Post,
            $"/api/strategy/units/{enemyUnit.Id}/directive",
            host.Room.RoomId,
            host.Credentials.PlayerToken,
            new SetUnitDirectiveRequest { Directive = "Retreat" },
            Guid.NewGuid().ToString("N"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(
            TestContext.Current.CancellationToken);
        Assert.Equal("NotSelfForce", error!.ErrorCode);
    }

    [Fact]
    public async Task Ready_AllConnectedPlayersAdvancesExactlyOneDay()
    {
        var host = await CreateRoom();
        var guestForce = host.Room.Forces.First(force => !force.Occupied);
        var joined = await client.PostAsJsonAsync(
            $"/api/multiplayer/rooms/{host.Room.RoomId}/join",
            new StrategyMultiplayerJoinRoomRequest { PlayerName = "guest", ForceId = guestForce.ForceId },
            TestContext.Current.CancellationToken);
        var guest = await joined.Content.ReadFromJsonAsync<StrategyMultiplayerRoomResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(guest);

        var before = await GetState(host.Room.RoomId, host.Credentials.PlayerToken);
        var hostReady = await Ready(host.Room.RoomId, host.Credentials.PlayerToken);
        Assert.False(hostReady.Advanced);
        Assert.Equal(0, hostReady.Advance.DaysAdvanced);

        var guestReady = await Ready(host.Room.RoomId, guest.Credentials.PlayerToken);
        Assert.True(guestReady.Advanced);
        Assert.Equal(1, guestReady.Advance.DaysAdvanced);

        var after = await GetState(host.Room.RoomId, host.Credentials.PlayerToken);
        Assert.Equal(before.Date.Year, after.Date.Year);
        Assert.Equal(before.Date.Month, after.Date.Month);
        Assert.Equal(before.Date.Day + 1, after.Date.Day);
        Assert.All(guestReady.Room.Players, player => Assert.False(player.Ready));
    }

    [Fact]
    public async Task Multiplayer_BlocksDirectAdvanceAndSaveEndpoints()
    {
        var host = await CreateRoom();
        var advance = await SendRoomJson(
            HttpMethod.Post,
            "/api/strategy/advance-day",
            host.Room.RoomId,
            host.Credentials.PlayerToken,
            new { },
            Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Forbidden, advance.StatusCode);

        var save = new HttpRequestMessage(HttpMethod.Get, "/api/strategy/save");
        save.Headers.Add(StrategyMultiplayerHeaders.RoomId, host.Room.RoomId);
        save.Headers.Add(StrategyMultiplayerHeaders.PlayerToken, host.Credentials.PlayerToken);
        var saveResponse = await client.SendAsync(save, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, saveResponse.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/strategy/save/")]
    [InlineData("POST", "/api/strategy/load/")]
    [InlineData("GET", "/api/strategy/debug/movement-trace")]
    [InlineData("GET", "/strategy/debug/ai-decision-trace/")]
    [InlineData("GET", "/api/strategy/debug/day-log")]
    [InlineData("POST", "/api/strategy/espionage-intel")]
    public async Task RestrictedActions_CannotBypassRoomPolicy(string method, string path)
    {
        var host = await CreateRoom();
        var response = await SendRoomJson(new HttpMethod(method), path, host.Room.RoomId,
            host.Credentials.PlayerToken,
            new
            {
                scenarioId = "mini_kanto",
                targetKind = "Stronghold",
                targetId = 2,
                scope = "Military",
                precision = "Exact"
            }, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ready_RejectsDelayedPreviousTurnEvenWithNewCommandId()
    {
        var host = await CreateRoom();
        var first = await Ready(host.Room.RoomId, host.Credentials.PlayerToken);
        Assert.True(first.Advanced);
        var delayed = await SendRoomJson(HttpMethod.Post,
            $"/api/multiplayer/rooms/{host.Room.RoomId}/ready", host.Room.RoomId,
            host.Credentials.PlayerToken,
            new StrategyMultiplayerReadyRequest { ExpectedTurn = 0 }, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.Conflict, delayed.StatusCode);
        var room = await client.GetFromJsonAsync<StrategyMultiplayerRoomDto>(
            $"/api/multiplayer/rooms/{host.Room.RoomId}", TestContext.Current.CancellationToken);
        Assert.Equal(1, room!.TurnNumber);
    }

    [Fact]
    public async Task Rooms_AdvanceIndependentlyAndCloseAfterLastLeave()
    {
        var first = await CreateRoom();
        var second = await CreateRoom();
        var before = await GetState(second.Room.RoomId, second.Credentials.PlayerToken);
        await Ready(first.Room.RoomId, first.Credentials.PlayerToken);
        var after = await GetState(second.Room.RoomId, second.Credentials.PlayerToken);
        Assert.Equal(before.Date.Day, after.Date.Day);
        var leave = await SendRoomJson(HttpMethod.Post,
            $"/api/multiplayer/rooms/{first.Room.RoomId}/leave", first.Room.RoomId,
            first.Credentials.PlayerToken, new { }, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.OK, leave.StatusCode);
        var missing = await client.GetAsync($"/api/multiplayer/rooms/{first.Room.RoomId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_RequiresCredentialsAndDoesNotAdvanceWorldVersion()
    {
        var host = await CreateRoom();
        var path = $"/api/multiplayer/rooms/{host.Room.RoomId}/heartbeat";
        var anonymous = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        var response = await SendRoomJson(HttpMethod.Get, path, host.Room.RoomId,
            host.Credentials.PlayerToken, new { }, Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var room = await response.Content.ReadFromJsonAsync<StrategyMultiplayerRoomDto>(
            TestContext.Current.CancellationToken);
        Assert.Equal(host.Room.WorldVersion, room!.WorldVersion);
        Assert.All(room.Players, player => Assert.True(player.Connected));
    }

    private async Task<StrategyMultiplayerRoomResponse> CreateRoom()
    {
        var response = await client.PostAsJsonAsync(
            "/api/multiplayer/rooms",
            new StrategyMultiplayerCreateRoomRequest
            {
                RoomName = $"room-{Guid.NewGuid():N}",
                PlayerName = "host",
                ScenarioId = "mini_kanto",
                Difficulty = "Easy",
                ForceId = 1,
                MaxPlayers = 8
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<StrategyMultiplayerRoomResponse>(
            TestContext.Current.CancellationToken);
        return Assert.IsType<StrategyMultiplayerRoomResponse>(payload);
    }

    private async Task<StrategyWorldStateDto> GetState(string roomId, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/strategy/state");
        request.Headers.Add(StrategyMultiplayerHeaders.RoomId, roomId);
        request.Headers.Add(StrategyMultiplayerHeaders.PlayerToken, token);
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<StrategyWorldStateDto>(
            TestContext.Current.CancellationToken);
        return Assert.IsType<StrategyWorldStateDto>(state);
    }

    private async Task<StrategyMultiplayerReadyResponse> Ready(string roomId, string token)
    {
        var room = await client.GetFromJsonAsync<StrategyMultiplayerRoomDto>(
            $"/api/multiplayer/rooms/{roomId}", TestContext.Current.CancellationToken);
        var response = await SendRoomJson(
            HttpMethod.Post,
            $"/api/multiplayer/rooms/{roomId}/ready",
            roomId,
            token,
            new StrategyMultiplayerReadyRequest { Ready = true, ExpectedTurn = room!.TurnNumber },
            Guid.NewGuid().ToString("N"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<StrategyMultiplayerReadyResponse>(
            TestContext.Current.CancellationToken);
        return Assert.IsType<StrategyMultiplayerReadyResponse>(payload);
    }

    private async Task<HttpResponseMessage> SendRoomJson<T>(
        HttpMethod method,
        string path,
        string roomId,
        string token,
        T value,
        string commandId)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.Add(StrategyMultiplayerHeaders.RoomId, roomId);
        request.Headers.Add(StrategyMultiplayerHeaders.PlayerToken, token);
        request.Headers.Add(StrategyMultiplayerHeaders.CommandId, commandId);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
