using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities.Abstraction;

namespace SengokuScroll.Domain.Contexts;

public interface IGameContext
{
    IGameWorldContext GameWorldContext { get; }

    IGameWorldEventDispatcher GameEventContext { get; }

    GameRuleConfig GameRuleConfig { get; }
}
