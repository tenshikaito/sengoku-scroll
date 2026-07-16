using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Application.Contexts;

/// <summary>命令/查询处理器可见的上下文。</summary>
public interface IGameRequestContext
{
    IGameContext GameContext { get; }

    GameSession GameSession { get; }
}
