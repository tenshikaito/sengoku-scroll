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

/// <summary>应用层 DI 扩展：注册导演、循环、领域规则/系统与命令处理器。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>一次性注册领域与应用服务（世界上下文 + 玩家会话）。</summary>
    public static IServiceCollection AddGameServices(this IServiceCollection services, GameWorldContext gameWorldContext, GameSession gameSession)
    {
        services.AddGameDomain();
        services.AddGameApplication(
            gameSession,
            gameWorldContext);

        return services;
    }

    /// <summary>注册应用层：双模式导演/循环、上下文、命令与事件处理器。</summary>
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

    /// <summary>注册领域层：事件总线、双模式引擎、规则、评估器、系统与行动。</summary>
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

    /// <summary>注册寻路等领域服务。</summary>
    public static IServiceCollection AddGameDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IPathfindingService, PathfindingService>();

        return services;
    }

    /// <summary>注册通用/外交/移动/单位游戏规则。</summary>
    public static IServiceCollection AddGameRules(this IServiceCollection services)
    {
        services.AddSingleton<CommonRules, CommonRules>();
        services.AddSingleton<DiplomacyRules, DiplomacyRules>();
        services.AddSingleton<MovementRules, MovementRules>();
        services.AddSingleton<UnitRules, UnitRules>();

        return services;
    }

    /// <summary>注册移动/攻击等行动合法性评估器。</summary>
    public static IServiceCollection AddGameEvaluators(this IServiceCollection services)
    {
        services.AddSingleton<UnitMoveEvaluator, UnitMoveEvaluator>();
        services.AddSingleton<CharacterMoveEvaluator, CharacterMoveEvaluator>();
        services.AddSingleton<UnitAttackEvaluator, UnitAttackEvaluator>();

        return services;
    }

    /// <summary>注册气候、经济、单位、角色、AI 等日推进系统。</summary>
    public static IServiceCollection AddGameSystems(this IServiceCollection services)
    {
        services.AddSingleton<IClimateSystem, ClimateSystem>();
        services.AddSingleton<IEconomySystem, EconomySystem>();
        services.AddSingleton<IUnitSystem, UnitSystem>();
        services.AddSingleton<ICharacterSystem, CharacterSystem>();
        services.AddSingleton<IAISystem, AISystem>();

        return services;
    }

    /// <summary>注册单位/角色移动行动与观察者。</summary>
    public static IServiceCollection AddGameActions(this IServiceCollection services)
    {
        services.AddSingleton<IUnitMoveObserver, NullUnitMoveObserver>();
        services.AddSingleton<UnitMoveAction>();
        services.AddSingleton<CharacterMoveAction>();

        return services;
    }
}
