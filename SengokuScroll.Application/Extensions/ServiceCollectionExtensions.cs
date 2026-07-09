using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Application.CommandHandlers;
using SengokuScroll.Application.Commands;
using SengokuScroll.Application.Constants;
using SengokuScroll.Application.Contexts;
using SengokuScroll.Application.EventHandlers;
using SengokuScroll.Application.Models;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Behaviors.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Diagnostics;
using SengokuScroll.Domain.Evaluators;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain.Systems;

namespace SengokuScroll.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGameServices(this IServiceCollection services, GameWorldContext gameWorldContext, GameSession gameSession)
    {
        services.AddGameDomain();
        services.AddGameApplication(
            gameSession,
            gameWorldContext);

        return services;
    }

    public static IServiceCollection AddGameApplication(
        this IServiceCollection services,
        GameSession gameSession,
        GameWorldContext gameWorldContext)
    {
        services.AddKeyedSingleton<IGameLoop, GameTimeLoop>(ServiceConstants.GameTimeLoop);
        services.AddKeyedSingleton<IGameLoop, GameEventLoop>(ServiceConstants.GameEventLoop);
        services.AddKeyedSingleton<IGameDirector, RpgGameDirector>(ServiceConstants.RpgGameDirector);
        services.AddKeyedSingleton<IGameDirector, StrategyGameDirector>(ServiceConstants.StrategyGameDirector);
        services.AddSingleton<IGameContext, GameContext>();
        services.AddSingleton<IGameWorldContext>(gameWorldContext);
        services.AddSingleton(gameSession);
        services.AddSingleton(new GameSystemConfig());
        services.AddSingleton(new GameRuleConfig());

        services.AddEventHandlers();
        services.AddQueryHandlers();
        services.AddCommandHandlers();

        return services;
    }

    private static IServiceCollection AddEventHandlers(this IServiceCollection services)
    {
        services.AddSingleton<CharacterMoveEventHandler>();

        return services;
    }

    private static IServiceCollection AddQueryHandlers(this IServiceCollection services)
    {
        return services;
    }

    private static IServiceCollection AddCommandHandlers(this IServiceCollection services)
    {
        services.AddKeyedScoped<IRequestHandler, CharacterMoveCommandHandler>(nameof(CharacterMoveCommand));

        return services;
    }

    public static IServiceCollection AddGameDomain(this IServiceCollection services)
    {
        services.AddSingleton<GameEventDispatcher>();
        services.AddSingleton<IGameEventDispatcher>(sp => sp.GetRequiredService<GameEventDispatcher>());
        services.AddSingleton<IGameWorldEventDispatcher>(sp => sp.GetRequiredService<GameEventDispatcher>());
        services.AddKeyedSingleton<IGameEngine, RpgGameEngine>(ServiceConstants.RpgGameEngine);
        services.AddGameRules();
        services.AddGameEvaluators();
        services.AddGameDomainServices();
        services.AddGameSystems();
        services.AddGameActions();

        return services;
    }

    public static IServiceCollection AddGameDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IPathfindingService, PathfindingService>();

        return services;
    }

    public static IServiceCollection AddGameRules(this IServiceCollection services)
    {
        services.AddSingleton<CommonRules, CommonRules>();
        services.AddSingleton<DiplomacyRules, DiplomacyRules>();
        services.AddSingleton<MovementRules, MovementRules>();
        services.AddSingleton<UnitRules, UnitRules>();

        return services;
    }

    public static IServiceCollection AddGameEvaluators(this IServiceCollection services)
    {
        services.AddSingleton<UnitMoveEvaluator, UnitMoveEvaluator>();
        services.AddSingleton<CharacterMoveEvaluator, CharacterMoveEvaluator>();
        services.AddSingleton<UnitAttackEvaluator, UnitAttackEvaluator>();

        return services;
    }

    public static IServiceCollection AddGameSystems(this IServiceCollection services)
    {
        services.AddSingleton<IClimateSystem, ClimateSystem>();
        services.AddSingleton<IEconomySystem, EconomySystem>();
        services.AddSingleton<IUnitSystem, UnitSystem>();
        services.AddSingleton<ICharacterSystem, CharacterSystem>();
        services.AddSingleton<IAISystem, AISystem>();

        return services;
    }

    public static IServiceCollection AddGameActions(this IServiceCollection services)
    {
        services.AddSingleton<IUnitMoveObserver, NullUnitMoveObserver>();
        services.AddSingleton<UnitMoveAction>();
        services.AddSingleton<CharacterMoveAction>();

        return services;
    }
}
