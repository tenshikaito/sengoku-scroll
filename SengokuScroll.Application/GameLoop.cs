using SengokuScroll.Application.Models;

namespace SengokuScroll.Application;

public interface IGameLoop : IEngineLoop
{
    Action? BeforeNextTime { get; set; }

    Action? NextTime { get; set; }

    Action? AfterNextTime { get; set; }
}

public abstract class GameLoopBase : IGameLoop
{
    private CancellationTokenSource? cts;
    private Task? task;

    private readonly ManualResetEventSlim pauseEvent = new(true);

    protected abstract int NextTurnIntervalMiliseconds { get; }

    public Action? BeforeNextTime { get; set; }

    public Action? NextTime { get; set; }

    public Action? AfterNextTime { get; set; }

    public void Start(bool isPause = false)
    {
        if (task != null)
            return;

        if (isPause)
            pauseEvent.Set();

        cts = new CancellationTokenSource();
        var token = cts.Token;

        task = Task.Run(async () =>
        {
            var interval = TimeSpan.FromMilliseconds(NextTurnIntervalMiliseconds);

            while (!token.IsCancellationRequested)
            {
                pauseEvent.Wait(token);

                var start = DateTime.UtcNow;

                if (CanNextTime())
                {
                    BeforeNextTime?.Invoke();

                    NextTime?.Invoke();

                    AfterNextTime?.Invoke();
                }

                var elapsed = DateTime.UtcNow - start;
                var delay = interval - elapsed;

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, token);
            }
        }, token);
    }

    protected abstract bool CanNextTime();

    public void Pause() => pauseEvent.Reset();

    public void Resume() => pauseEvent.Set();

    public async Task StopAsync()
    {
        if (cts == null)
            return;

        cts.Cancel();

        try
        {
            if (task != null)
                await task;
        }
        catch (OperationCanceledException)
        {
        }

        cts.Dispose();
        cts = null;
        task = null;
    }
}

public class GameEventLoop(GameSystemConfig config) : GameLoopBase
{
    protected override int NextTurnIntervalMiliseconds => config.MovingNextTurnIntervalMilisecond;

    protected override bool CanNextTime() => true;
}

public class GameTimeLoop : GameLoopBase
{
    protected override int NextTurnIntervalMiliseconds => 1000;

    protected override bool CanNextTime() => true;
}

//public class GameTimeLoop(CommandDispatcher commandExecutor) : IGameLoop
//{
//    private readonly CommandQueue queue = new();
//    private Task? worker;

//    public Task<GameResult> EnqueueCommandAsync(ICommand cmd)
//        => queue.EnqueueAsync(cmd);

//    public void Start()
//    {
//        if (worker != null && !worker.IsCompleted)
//            return;

//        worker = ProcessAsync();
//    }

//    public void Pause()
//    {
//        throw new NotImplementedException();
//    }

//    public void Resume()
//    {
//        throw new NotImplementedException();
//    }

//    public async Task StopAsync()
//    {
//        queue.Complete();

//        if (worker != null)
//        {
//            await worker;
//        }
//    }

//    private async Task ProcessAsync()
//    {
//        await foreach (var env in queue.Reader.ReadAllAsync())
//        {
//            try
//            {
//                var result = commandExecutor.HandleCommand(env.Command, context);

//                env.Tcs.SetResult(result);
//            }
//            catch (Exception ex)
//            {
//                env.Tcs.SetException(ex);
//            }
//        }
//    }
//}
