using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Application;
using SengokuScroll.Application.Extensions;

namespace SengokuScroll.Host;

public class GameBuilder
{
    private readonly IServiceCollection services = new ServiceCollection();

    public GameBuilder Configure(GameOptions gameOptions, Action<IServiceCollection> configure)
    {
        services.AddSingleton(gameOptions);

        services.AddGameDomain();
        services.AddGameApplication();

        configure(services);

        return this;
    }

    public Game Build()
    {
        var sp = services.BuildServiceProvider();

        return sp.GetRequiredService<Game>();
    }
}