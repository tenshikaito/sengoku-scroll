using System.Collections.Concurrent;
using System.Security.Cryptography;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Models;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.WebApi.Models;

namespace SengokuScroll.WebApi.Multiplayer;

public sealed class StrategyMultiplayerRoomManager : IDisposable
{
    public const int MaximumRoomCount = 64;

    private readonly ConcurrentDictionary<string, StrategyMultiplayerRoomSession> rooms =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<StrategyMultiplayerRoomDto> ListRooms()
        => rooms.Values
            .OrderBy(room => room.RoomName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(room => room.RoomId, StringComparer.Ordinal)
            .Select(room => room.ToDto())
            .ToList();

    public bool TryGetRoom(string roomId, out StrategyMultiplayerRoomSession room)
        => rooms.TryGetValue(NormalizeRoomId(roomId), out room!);

    public bool TryRemoveRoom(string roomId)
    {
        if (!rooms.TryRemove(NormalizeRoomId(roomId), out var room))
            return false;
        room.Dispose();
        return true;
    }

    public IReadOnlyList<StrategyMultiplayerForceDto> ListPlayableForces(string scenarioId)
    {
        using var host = new StrategySimulationHost();
        var loaded = host.LoadScenario(
            string.IsNullOrWhiteSpace(scenarioId) ? "mini_kanto" : scenarioId.Trim(),
            new StrategyLoadOptions { Difficulty = StrategyDifficulty.Normal });
        if (!loaded.IsSuccess)
            throw new StrategyMultiplayerException(loaded.Error?.Code ?? "ScenarioLoadFailed");

        return loaded.Value.Forces
            .Where(force => force.Category == "Military" && force.StrongholdCount > 0)
            .OrderBy(force => force.Id)
            .Select(force => new StrategyMultiplayerForceDto
            {
                ForceId = force.Id,
                ForceName = force.Name,
                Category = force.Category,
                Occupied = false
            })
            .ToList();
    }

    public StrategyMultiplayerRoomResponse CreateRoom(StrategyMultiplayerCreateRoomRequest request)
    {
        if (rooms.Count >= MaximumRoomCount)
            throw new StrategyMultiplayerException("RoomLimitReached");

        var roomName = NormalizeDisplayName(request.RoomName, "战国房间", 40);
        var playerName = NormalizeDisplayName(request.PlayerName, "玩家", 24);
        var scenarioId = string.IsNullOrWhiteSpace(request.ScenarioId)
            ? "mini_kanto"
            : request.ScenarioId.Trim();
        if (request.MaxPlayers is < 1 or > 8)
            throw new StrategyMultiplayerException("InvalidMaxPlayers");

        var difficulty = string.IsNullOrWhiteSpace(request.Difficulty)
            ? StrategyDifficulty.Normal
            : StrategyDifficultyRules.Parse(request.Difficulty);
        var loadOptions = new StrategyLoadOptions
        {
            Difficulty = difficulty,
            CustomStartOptions = difficulty == StrategyDifficulty.Custom && request.CustomStartOptions is not null
                ? GameStartOptionsMapper.FromDto(request.CustomStartOptions)
                : null,
            AllForcesAiControlled = false,
        };

        var host = new StrategySimulationHost();
        var loaded = host.LoadScenario(scenarioId, loadOptions);
        if (!loaded.IsSuccess)
        {
            host.Dispose();
            throw new StrategyMultiplayerException(loaded.Error?.Code ?? "ScenarioLoadFailed");
        }

        var playableForces = loaded.Value.Forces
            .Where(force => force.Category == "Military" && force.StrongholdCount > 0)
            .OrderBy(force => force.Id)
            .Select(force => new StrategyMultiplayerForceDefinition(
                force.Id,
                force.Name,
                force.Category))
            .ToArray();
        if (!playableForces.Any(force => force.ForceId == request.ForceId))
        {
            host.Dispose();
            throw new StrategyMultiplayerException("ForceNotPlayable");
        }

        var roomId = Enumerable.Range(0, 8)
            .Select(_ => CreateRoomId())
            .FirstOrDefault(candidate => !rooms.ContainsKey(candidate));
        if (roomId is null)
        {
            host.Dispose();
            throw new StrategyMultiplayerException("RoomIdGenerationFailed");
        }

        var room = new StrategyMultiplayerRoomSession(
            roomId,
            roomName,
            scenarioId,
            request.MaxPlayers,
            host,
            playableForces);
        var joined = room.AddPlayer(playerName, request.ForceId, isHost: true);
        room.RefreshHumanControlledForces();
        if (!rooms.TryAdd(roomId, room))
        {
            room.Dispose();
            throw new StrategyMultiplayerException("RoomIdGenerationFailed");
        }
        return new StrategyMultiplayerRoomResponse
        {
            Room = room.ToDto(),
            Credentials = joined.ToCredentials()
        };
    }

    public void Dispose()
    {
        foreach (var room in rooms.Values)
            room.Dispose();
        rooms.Clear();
    }

    private static string CreateRoomId()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(5));

