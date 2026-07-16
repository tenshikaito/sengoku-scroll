namespace SengokuScroll.Application.Events;

/// <summary>游戏暂停信号：UI 确认后调用 <see cref="Complete"/> 继续循环。</summary>
public class GamePauseEvent : IAwaitableGameEvent
{
    private readonly TaskCompletionSource tcs = new();

    public Task Completion => tcs.Task;

    /// <summary>通知等待方暂停已解除。</summary>
    public void Complete() => tcs.SetResult();
}