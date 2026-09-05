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

    private sealed class FastLoop : GameLoopBase
    {
        protected override int NextTurnIntervalMiliseconds => 10;
        protected override bool CanNextTime() => true;
    }
}
