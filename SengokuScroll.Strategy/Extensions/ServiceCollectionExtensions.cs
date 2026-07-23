using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SengokuScroll.Application.Constants;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Localization;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Systems;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Extensions;

/// <summary>策略模式依赖注入扩展。</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>注册策略仿真宿主（WebApi / 单机 M1-e）。</summary>
    public static IServiceCollection AddStrategySimulationHost(this IServiceCollection services)
    {
        services.AddSengokuLocalization();
        services.TryAddSingleton<IOptions<StrategyDayDebugOptions>>(
            _ => Options.Create(new StrategyDayDebugOptions()));
        services.TryAddSingleton<IOptions<StrategyAiTraceOptions>>(
            _ => Options.Create(new StrategyAiTraceOptions()));
        services.TryAddSingleton<IStrategyDayDebugLog, StrategyDayDebugLog>();
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
        services.AddSingleton<MigrantDispatchHelper>();
        services.AddSingleton<MessageCarrierDispatchHelper>();
        services.AddSingleton<StrategyPendingBattleReportStore>();
        services.AddSingleton<StrategyPendingEventStore>();
        services.AddSingleton<BattleReportDeliveryHelper>();
        services.AddSingleton<StrategyFieldEngagementRegistry>();
        services.AddSingleton<StrategyWarOccupationRegistry>();
        services.AddSingleton<StrategyForceLordRegistry>();
        services.AddSingleton<StrongholdCaptureHelper>();
        services.AddSingleton<BattleAftermathHelper>();

        services.AddSingleton<StrategyTributeLedger>();
        services.AddSingleton<StrategyIntelligenceLedger>();
        services.AddSingleton<MerchantTaxLedger>();
        services.AddSingleton<TariffTaxLedger>();
        services.AddSingleton<MonthlyTaxCollectionLedger>();

        services.AddSingleton<StrategyMarketSystem>();
        services.AddSingleton<IStrategyMarketSystem>(sp => sp.GetRequiredService<StrategyMarketSystem>());

        services.AddSingleton<StrategyEconomySystem>();
        services.AddSingleton<IStrategyEconomySystem>(sp => sp.GetRequiredService<StrategyEconomySystem>());
        services.AddSingleton<IEconomySystem>(sp => sp.GetRequiredService<StrategyEconomySystem>());

        services.AddSingleton<IStrategySupplySystem, StrategySupplySystem>();
        services.AddSingleton<StrategyMigrantSystem>();
        services.AddSingleton<IStrategyMigrantSystem>(sp => sp.GetRequiredService<StrategyMigrantSystem>());
        services.AddSingleton<IStrategyMessageCarrierSystem, StrategyMessageCarrierSystem>();

        services.AddSingleton<StrategyVisibilityLedger>();
        services.AddSingleton<StrategyEspionageIntelLedger>();
        services.AddSingleton<StrategyMessageLedger>();
        services.AddSingleton<StrategyVisionSystem>();
        services.AddSingleton<IStrategyVisionSystem>(sp => sp.GetRequiredService<StrategyVisionSystem>());

        services.AddSingleton<StrategyInstantBattleSystem>();

        services.AddSingleton<StrategyBattleResolutionSystem>();
        services.AddSingleton<IStrategyBattleResolutionSystem>(
            sp => sp.GetRequiredService<StrategyBattleResolutionSystem>());

        services.AddSingleton<StrategyMoveEngagementSystem>();
        services.AddSingleton<IStrategyMoveEngagementSystem>(
            sp => sp.GetRequiredService<StrategyMoveEngagementSystem>());

        services.AddSingleton<StrategySiegeSystem>();
        services.AddSingleton<IStrategySiegeSystem>(sp => sp.GetRequiredService<StrategySiegeSystem>());

        services.AddSingleton<StrategyStrongholdOccupationSystem>();
        services.AddSingleton<IStrategyStrongholdOccupationSystem>(
            sp => sp.GetRequiredService<StrategyStrongholdOccupationSystem>());

        services.AddSingleton<StrategyRecruitTaskSystem>();
        services.AddSingleton<IStrategyRecruitTaskSystem>(sp => sp.GetRequiredService<StrategyRecruitTaskSystem>());

        services.AddSingleton<StrategyStrongholdGovernanceSystem>();
        services.AddSingleton<IStrategyStrongholdGovernanceSystem>(
            sp => sp.GetRequiredService<StrategyStrongholdGovernanceSystem>());

        services.AddSingleton<StrategyCharacterAISystem>();
        services.AddSingleton<IStrategyCharacterAISystem>(sp => sp.GetRequiredService<StrategyCharacterAISystem>());

        services.AddSingleton<StrategyCharacterStaminaSystem>();
        services.AddSingleton<IStrategyCharacterStaminaSystem>(sp => sp.GetRequiredService<StrategyCharacterStaminaSystem>());

        services.AddSingleton<StrategyAISystem>();
        services.AddSingleton<IStrategyAISystem>(sp => sp.GetRequiredService<StrategyAISystem>());
        services.AddSingleton<IAISystem>(sp => sp.GetRequiredService<StrategyAISystem>());

        services.AddKeyedSingleton<IGameEngine>(ServiceConstants.StrategyGameEngine, (sp, _) =>
        {
            var systems = BuildStrategySystems(sp);
            var debugLog = sp.GetRequiredService<IStrategyDayDebugLog>();
            return debugLog.IsEnabled
                ? new StrategyDebugGameEngine(systems, debugLog)
                : new StrategyGameEngineCore(systems);
        });

        return services;
    }

    private static IEnumerable<IGameSystem> BuildStrategySystems(IServiceProvider sp)
        =>
        [
            sp.GetRequiredService<IStrategyTimeSystem>(),
            sp.GetRequiredService<IClimateSystem>(),
            sp.GetRequiredService<IStrategyMarketSystem>(),
            sp.GetRequiredService<IEconomySystem>(),
            sp.GetRequiredService<IStrategyMigrantSystem>(),
            sp.GetRequiredService<IStrategySupplySystem>(),
            sp.GetRequiredService<IStrategyStrongholdGovernanceSystem>(),
            sp.GetRequiredService<IStrategyRecruitTaskSystem>(),
            sp.GetRequiredService<IStrategyCharacterAISystem>(),
            sp.GetRequiredService<IAISystem>(),
            sp.GetRequiredService<IUnitSystem>(),
            sp.GetRequiredService<IStrategyVisionSystem>(),
            sp.GetRequiredService<IStrategySiegeSystem>(),
            sp.GetRequiredService<IStrategyMoveEngagementSystem>(),
            sp.GetRequiredService<IStrategyStrongholdOccupationSystem>(),
            sp.GetRequiredService<IStrategyMessageCarrierSystem>(),
            sp.GetRequiredService<IStrategyBattleResolutionSystem>(),
            sp.GetRequiredService<ICharacterSystem>(),
            sp.GetRequiredService<IStrategyCharacterStaminaSystem>(),
        ];
}
