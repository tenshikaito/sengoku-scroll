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

public interface IGame : IEngineLoop
{
    Task<GameResult<T>> QueryAsync<T>(IQuery<T> query) where T : notnull;

    Task<GameResult> SendCommandAsync<TCommand>(TCommand cmd) where TCommand : ICommand;
}

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

    public void Pause() => gameDirector.Pause();

    public void Resume() => gameDirector.Resume();

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

    public async Task<GameResult<T>> QueryAsync<T>(IQuery<T> query) where T : notnull
    {
        CheckRunning();

        var rc = new GameRequestContext(gameSession, gameContext);

        var handler = (IRequestHandler<IQuery<T>, T>)serviceProvider.GetRequiredKeyedService<IRequestHandler>(query.GetType().Name);

        var r = handler.Handle(query, rc);

        return r;
    }

    public async Task<GameResult> SendCommandAsync<TCommand>(TCommand cmd) where TCommand : ICommand
    {
        CheckRunning();

        var rc = new GameRequestContext(gameSession, gameContext);

        var handler = (IRequestHandler<TCommand>)serviceProvider.GetRequiredKeyedService<IRequestHandler>(cmd.GetType().Name);

        var r = handler.Handle(cmd, rc);

        return r;
    }

    public void RegisterEventHandler<TEvent>(IGameEventHandler<TEvent> handler) where TEvent : IGameEvent
        => serviceProvider.GetRequiredService<IGameWorldEventDispatcher>().Register(handler);

    private void CheckRunning()
    {
        if (!IsRunning)
            throw new InvalidOperationException("Game is not running.");
    }
}
