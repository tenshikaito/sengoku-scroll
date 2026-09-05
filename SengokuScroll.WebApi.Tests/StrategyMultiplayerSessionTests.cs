using SengokuScroll.Strategy.Hosting;
using SengokuScroll.WebApi.Multiplayer;

namespace SengokuScroll.WebApi.Tests;

public sealed class StrategyMultiplayerSessionTests
{
    private static StrategyMultiplayerRoomSession Create(TimeProvider? clock = null)
        => new("test", "test", "mini_kanto", 8, new StrategySimulationHost(),
            [new(1, "one", "Military"), new(2, "two", "Military")], clock);

    [Fact]
    public void RetriedCommand_RemainsDeduplicatedUntilItsOwnEviction()
    {
        using var room = Create();
        Assert.True(room.TryReserveCommandId("retry"));
        room.ReleaseCommandId("retry");
        for (var i = 0; i < 2047; i++) Assert.True(room.TryReserveCommandId($"other-{i}"));
        Assert.True(room.TryReserveCommandId("retry"));
        Assert.True(room.TryReserveCommandId("new"));
        Assert.False(room.TryReserveCommandId("retry"));
    }

    [Fact]
    public void Lease_ExpiresOnlyInactivePlayerAndReconnectRestoresPresence()
    {
        var clock = new TestClock();
        using var room = Create(clock);
        var first = room.AddPlayer("first", 1);
        var second = room.AddPlayer("second", 2);
        room.SetReady(first, true);
        clock.Advance(TimeSpan.FromSeconds(13));
        room.MarkConnected(second);
        room.SetReady(second, true);
        Assert.True(room.AreAllConnectedPlayersReady());
        Assert.False(first.Connected);
        Assert.False(first.Ready);
        Assert.True(room.TryReconnect(first.PlayerId, first.PlayerToken, out _));
        Assert.False(room.AreAllConnectedPlayersReady());
    }

    [Fact]
    public async Task ClosedRoom_AllowsQueuedRequestToWakeAndRejectsRevokedToken()
    {
        using var room = Create();
        var player = room.AddPlayer("first", 1);
        await room.Gate.WaitAsync(TestContext.Current.CancellationToken);
        var waiter = room.Gate.WaitAsync(TestContext.Current.CancellationToken);
        room.Dispose();
        room.Gate.Release();
        await waiter;
        try
        {
            Assert.True(room.IsClosed);
            Assert.False(room.TryAuthenticate(player.PlayerToken, out _));
            Assert.Throws<StrategyMultiplayerException>(() => room.AddPlayer("late", 2));
        }
        finally { room.Gate.Release(); }
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }
}
