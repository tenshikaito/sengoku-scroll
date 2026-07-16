using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Application.Constants;
using SengokuScroll.Application.Models;
using SengokuScroll.Domain;

namespace SengokuScroll.Application;

/// <summary>游戏导演：将引擎 <see cref="IGameEngine.NextTime"/> 挂到循环并控制启停。</summary>
public interface IGameDirector : IEngineLoop
{
}

/// <summary>导演基类：共享循环启停，子类按模式注入不同循环与引擎。</summary>
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

    /// <inheritdoc />
    public void Start(bool isPause = false) => gameLoop.Start(isPause);

    /// <inheritdoc />
    public virtual void Pause() => gameLoop.Pause();

    /// <inheritdoc />
    public virtual void Resume() => gameLoop.Resume();

    /// <inheritdoc />
    public Task StopAsync() => gameLoop.StopAsync();
}

/// <summary>RPG 模式导演：事件驱动循环，移动等行为触发 <see cref="IGameEngine.NextTime"/>。</summary>
public class RpgGameDirector(
    [FromKeyedServices(ServiceConstants.GameEventLoop)] IGameLoop gameLoop,
    [FromKeyedServices(ServiceConstants.RpgGameEngine)] IGameEngine gameEngine)
    : GameDirectorBase(gameLoop, gameEngine)
{
}

/// <summary>战略模式导演：固定时间间隔日推进，执行经济/移动/战斗等系统链。</summary>
public class StrategyGameDirector(
    [FromKeyedServices(ServiceConstants.GameTimeLoop)] IGameLoop gameLoop,
    [FromKeyedServices(ServiceConstants.StrategyGameEngine)] IGameEngine gameEngine)
    : GameDirectorBase(gameLoop, gameEngine)
{
}