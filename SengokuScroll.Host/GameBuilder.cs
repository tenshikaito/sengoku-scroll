using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Application;
using SengokuScroll.Application.Contexts;
using SengokuScroll.Application.Extensions;

namespace SengokuScroll.Host;

public class GameBuilder
{
    private readonly IServiceCollection services = new ServiceCollection();
    private GameOptions? gameOptions;

    public GameBuilder Configure(GameOptions gameOptions, Action<IServiceCollection> configure)
    {
        this.gameOptions = gameOptions;
        services.AddSingleton(gameOptions);

        var playerCharacter = gameOptions.GameWorld.GameData.Characters
            .GetValueOrDefault(gameOptions.PlayerSelectedId)
            ?? throw new ArgumentException(
                $"Player character {gameOptions.PlayerSelectedId} does not exist in the game world.",
                nameof(gameOptions));

        services.AddGameServices(
            new GameWorldContext(gameOptions.GameWorld),
            new GameSession(gameOptions.GamePlayer, playerCharacter));

        configure(services);

        return this;
    }

    public Game Build()
    {
        if (gameOptions is null)
            throw new InvalidOperationException("Configure must be called before Build.");

        var sp = services.BuildServiceProvider();
        gameOptions.ServiceProvider = sp;

        return new Game(gameOptions);
    }
}