    private static string NormalizeRoomId(string roomId)
        => roomId.Trim().ToUpperInvariant();

    internal static string NormalizeDisplayName(string? value, string fallback, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}

public sealed class StrategyMultiplayerRoomSession : IDisposable
{
    private const int ProcessedCommandCapacity = 2048;
    private static readonly TimeSpan ConnectionLease = TimeSpan.FromSeconds(12);
    private readonly object sync = new();
    private readonly Dictionary<string, StrategyMultiplayerPlayer> playersById =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, StrategyMultiplayerPlayer> playersByToken =
        new(StringComparer.Ordinal);
    private readonly HashSet<string> processedCommandIds = new(StringComparer.Ordinal);
    private readonly Queue<string> processedCommandOrder = new();
    private long worldVersion = 1;
    private bool hasStarted;

    public StrategyMultiplayerRoomSession(
        string roomId,
        string roomName,
        string scenarioId,
        int maxPlayers,
        StrategySimulationHost host,
        IReadOnlyList<StrategyMultiplayerForceDefinition> forces)
    {
        RoomId = roomId;
        RoomName = roomName;
        ScenarioId = scenarioId;
        MaxPlayers = maxPlayers;
        Host = host;
        Forces = forces;
    }

    public string RoomId { get; }

    public string RoomName { get; }

    public string ScenarioId { get; }

    public int MaxPlayers { get; }

    public StrategySimulationHost Host { get; }

    public IReadOnlyList<StrategyMultiplayerForceDefinition> Forces { get; }

    public SemaphoreSlim Gate { get; } = new(1, 1);

    public long WorldVersion => Interlocked.Read(ref worldVersion);

    public int PlayerCount
    {
        get
        {
            lock (sync)
                return playersById.Count;
        }
    }

    public StrategyMultiplayerPlayer AddPlayer(string playerName, int forceId, bool isHost = false)
    {
        lock (sync)
        {
            if (playersById.Count >= MaxPlayers)
                throw new StrategyMultiplayerException("RoomFull");
            if (!Forces.Any(force => force.ForceId == forceId))
                throw new StrategyMultiplayerException("ForceNotPlayable");
            if (playersById.Values.Any(player => player.ForceId == forceId))
                throw new StrategyMultiplayerException("ForceAlreadyOccupied");

            var player = new StrategyMultiplayerPlayer(
                Guid.NewGuid().ToString("N"),
                Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
                StrategyMultiplayerRoomManager.NormalizeDisplayName(playerName, "玩家", 24),
                forceId,
                isHost);
            playersById.Add(player.PlayerId, player);
            playersByToken.Add(player.PlayerToken, player);
            return player;
        }
    }

    public bool TryAuthenticate(string playerToken, out StrategyMultiplayerPlayer player)
    {
        lock (sync)
            return playersByToken.TryGetValue(playerToken, out player!);
    }

