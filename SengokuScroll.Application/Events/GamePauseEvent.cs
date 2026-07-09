namespace SengokuScroll.Application.Events;

public class GamePauseEvent : IAwaitableGameEvent
{
    private readonly TaskCompletionSource tcs = new();

    public Task Completion => tcs.Task;

    public void Complete() => tcs.SetResult();
}