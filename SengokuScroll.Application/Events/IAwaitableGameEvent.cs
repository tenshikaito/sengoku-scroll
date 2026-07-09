namespace SengokuScroll.Application.Events;

public interface IAwaitableGameEvent
{
    Task Completion { get; }
}