using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Events;

namespace SengokuScroll.Application.EventHandlers;

public class CharacterMoveEventHandler : IGameEventHandler<CharacterMovedEvent>
{
    public void Handle(CharacterMovedEvent e)
    {
        // 占位：后续可在此同步会话状态、触发 UI 通知等；勿再次 Publish 同一事件。
    }
}
