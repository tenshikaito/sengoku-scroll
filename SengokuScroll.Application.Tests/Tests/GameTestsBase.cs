using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SengokuScroll.Application.Contexts;
using SengokuScroll.Application.Extensions;
using SengokuScroll.Application.Tests.Data;
using SengokuScroll.Application.Tests.Log;
using SengokuScroll.Domain.Entities;

namespace SengokuScroll.Application.Tests.Tests;

public abstract class GameTestsBase(ITestOutputHelper output)
{
    protected readonly ITestOutputHelper output = output;

    protected Task Build(Func<Game, Character, Task> run, Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();

        var gameDataProcessor = new ExampleGameDataProcessor();
        var gameWorld = gameDataProcessor.Load("test_world");
        var gamePlayer = new GamePlayer(Guid.NewGuid(), "test");
        var playerCharacter = gameWorld.GameData.Characters.GetValueOrDefault(1)!;
        var gameSession = new GameSession(gamePlayer, playerCharacter);

        var gameWorldContext = new GameWorldContext(gameWorld);

        services.AddGameServices(gameWorldContext, gameSession);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new TestOutputLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        configure?.Invoke(services);

        using var scope = services.BuildServiceProvider().CreateScope();

        var sp = scope.ServiceProvider;

        var gameOptions = new GameOptions()
        {
            GamePlayer = gamePlayer,
            GameWorld = gameWorld,
            GameMode = GameMode.RolePlaying,
            PlayerSelectedId = 1,
            ServiceProvider = sp,
        };

        return run(new Game(gameOptions), playerCharacter);
    }

    protected class TestGameLoop() : IGameLoop
    {
        public Action? BeforeNextTime { get; set; }
        public Action? NextTime { get; set; }
        public Action? AfterNextTime { get; set; }

        public void Pause()
        {
        }

        public void Resume()
        {
            NextTime?.Invoke();
        }

        public void Start(bool isPause = false)
        {
        }

        public Task StopAsync() => Task.CompletedTask;
    }
}