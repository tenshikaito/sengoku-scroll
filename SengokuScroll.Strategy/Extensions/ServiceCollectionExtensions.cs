using Microsoft.Extensions.DependencyInjection;
using SengokuScroll.Application.Constants;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Systems;

namespace SengokuScroll.Strategy.Extensions;

/// <summary>策略模式依赖注入扩展。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>注册策略仿真宿主（WebApi / 单机 M1-e）。</summary>
    public static IServiceCollection AddStrategySimulationHost(this IServiceCollection services)
    {
        services.AddSingleton<StrategySimulationHost>();
        return services;
    }

    /// <summary>
    /// 注册策略模式专属系统，并替换 <see cref="ServiceConstants.StrategyGameEngine"/> 的系统链。
    /// </summary>
    public static IServiceCollection AddStrategyMode(this IServiceCollection services)
    {
        services.AddSingleton<StrategyTimeSystem>();
        services.AddSingleton<IStrategyTimeSystem>(sp => sp.GetRequiredService<StrategyTimeSystem>());
        services.AddSingleton<StrategyUnitSystem>();
        services.AddSingleton<IStrategyUnitSystem>(sp => sp.GetRequiredService<StrategyUnitSystem>());
        services.AddSingleton<IUnitSystem>(sp => sp.GetRequiredService<StrategyUnitSystem>());

        services.AddSingleton<SupplyConvoyDispatchHelper>();
        services.AddSingleton<MessengerDispatchHelper>();

        services.AddSingleton<StrategyEconomySystem>();
        services.AddSingleton<IStrategyEconomySystem>(sp => sp.GetRequiredService<StrategyEconomySystem>());
        services.AddSingleton<IEconomySystem>(sp => sp.GetRequiredService<StrategyEconomySystem>());

        services.AddSingleton<IStrategySupplySystem, StrategySupplySystem>();
        services.AddSingleton<IStrategyMessengerSystem, StrategyMessengerSystem>();

        services.AddSingleton<StrategyInstantBattleSystem>();

        services.AddSingleton<StrategyBattleResolutionSystem>();
        services.AddSingleton<IStrategyBattleResolutionSystem>(
            sp => sp.GetRequiredService<StrategyBattleResolutionSystem>());

        services.AddSingleton<StrategyAISystem>();
        services.AddSingleton<IStrategyAISystem>(sp => sp.GetRequiredService<StrategyAISystem>());
        services.AddSingleton<IAISystem>(sp => sp.GetRequiredService<StrategyAISystem>());

        services.AddKeyedSingleton<IGameEngine>(ServiceConstants.StrategyGameEngine, (sp, _) =>
            new ConfigurableGameEngine(
            [
                sp.GetRequiredService<IStrategyTimeSystem>(),
                sp.GetRequiredService<IClimateSystem>(),
                sp.GetRequiredService<IEconomySystem>(),
                sp.GetRequiredService<IStrategySupplySystem>(),
                sp.GetRequiredService<IUnitSystem>(),
                sp.GetRequiredService<IStrategyBattleResolutionSystem>(),
                sp.GetRequiredService<IStrategyMessengerSystem>(),
                sp.GetRequiredService<ICharacterSystem>(),
                sp.GetRequiredService<IAISystem>()
            ]));

        return services;
    }
}
