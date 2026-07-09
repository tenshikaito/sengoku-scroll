using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Application.Contexts;

public class GameRequestContext(GameSession gameSession, IGameContext gameContext) : IGameRequestContext
{
    public GameSession GameSession { get; } = gameSession;

    public IGameContext GameContext { get; } = gameContext;
}
