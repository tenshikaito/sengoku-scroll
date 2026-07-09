using SengokuScroll.Domain.Events;

namespace SengokuScroll.Domain.Contexts;

public interface IGameEventHandler
{

}

public interface IGameEventHandler<TEvent> : IGameEventHandler where TEvent : IGameEvent
{
    void Handle(TEvent e);
}
