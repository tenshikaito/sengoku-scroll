using SengokuScroll.Domain.Events;

namespace SengokuScroll.Domain.Contexts;

public interface IGameWorldEventDispatcher
{
    void Register<TEvent>(IGameEventHandler<TEvent> handler) where TEvent : IGameEvent;

    void Publish<TEvent>(TEvent e) where TEvent : IGameEvent;
}
