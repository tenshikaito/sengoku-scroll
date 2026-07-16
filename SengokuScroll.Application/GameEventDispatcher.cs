using System.Collections.Concurrent;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Events;

namespace SengokuScroll.Application;

/// <summary>应用层游戏事件分发器（与领域 <see cref="IGameWorldEventDispatcher"/> 同实例）。</summary>
public interface IGameEventDispatcher : IGameWorldEventDispatcher
{
}

/// <summary>按事件类型注册处理器并同步发布领域事件（如角色移动、战斗结算后通知）。</summary>
public class GameEventDispatcher : IGameEventDispatcher
{
    private readonly ConcurrentDictionary<Type, List<IGameEventHandler>> handlers = new();

    private readonly object registerLock = new();

    /// <summary>注册某类领域事件的处理器。</summary>
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

    /// <summary>向已注册处理器发布事件；无订阅者则忽略。</summary>
    public void Publish<TEvent>(TEvent e)
        where TEvent : IGameEvent
    {
        var type = typeof(TEvent);

        if (!handlers.TryGetValue(type, out var list))
            return;

        IGameEventHandler[] snapshot;
        lock (registerLock)
            snapshot = [.. list];

        foreach (var handler in snapshot)
            ((IGameEventHandler<TEvent>)handler).Handle(e);
    }
}
