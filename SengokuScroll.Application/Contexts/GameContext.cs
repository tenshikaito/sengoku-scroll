using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;

namespace SengokuScroll.Application.Contexts;

public class GameContext(
    IGameWorldContext gameDataContext,
    IGameWorldEventDispatcher gameEventContext,
    GameRuleConfig gameRuleConfig)
    : IGameContext
{
    public IGameWorldContext GameWorldContext { get; } = gameDataContext;

    public IGameWorldEventDispatcher GameEventContext { get; } = gameEventContext;

    public GameWorld GameWorld { get; } = gameDataContext.GameWorld;

    public GameMasterData GameMasterData => GameWorld.GameMasterData;

    public GameMapMasterData GameMapMasterData => GameWorld.GameMapMasterData;

    public GameRuleConfig GameRuleConfig { get; } = gameRuleConfig;
}
