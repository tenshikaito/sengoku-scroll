using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Application.Constants;
using SengokuScroll.Application.Models;
using SengokuScroll.Domain;

namespace SengokuScroll.Application;

public interface IGameDirector : IEngineLoop
{
}

public abstract class GameDirectorBase : IGameDirector
{
    private readonly IGameLoop gameLoop;

    public GameDirectorBase(
        IGameLoop gameLoop,
        IGameEngine gameEngine)
    {
        this.gameLoop = gameLoop;

        gameLoop.NextTime = gameEngine.NextTime;
    }

    public void Start(bool isPause = false) => gameLoop.Start(isPause);

    public virtual void Pause() => gameLoop.Pause();

    public virtual void Resume() => gameLoop.Resume();

    public Task StopAsync() => gameLoop.StopAsync();
}

public class RpgGameDirector(
    [FromKeyedServices(ServiceConstants.GameEventLoop)] IGameLoop gameLoop,
    [FromKeyedServices(ServiceConstants.RpgGameEngine)] IGameEngine gameEngine)
    : GameDirectorBase(gameLoop, gameEngine)
{
}

public class StrategyGameDirector(
    [FromKeyedServices(ServiceConstants.GameTimeLoop)] IGameLoop gameLoop,
    [FromKeyedServices(ServiceConstants.StrategyGameEngine)] IGameEngine gameEngine)
    : GameDirectorBase(gameLoop, gameEngine)
{
}