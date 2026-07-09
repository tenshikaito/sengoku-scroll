using System.Collections.Concurrent;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Events;

namespace SengokuScroll.Application;

public interface IGameEventDispatcher : IGameWorldEventDispatcher
{
}

public class GameEventDispatcher : IGameEventDispatcher
{
    private readonly ConcurrentDictionary<Type, List<IGameEventHandler>> handlers = new();

    private readonly object registerLock = new();

    public void Register<TEvent>(IGameEventHandler<TEvent> handler)
        where TEvent : IGameEvent
    {
        var type = typeof(TEvent);

        lock (registerLock)
        {
            var list = handlers.GetOrAdd(type, _ => []);
            list.Add(handler);
        }
    }

    public void Publish<TEvent>(TEvent e)
        where TEvent : IGameEvent
    {
        var type = typeof(TEvent);

        if (!handlers.TryGetValue(type, out var list))
            return;

        IGameEventHandler[] snapshot;
        lock (registerLock)
            snapshot = list.ToArray();

        foreach (var handler in snapshot)
            ((IGameEventHandler<TEvent>)handler).Handle(e);
    }
}
