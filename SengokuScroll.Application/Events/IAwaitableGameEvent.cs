namespace SengokuScroll.Application.Events;

/// <summary>可等待完成的游戏事件（如暂停直到 UI 确认）。</summary>
public interface IAwaitableGameEvent
{
    Task Completion { get; }
}