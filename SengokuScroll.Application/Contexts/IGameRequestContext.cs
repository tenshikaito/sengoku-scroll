using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Application.Contexts;

public interface IGameRequestContext
{
    IGameContext GameContext { get; }

    GameSession GameSession { get; }
}
