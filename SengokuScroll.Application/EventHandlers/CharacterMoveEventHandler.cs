using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Events;

namespace SengokuScroll.Application.EventHandlers;

/// <summary>角色移动领域事件：占位，供会话/UI 层订阅（勿在此再次 Publish）。</summary>
public class CharacterMoveEventHandler : IGameEventHandler<CharacterMovedEvent>
{
    /// <summary>角色移动后回调；当前无世界状态变更。</summary>
    public void Handle(CharacterMovedEvent e)
    {
        // 占位：后续可在此同步会话状态、触发 UI 通知等；勿再次 Publish 同一事件。
    }
}
