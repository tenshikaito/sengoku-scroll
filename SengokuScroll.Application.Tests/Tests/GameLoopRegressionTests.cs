namespace SengokuScroll.Application.Tests.Tests;

public sealed class GameLoopRegressionTests
{
    [Fact]
    public async Task PausedStart_WaitsForResumeAndCanRestartAfterStop()
    {
        var loop = new FastLoop();
        var advanced = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loop.NextTime = () => advanced.TrySetResult();
        loop.Start(isPause: true);
        try
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.False(advanced.Task.IsCompleted);
            loop.Resume();
            await advanced.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        finally { await loop.StopAsync(); }

        advanced = new(TaskCreationOptions.RunContinuationsAsynchronously);
        loop.Pause();
        loop.Start(isPause: false);
        try { await advanced.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken); }
        finally { await loop.StopAsync(); }
    }

    [Fact]
    public async Task FaultedLoop_ReportsFailureButCanBeStoppedAndRestarted()
    {
        var loop = new FastLoop();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loop.NextTime = () =>
        {
            entered.TrySetResult();
            throw new InvalidOperationException("simulation failed");
        };
        loop.Start();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(loop.StopAsync);

        var restarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loop.NextTime = () => restarted.TrySetResult();
        loop.Start();
        try { await restarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken); }
        finally { await loop.StopAsync(); }
    }

    [Fact]
    public async Task ConcurrentStartAndStop_OnlyOneLoopOwnsTheLifecycle()
    {
        var loop = new FastLoop();
        var entries = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        loop.NextTime = () =>
        {
            Interlocked.Increment(ref entries);
            entered.TrySetResult();
            release.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        };
        Parallel.For(0, 64, _ => loop.Start(isPause: true));
        loop.Resume();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            var stops = Enumerable.Range(0, 16).Select(_ => loop.StopAsync()).ToArray();
            release.Set();
            await Task.WhenAll(stops).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(1, entries);
        }
        finally
        {
            release.Set();
            await loop.StopAsync();
        }
    }

    [Fact]
    public async Task StopWhilePaused_DoesNotRequireResume()
    {
        var loop = new FastLoop();
        loop.NextTime = () => Assert.Fail("Paused loop advanced");
        loop.Start(isPause: true);
        await loop.StopAsync().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    private sealed class FastLoop : GameLoopBase
    {
        protected override int NextTurnIntervalMiliseconds => 10;
        protected override bool CanNextTime() => true;
    }
}
