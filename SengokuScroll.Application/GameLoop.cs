using SengokuScroll.Application.Models;

namespace SengokuScroll.Application;

/// <summary>游戏时间推进循环：按间隔调用 BeforeNextTime → NextTime → AfterNextTime。</summary>
public interface IGameLoop : IEngineLoop
{
    /// <summary>每回合/每日推进前钩子（如刷新 UI 缓冲）。</summary>
    Action? BeforeNextTime { get; set; }

    /// <summary>核心推进：通常绑定 <see cref="IGameEngine.NextTime"/>（日结算）。</summary>
    Action? NextTime { get; set; }

    /// <summary>推进后钩子（如刷快照、发事件）。</summary>
    Action? AfterNextTime { get; set; }
}

/// <summary>后台定时循环基类；暂停时异步等待，不占用线程池工作线程。</summary>
public abstract class GameLoopBase : IGameLoop
{
    private CancellationTokenSource? cts;
    private Task? task;

    private readonly object lifecycleSync = new();
    private TaskCompletionSource resumeSignal = CreateResumeSignal();

    private static TaskCompletionSource CreateResumeSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected abstract int NextTurnIntervalMiliseconds { get; }

    public Action? BeforeNextTime { get; set; }

    public Action? NextTime { get; set; }

    public Action? AfterNextTime { get; set; }

    /// <summary>启动后台循环；<paramref name="isPause"/> 为 true 时保持暂停态。</summary>
    public void Start(bool isPause = false)
    {
        lock (lifecycleSync)
        {
            if (task != null)
                return;

            resumeSignal = CreateResumeSignal();
            if (!isPause) resumeSignal.TrySetResult();

            cts = new CancellationTokenSource();
            var token = cts.Token;

            task = Task.Run(async () =>
            {
                var interval = TimeSpan.FromMilliseconds(NextTurnIntervalMiliseconds);

                while (!token.IsCancellationRequested)
                {
                    Task resume;
                    lock (lifecycleSync) resume = resumeSignal.Task;
                    await resume.WaitAsync(token);
                    token.ThrowIfCancellationRequested();

                    var start = System.Diagnostics.Stopwatch.GetTimestamp();

                    if (CanNextTime())
                    {
                        // 日推进三阶段：前处理 → 引擎结算 → 后处理
                        BeforeNextTime?.Invoke();

                        NextTime?.Invoke();

                        AfterNextTime?.Invoke();
                    }

                    var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(start);
                    var delay = interval - elapsed;

                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, token);
                }
            }, token);
        }
    }

    protected abstract bool CanNextTime();

    /// <summary>暂停下一次日/回合推进；已经开始的一次结算会正常完成。</summary>
    public void Pause()
    {
        lock (lifecycleSync)
            if (resumeSignal.Task.IsCompleted) resumeSignal = CreateResumeSignal();
    }

    /// <summary>解除暂停，继续推进。</summary>
    public void Resume()
    {
        lock (lifecycleSync) resumeSignal.TrySetResult();
    }

    /// <summary>取消循环并等待后台任务结束。</summary>
    public async Task StopAsync()
    {
        CancellationTokenSource stoppingCts;
        Task stoppingTask;
        lock (lifecycleSync)
        {
            if (cts is null || task is null) return;
            stoppingCts = cts;
            stoppingTask = task;
            stoppingCts.Cancel();
        }

        try
        {
            await stoppingTask;
        }
        catch (OperationCanceledException)
        {
        }

        finally
        {
            lock (lifecycleSync)
            {
                // Another StopAsync caller must not clear a subsequently restarted loop.
                if (ReferenceEquals(task, stoppingTask))
                {
                    stoppingCts.Dispose();
                    cts = null;
                    task = null;
                }
            }
        }
    }
}

/// <summary>RPG 事件循环：移动等事件后按配置间隔推进（默认 800ms）。</summary>
public class GameEventLoop(GameSystemConfig config) : GameLoopBase
{
    protected override int NextTurnIntervalMiliseconds => config.MovingNextTurnIntervalMilisecond;

    protected override bool CanNextTime() => true;
}

/// <summary>战略时间循环：约 1 秒推进 1 游戏日。</summary>
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
