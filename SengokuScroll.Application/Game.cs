using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SengokuScroll.Application.Constants;
using SengokuScroll.Application.Contexts;
using SengokuScroll.Application.EventHandlers;
using SengokuScroll.Application.Models;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Events;

namespace SengokuScroll.Application;

/// <summary>游戏运行时门面：推进循环、只读查询与玩家命令入口。</summary>
public interface IGame : IEngineLoop
{
    /// <summary>只读查询（如角色状态、地图信息）。</summary>
    Task<GameResult<T>> QueryAsync<T>(IQuery<T> query) where T : notnull;

    /// <summary>提交会改变世界状态的玩家命令（如移动、攻击）。</summary>
    Task<GameResult> SendCommandAsync<TCommand>(TCommand cmd) where TCommand : ICommand;
}

/// <summary>绑定导演、会话与 DI，按游戏模式启动对应引擎循环。</summary>
public class Game : IGame
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<Game> logger;
    private readonly IGameDirector gameDirector;
    private readonly IGameContext gameContext;
    private readonly GameSession gameSession;
    //private readonly GameOptions gameOptions;

    public bool IsRunning { get; private set; }

    public Game(GameOptions gameOptions)
    {
        var sp = gameOptions.ServiceProvider;

        logger = sp.GetRequiredService<ILogger<Game>>();
        // 业务：RPG 用事件循环（移动后推进），战略用固定日间隔循环
        gameDirector = sp.GetRequiredKeyedService<IGameDirector>(gameOptions.GameMode switch
        {
            GameMode.RolePlaying => ServiceConstants.RpgGameDirector,
            GameMode.GrandStrategy => ServiceConstants.StrategyGameDirector,
            _ => throw new NotSupportedException("Not Supported GameMode.")
        });

        gameContext = sp.GetRequiredService<IGameContext>();
        gameSession = sp.GetRequiredService<GameSession>();

        void InitEventHandler()
        {
            var eventDispatcher = sp.GetRequiredService<IGameEventDispatcher>();

            eventDispatcher.Register(sp.GetRequiredService<CharacterMoveEventHandler>());
        }

        InitEventHandler();

        serviceProvider = sp;
    }

    /// <summary>启动游戏循环；<paramref name="isPause"/> 为 true 时先暂停（待玩家准备）。</summary>
    public void Start(bool isPause = false)
    {
        if (IsRunning)
        {
            logger.LogInformation("Game is already running.");

            return;
        }

        IsRunning = true;

        logger.LogInformation("Game starting...");

        gameDirector!.Start(isPause);

        logger.LogInformation("Game started.");
    }

    /// <summary>暂停日/回合推进。</summary>
    public void Pause() => gameDirector.Pause();

    /// <summary>恢复日/回合推进。</summary>
    public void Resume() => gameDirector.Resume();

    /// <summary>停止循环并等待后台任务结束。</summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            logger.LogInformation("Game already stopped.");

            return;
        }

        logger.LogInformation("Stopping game...");

        await gameDirector!.StopAsync();

        IsRunning = false;

        logger.LogInformation("Game stopped.");
    }

    /// <inheritdoc cref="IGame.QueryAsync{T}(IQuery{T})"/>
    public async Task<GameResult<T>> QueryAsync<T>(IQuery<T> query) where T : notnull
    {
        CheckRunning();

        var rc = new GameRequestContext(gameSession, gameContext);

        var handler = (IRequestHandler<IQuery<T>, T>)serviceProvider.GetRequiredKeyedService<IRequestHandler>(query.GetType().Name);

        var r = handler.Handle(query, rc);

        return r;
    }

    /// <inheritdoc cref="IGame.SendCommandAsync{TCommand}(TCommand)"/>
    public async Task<GameResult> SendCommandAsync<TCommand>(TCommand cmd) where TCommand : ICommand
    {
        CheckRunning();

        var rc = new GameRequestContext(gameSession, gameContext);

        var handler = (IRequestHandler<TCommand>)serviceProvider.GetRequiredKeyedService<IRequestHandler>(cmd.GetType().Name);

        var r = handler.Handle(cmd, rc);

        return r;
    }

    /// <summary>运行时追加领域事件处理器。</summary>
    public void RegisterEventHandler<TEvent>(IGameEventHandler<TEvent> handler) where TEvent : IGameEvent
        => serviceProvider.GetRequiredService<IGameWorldEventDispatcher>().Register(handler);

    private void CheckRunning()
    {
        if (!IsRunning)
            throw new InvalidOperationException("Game is not running.");
    }
}
