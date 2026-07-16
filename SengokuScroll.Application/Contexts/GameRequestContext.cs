using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Application.Contexts;

/// <summary>单次查询/命令请求的会话与世界上下文。</summary>
public class GameRequestContext(GameSession gameSession, IGameContext gameContext) : IGameRequestContext
{
    public GameSession GameSession { get; } = gameSession;

    public IGameContext GameContext { get; } = gameContext;
}
