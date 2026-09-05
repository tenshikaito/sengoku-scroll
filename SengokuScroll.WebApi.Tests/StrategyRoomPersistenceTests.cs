using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.WebApi.Models;
using SengokuScroll.WebApi.Multiplayer;

namespace SengokuScroll.WebApi.Tests;

public sealed class StrategyRoomPersistenceTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "sengoku-room-test-" + Guid.NewGuid().ToString("N"));
    private StrategyMultiplayerRoomManager CreateManager() => new(
        Options.Create(new StrategyMultiplayerOptions { StoragePath = directory }),
        NullLoggerFactory.Instance, Options.Create(new StrategyDayDebugOptions()),
        Options.Create(new StrategyAiTraceOptions()), new TestEnvironment { ContentRootPath = directory });

    [Fact]
    public void Restart_RestoresWorldCredentialsTurnDedupAndPrivateMailboxes()
    {
        string roomId, playerId, token, saved;
        using (var manager = CreateManager())
        {
            var created = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 2, Difficulty = "Easy" });
            roomId = created.Room.RoomId; playerId = created.Credentials.PlayerId; token = created.Credentials.PlayerToken;
            Assert.True(manager.TryGetRoom(roomId, out var room));
            Assert.True(room.TryAuthenticate(token, out var player));
            Assert.True(room.Host.AdvanceDays(31).IsSuccess);
            room.MarkWorldChanged(started: true);
            Assert.True(room.TryReserveCommandId(playerId + ":committed"));
            room.SetReady(player, true);
            manager.Persist(room);
            saved = StrategySimulationHost.SerializeSave(room.Host.CaptureSave().Value);
            Assert.NotEmpty(room.Host.ReadPrivateEvents(1).Entries);
            Assert.DoesNotContain(token, System.Text.Json.JsonSerializer.Serialize(manager.ListRooms()));
        }
        using (var recovered = CreateManager())
        {
            Assert.True(recovered.TryGetRoom(roomId, out var room));
            Assert.Equal(1, room.TurnNumber);
            Assert.Equal(2, room.WorldVersion);
            Assert.False(Assert.Single(room.ToDto().Players).Connected);
            Assert.False(Assert.Single(room.ToDto().Players).Ready);
            Assert.Equal(saved, StrategySimulationHost.SerializeSave(room.Host.CaptureSave().Value));
            Assert.True(room.TryReconnect(playerId, token, out var player));
            Assert.False(room.TryReserveCommandId(playerId + ":committed"));
            Assert.NotEmpty(room.Host.ReadPrivateEvents(1).Entries);
            Assert.True(room.Host.AcknowledgePrivateEvents(1, room.Host.ReadPrivateEvents(1).LastSequence));
            recovered.Persist(room);
        }
        using var again = CreateManager();
        Assert.True(again.TryGetRoom(roomId, out var restored));
        Assert.Empty(restored.Host.ReadPrivateEvents(1).Entries);
        Assert.True(again.TryRemoveRoom(roomId));
        Assert.False(File.Exists(Path.Combine(directory, roomId + ".json")));
    }

    [Fact]
    public void InvalidSnapshot_IsPreservedAndHealthyRoomStillRecovers()
    {
        string id;
        using (var manager = CreateManager()) id = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 1 }).Room.RoomId;
        var corrupt = Path.Combine(directory, "ABCDEF1234.json");
        File.WriteAllText(corrupt, "{broken");
        using var restored = CreateManager();
        Assert.True(restored.TryGetRoom(id, out _));
        Assert.Single(restored.ListRooms());
        Assert.Equal("{broken", File.ReadAllText(corrupt));
    }

    [Fact]
    public void CorruptRuntime_IsPreservedAndNotLoadedAsPartialRoom()
    {
        string id;
        using (var manager = CreateManager()) id = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 1 }).Room.RoomId;
        var path = Path.Combine(directory, id + ".json");
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        var save = root.AsObject().First(x => x.Value is System.Text.Json.Nodes.JsonObject obj
            && obj.Any(p => p.Key.Equals("runtimeState", StringComparison.OrdinalIgnoreCase))).Value!.AsObject();
        var runtimeKey = save.First(x => x.Key.Equals("runtimeState", StringComparison.OrdinalIgnoreCase)).Key;
        save[runtimeKey]!["Units"] = null;
        var corrupted = root.ToJsonString();
        File.WriteAllText(path, corrupted);
        using var recovered = CreateManager();
        Assert.False(recovered.TryGetRoom(id, out _));
        Assert.Equal(corrupted, File.ReadAllText(path));
    }

    [Fact]
    public void StorageFailure_SuspendsRoomAndDoesNotReleaseCommittedCommand()
    {
        using var manager = CreateManager();
        var created = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 1 });
        Assert.True(manager.TryGetRoom(created.Room.RoomId, out var room));
        var target = Path.Combine(directory, room.RoomId + ".json");
        File.Move(target, target + ".backup");
        Directory.CreateDirectory(target); // deterministic atomic-replace failure
        Assert.True(room.TryReserveCommandId("committed"));
        var error = Assert.Throws<StrategyMultiplayerException>(() => manager.Persist(room));
        Assert.Equal("RoomStorageFailed", error.Code);
        Assert.Equal("StorageError", room.ToDto().Status);
        Assert.False(room.TryReserveCommandId("committed"));
        Assert.Throws<StrategyMultiplayerException>(() => room.TryAuthenticate(created.Credentials.PlayerToken, out _));
        Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        Assert.True(File.Exists(target + ".backup"));
    }

    [Fact]
    public void Hibernate_PreservesWorldCredentialsAndCommandDedupAndWakesOnDemand()
    {
        using var manager = CreateManager();
        var created = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 1 });
        Assert.True(manager.TryGetRoom(created.Room.RoomId, out var room));
        Assert.True(room.TryAuthenticate(created.Credentials.PlayerToken, out var player));
        Assert.True(room.Host.AdvanceDays(3).IsSuccess);
        room.TryReserveCommandId("before-sleep");
        var before = StrategySimulationHost.SerializeSave(room.Host.CaptureSave().Value);
        player.LastSeenUtc = DateTimeOffset.UtcNow.AddHours(-1);
        manager.HibernateIdleRooms();
        Assert.True(room.IsHibernating);
        Assert.Single(manager.ListRooms());
        Assert.True(room.IsHibernating); // listing must not wake a world
        Assert.True(File.Exists(Path.Combine(directory, room.RoomId + ".json")));
        Assert.True(room.TryReconnect(created.Credentials.PlayerId, created.Credentials.PlayerToken, out _));
        Assert.Equal(before, StrategySimulationHost.SerializeSave(room.Host.CaptureSave().Value));
        Assert.False(room.IsHibernating);
        Assert.False(room.TryReserveCommandId("before-sleep"));
    }

    [Fact]
    public void Hibernate_DoesNotInterruptActiveRoomOrBusyGate()
    {
        using var manager = CreateManager();
        var created = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 1 });
        Assert.True(manager.TryGetRoom(created.Room.RoomId, out var room));
        manager.HibernateIdleRooms();
        Assert.False(room.IsHibernating);
        Assert.True(room.TryAuthenticate(created.Credentials.PlayerToken, out var player));
        player.LastSeenUtc = DateTimeOffset.UtcNow.AddHours(-1);
        room.Gate.Wait(TestContext.Current.CancellationToken);
        try { manager.HibernateIdleRooms(); Assert.False(room.IsHibernating); }
        finally { room.Gate.Release(); }
        manager.HibernateIdleRooms();
        Assert.True(room.IsHibernating);
    }

    [Fact]
    public void Hibernate_StorageFailureKeepsWorldInMemory()
    {
        using var manager = CreateManager();
        var created = manager.CreateRoom(new() { ForceId = 1, MaxPlayers = 1 });
        Assert.True(manager.TryGetRoom(created.Room.RoomId, out var room));
        Assert.True(room.TryAuthenticate(created.Credentials.PlayerToken, out var player));
        var original = room.Host;
        var path = Path.Combine(directory, room.RoomId + ".json");
        File.Move(path, path + ".backup");
        Directory.CreateDirectory(path);
        player.LastSeenUtc = DateTimeOffset.UtcNow.AddHours(-1);
        manager.HibernateIdleRooms();
        Assert.False(room.IsHibernating);
        Assert.True(room.StorageFailed);
        Assert.Same(original, room.Host);
        Assert.True(room.Host.CaptureSave().IsSuccess);
    }

    [Fact]
    public void Storage_RejectsPublicDirectoryAndConcurrentWriters()
    {
        Assert.Throws<InvalidOperationException>(() => new StrategyRoomStore(
            new() { StoragePath = "wwwroot/private" }, directory));
        using var manager = CreateManager();
        Assert.Throws<IOException>(() => CreateManager());
    }

    public void Dispose()
    {
        // Only this test's uniquely created directory is removed.
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "SengokuScroll";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