    public bool TryReconnect(string playerId, string playerToken, out StrategyMultiplayerPlayer player)
    {
        lock (sync)
        {
            if (!playersById.TryGetValue(playerId, out player!)
                || !string.Equals(player.PlayerToken, playerToken, StringComparison.Ordinal))
            {
                player = null!;
                return false;
            }

            player.Connected = true;
            player.LastSeenUtc = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public void MarkConnected(StrategyMultiplayerPlayer player)
    {
        lock (sync)
        {
            player.Connected = true;
            player.LastSeenUtc = DateTimeOffset.UtcNow;
        }
    }

    public void MarkDisconnected(StrategyMultiplayerPlayer player)
    {
        lock (sync)
        {
            player.Connected = false;
            player.Ready = false;
        }
    }

    public void RemovePlayer(StrategyMultiplayerPlayer player)
    {
        lock (sync)
        {
            playersById.Remove(player.PlayerId);
            playersByToken.Remove(player.PlayerToken);
        }
    }

    public void SetReady(StrategyMultiplayerPlayer player, bool ready)
    {
        lock (sync)
            player.Ready = ready;
    }

    public bool AreAllConnectedPlayersReady()
    {
        lock (sync)
        {
            RefreshConnectionStatesNoLock();
            var connected = playersById.Values.Where(player => player.Connected).ToArray();
            return connected.Length > 0 && connected.All(player => player.Ready);
        }
    }

    public void ResetReady()
    {
        lock (sync)
        {
            foreach (var player in playersById.Values)
                player.Ready = false;
        }
    }

    public void RefreshHumanControlledForces()
    {
        int[] humanForceIds;
        lock (sync)
        {
            RefreshConnectionStatesNoLock();
            humanForceIds = playersById.Values
                .Where(player => player.Connected)
                .Select(player => player.ForceId)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();
        }

        var configured = Host.ConfigureHumanControlledForces(humanForceIds);
        if (!configured.IsSuccess)
            throw new StrategyMultiplayerException(configured.Error?.Code ?? "HumanForceConfigurationFailed");
    }

    public bool TryReserveCommandId(string commandId)
    {
        lock (sync)
        {
            if (!processedCommandIds.Add(commandId))
                return false;

            processedCommandOrder.Enqueue(commandId);
            while (processedCommandOrder.Count > ProcessedCommandCapacity)
                processedCommandIds.Remove(processedCommandOrder.Dequeue());
            return true;
        }
    }

    public void ReleaseCommandId(string commandId)
    {
        lock (sync)
            processedCommandIds.Remove(commandId);
    }

    public long MarkWorldChanged(bool started = false)
    {
        if (started)
        {
            lock (sync)
                hasStarted = true;
        }
        return Interlocked.Increment(ref worldVersion);
    }

    public StrategyMultiplayerRoomDto ToDto()
    {
        lock (sync)
        {
            RefreshConnectionStatesNoLock();
            var occupiedForceIds = playersById.Values.Select(player => player.ForceId).ToHashSet();
            return new StrategyMultiplayerRoomDto
            {
                RoomId = RoomId,
                RoomName = RoomName,
                ScenarioId = ScenarioId,
                Status = hasStarted ? "Running" : "Waiting",
                MaxPlayers = MaxPlayers,
                PlayerCount = playersById.Count,
                WorldVersion = WorldVersion,
                Players = playersById.Values
                    .OrderByDescending(player => player.IsHost)
                    .ThenBy(player => player.PlayerName, StringComparer.OrdinalIgnoreCase)
                    .Select(player => player.ToDto())
                    .ToList(),
                Forces = Forces
                    .Select(force => new StrategyMultiplayerForceDto
                    {
                        ForceId = force.ForceId,
                        ForceName = force.ForceName,
                        Category = force.Category,
                        Occupied = occupiedForceIds.Contains(force.ForceId)
                    })
                    .ToList()
            };
        }
    }

    private void RefreshConnectionStatesNoLock()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var player in playersById.Values)
        {
            if (player.Connected && now - player.LastSeenUtc > ConnectionLease)
            {
                player.Connected = false;
                player.Ready = false;
            }
        }
    }

    public void Dispose()
    {
        Host.Dispose();
        Gate.Dispose();
    }
}

public sealed class StrategyMultiplayerPlayer(
    string playerId,
    string playerToken,
    string playerName,
    int forceId,
    bool isHost)
{
    public string PlayerId { get; } = playerId;

    public string PlayerToken { get; } = playerToken;

    public string PlayerName { get; } = playerName;

    public int ForceId { get; } = forceId;

    public bool IsHost { get; } = isHost;

    public bool Ready { get; set; }

    public bool Connected { get; set; } = true;

    public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;

    public StrategyMultiplayerCredentialsDto ToCredentials()
        => new()
        {
            PlayerId = PlayerId,
            PlayerToken = PlayerToken,
            ForceId = ForceId,
            IsHost = IsHost
        };

    public StrategyMultiplayerPlayerDto ToDto()
        => new()
        {
            PlayerId = PlayerId,
            PlayerName = PlayerName,
            ForceId = ForceId,
            IsHost = IsHost,
            Ready = Ready,
            Connected = Connected
        };
}

public sealed record StrategyMultiplayerForceDefinition(
    int ForceId,
    string ForceName,
    string Category);

public sealed class StrategyMultiplayerException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
