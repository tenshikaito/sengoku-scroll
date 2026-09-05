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
    private readonly object creationSync = new();
    private readonly TimeSpan connectionLease;
    private readonly Func<StrategySimulationHost> createHost;
    private readonly StrategyRoomStore store;
    private readonly ILogger logger;
    private readonly TimeSpan? idleTimeout;

    public StrategyMultiplayerRoomManager(
        Microsoft.Extensions.Options.IOptions<StrategyMultiplayerOptions> options,
        ILoggerFactory loggerFactory,
        Microsoft.Extensions.Options.IOptions<SengokuScroll.Strategy.Diagnostics.StrategyDayDebugOptions> dayDebugOptions,
        Microsoft.Extensions.Options.IOptions<SengokuScroll.Strategy.Diagnostics.StrategyAiTraceOptions> aiTraceOptions,
        IHostEnvironment environment)
    {
        connectionLease = TimeSpan.FromSeconds(options.Value.ConnectionLeaseSeconds);
        idleTimeout = options.Value.PersistenceEnabled && options.Value.IdleHibernateSeconds > 0
            ? TimeSpan.FromSeconds(options.Value.IdleHibernateSeconds) : null;
        createHost = () => new StrategySimulationHost(loggerFactory, dayDebugOptions, aiTraceOptions);
        logger = loggerFactory.CreateLogger<StrategyMultiplayerRoomManager>();
        store = new(options.Value, environment.ContentRootPath);
        foreach (var (path, snapshot) in store.ReadAll())
        {
            StrategySimulationHost? host = null;
            try
            {
                if (snapshot is null || snapshot.FormatVersion != 1 || !snapshot.World.IsMultiplayer
                    || snapshot.Players.Count is < 1 or > 8 || snapshot.MaxPlayers is < 1 or > 8
                    || snapshot.Players.Count > snapshot.MaxPlayers || snapshot.TurnNumber < 0 || snapshot.WorldVersion < 1
                    || snapshot.ProcessedCommands.Count > 2048 || rooms.Count >= MaximumRoomCount)
                    throw new InvalidOperationException("Invalid room snapshot");
                host = createHost();
                if (!host.RestoreSave(snapshot.World).IsSuccess) throw new InvalidOperationException("World restore failed");
                var room = new StrategyMultiplayerRoomSession(snapshot.RoomId, snapshot.RoomName, snapshot.ScenarioId,
                    snapshot.MaxPlayers, host, snapshot.Forces, connectionLease: connectionLease);
                room.RestoreSession(snapshot);
                ConfigureHibernation(room);
                room.RefreshHumanControlledForces();
                if (!rooms.TryAdd(room.RoomId, room)) throw new InvalidOperationException("Duplicate room");
                host = null;
            }
            catch (Exception ex)
            {
                host?.Dispose();
                // Preserve invalid files for manual recovery, never replace them during startup.
                logger.LogWarning("Skipped room snapshot {Path}: {ErrorType}", path, ex.GetType().Name);
            }
        }
    }

    // Caller holds Gate; serialize only after its player perspective lease has ended.
    private void ConfigureHibernation(StrategyMultiplayerRoomSession room)
        => room.SetWakeFactory(() =>
        {
            var host = createHost();
            try
            {
                var snapshot = store.Read(room.RoomId);
                if (snapshot.RoomId != room.RoomId || !host.RestoreSave(snapshot.World).IsSuccess)
                    throw new InvalidOperationException("Room wake failed");
                return host;
            }
            catch { host.Dispose(); throw; }
        });

    public void HibernateIdleRooms()
    {
        if (idleTimeout is not TimeSpan timeout) return;
        foreach (var room in rooms.Values)
        {
            if (!room.Gate.Wait(0)) continue;
            try { room.TryHibernate(timeout, () => Persist(room)); }
            catch (Exception ex) { logger.LogWarning("Room hibernation failed: {ErrorType}", ex.GetType().Name); }
            finally { room.Gate.Release(); }
        }
    }

    public void Persist(StrategyMultiplayerRoomSession room)
    {
        try { store.Write(room.CaptureSnapshot()); }
        catch (Exception ex)
        {
            room.StorageFailed = true;
            logger.LogError(ex, "Room storage failed for {RoomId}; room suspended until restart", room.RoomId);
            throw new StrategyMultiplayerException("RoomStorageFailed");
        }
    }

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
        try { store.Delete(NormalizeRoomId(roomId)); }
        catch (Exception)
        {
            if (TryGetRoom(roomId, out var failedRoom)) failedRoom.StorageFailed = true;
            throw new StrategyMultiplayerException("RoomStorageFailed");
        }
        if (!rooms.TryRemove(NormalizeRoomId(roomId), out var room))
            return false;
        // Called with the room gate held: waiters must be allowed to acquire it
        // and observe the closed state rather than race SemaphoreSlim.Dispose.
        room.Dispose();
        return true;
    }

    public IReadOnlyList<StrategyMultiplayerForceDto> ListPlayableForces(string scenarioId)
    {
        using var host = createHost();
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
        lock (creationSync)
            return CreateRoomCore(request);
    }

    private StrategyMultiplayerRoomResponse CreateRoomCore(StrategyMultiplayerCreateRoomRequest request)
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
            IsMultiplayer = true,
        };

        var host = createHost();
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
            .FirstOrDefault(candidate => !rooms.ContainsKey(candidate) && !store.Exists(candidate));
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
            playableForces,
            connectionLease: connectionLease);
        var joined = room.AddPlayer(playerName, request.ForceId, isHost: true);
        ConfigureHibernation(room);
        room.RefreshHumanControlledForces();
        try { Persist(room); }
        catch { room.Dispose(); throw; }
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
        store.Dispose();
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
    private readonly TimeSpan connectionLease;
    private readonly object sync = new();
    private readonly Dictionary<string, StrategyMultiplayerPlayer> playersById =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, StrategyMultiplayerPlayer> playersByToken =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, LinkedListNode<string>> processedCommandIds = new(StringComparer.Ordinal);
    private readonly LinkedList<string> processedCommandOrder = new();
    private readonly TimeProvider clock;
    private long worldVersion = 1;
    private long turnNumber;
    private bool hasStarted;
    private bool closed;
    private StrategySimulationHost? host;
    private Func<StrategySimulationHost>? wakeFactory;
    public bool IsHibernating { get { lock (sync) return !closed && host is null; } }
    internal void SetWakeFactory(Func<StrategySimulationHost> factory) => wakeFactory = factory;

    // Caller owns Gate; metadata and credentials remain resident while the world is on disk.
    public bool TryHibernate(TimeSpan idle, Action persist)
    {
        lock (sync)
        {
            RefreshConnectionStatesNoLock();
            if (closed || StorageFailed || host is null || wakeFactory is null || playersById.Count == 0
                || playersById.Values.Any(p => p.Connected || clock.GetUtcNow() - p.LastSeenUtc < idle)) return false;
            persist();
            host.Dispose();
            host = null;
            return true;
        }
    }
    public bool StorageFailed { get; internal set; }

    public StrategyRoomSnapshot CaptureSnapshot()
    {
        lock (sync)
        {
            var save = Host.CaptureSave();
            if (!save.IsSuccess) throw new InvalidOperationException("World capture failed");
            return new(1, RoomId, RoomName, ScenarioId, MaxPlayers, WorldVersion, TurnNumber, hasStarted,
                Forces, playersById.Values.OrderBy(p => p.PlayerId, StringComparer.Ordinal).ToArray(),
                processedCommandOrder.ToArray(), save.Value);
        }
    }

    public void RestoreSession(StrategyRoomSnapshot snapshot)
    {
        lock (sync)
        {
            foreach (var player in snapshot.Players)
            {
                if (string.IsNullOrWhiteSpace(player.PlayerId) || string.IsNullOrWhiteSpace(player.PlayerToken)
                    || !Forces.Any(f => f.ForceId == player.ForceId)
                    || playersById.Values.Any(p => p.ForceId == player.ForceId))
                    throw new InvalidOperationException("Invalid player snapshot");
                player.Connected = false;
                player.Ready = false;
                playersById.Add(player.PlayerId, player);
                playersByToken.Add(player.PlayerToken, player);
            }
            worldVersion = snapshot.WorldVersion;
            turnNumber = snapshot.TurnNumber;
            hasStarted = snapshot.HasStarted;
            foreach (var command in snapshot.ProcessedCommands) TryReserveCommandId(command);
        }
    }

    public StrategyMultiplayerRoomSession(
        string roomId,
        string roomName,
        string scenarioId,
        int maxPlayers,
        StrategySimulationHost host,
        IReadOnlyList<StrategyMultiplayerForceDefinition> forces,
        TimeProvider? clock = null,
        TimeSpan? connectionLease = null)
    {
        RoomId = roomId;
        RoomName = roomName;
        ScenarioId = scenarioId;
        MaxPlayers = maxPlayers;
        this.host = host;
        Forces = forces;
        this.clock = clock ?? TimeProvider.System;
        this.connectionLease = connectionLease ?? TimeSpan.FromSeconds(90);
        if (this.connectionLease <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(connectionLease));
    }

    public string RoomId { get; }

    public string RoomName { get; }

    public string ScenarioId { get; }

    public int MaxPlayers { get; }

    public StrategySimulationHost Host
    {
        get
        {
            lock (sync)
            {
                if (closed) throw new StrategyMultiplayerException("RoomNotFound");
                if (host is not null) return host;
                try { return host = wakeFactory!(); }
                catch { StorageFailed = true; throw new StrategyMultiplayerException("RoomStorageFailed"); }
            }
        }
    }

    public IReadOnlyList<StrategyMultiplayerForceDefinition> Forces { get; }

    public SemaphoreSlim Gate { get; } = new(1, 1);

    public long WorldVersion => Interlocked.Read(ref worldVersion);
    public long TurnNumber => Interlocked.Read(ref turnNumber);
    public bool IsClosed { get { lock (sync) return closed; } }

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
        if (StorageFailed) throw new StrategyMultiplayerException("RoomStorageFailed");
        lock (sync)
        {
            if (closed)
                throw new StrategyMultiplayerException("RoomNotFound");
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
            player.LastSeenUtc = clock.GetUtcNow();
            playersById.Add(player.PlayerId, player);
            playersByToken.Add(player.PlayerToken, player);
            return player;
        }
    }

    public bool TryAuthenticate(string playerToken, out StrategyMultiplayerPlayer player)
    {
        if (StorageFailed) throw new StrategyMultiplayerException("RoomStorageFailed");
        lock (sync)
        {
            player = null!;
            return !closed && playersByToken.TryGetValue(playerToken, out player!);
        }
    }

    public bool TryReconnect(string playerId, string playerToken, out StrategyMultiplayerPlayer player)
    {
        if (StorageFailed) throw new StrategyMultiplayerException("RoomStorageFailed");
        lock (sync)
        {
            if (closed || !playersById.TryGetValue(playerId, out player!)
                || !string.Equals(player.PlayerToken, playerToken, StringComparison.Ordinal))
            {
                player = null!;
                return false;
            }

            player.Connected = true;
            player.LastSeenUtc = clock.GetUtcNow();
            return true;
        }
    }

    public void MarkConnected(StrategyMultiplayerPlayer player)
    {
        lock (sync)
        {
            player.Connected = true;
            player.LastSeenUtc = clock.GetUtcNow();
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
            if (processedCommandIds.ContainsKey(commandId))
                return false;

            processedCommandIds.Add(commandId, processedCommandOrder.AddLast(commandId));
            while (processedCommandOrder.Count > ProcessedCommandCapacity)
            {
                processedCommandIds.Remove(processedCommandOrder.First!.Value);
                processedCommandOrder.RemoveFirst();
            }
            return true;
        }
    }

    public void ReleaseCommandId(string commandId)
    {
        lock (sync)
        {
            if (processedCommandIds.Remove(commandId, out var node))
                processedCommandOrder.Remove(node);
        }
    }

    public long MarkWorldChanged(bool started = false)
    {
        if (started)
        {
            lock (sync)
                hasStarted = true;
            Interlocked.Increment(ref turnNumber);
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
                Status = StorageFailed ? "StorageError" : hasStarted ? "Running" : "Waiting",
                MaxPlayers = MaxPlayers,
                PlayerCount = playersById.Count,
                WorldVersion = WorldVersion,
                TurnNumber = TurnNumber,
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
        var now = clock.GetUtcNow();
        foreach (var player in playersById.Values)
        {
            if (player.Connected && now - player.LastSeenUtc > connectionLease)
            {
                player.Connected = false;
                player.Ready = false;
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (closed) return;
            closed = true;
            playersById.Clear();
            playersByToken.Clear();
            host?.Dispose();
            host = null;
        }
        // Gate has no unmanaged resource unless AvailableWaitHandle is accessed.
        // Retain it for already queued requests; GC reclaims it with the room.
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
