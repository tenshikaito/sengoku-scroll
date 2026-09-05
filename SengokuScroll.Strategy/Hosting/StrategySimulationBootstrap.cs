using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SengokuScroll.Application;
using SengokuScroll.Application.Constants;
using SengokuScroll.Application.Contexts;
using SengokuScroll.Application.Extensions;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Diagnostics;
using SengokuScroll.Localization;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Extensions;
using SengokuScroll.Strategy.Vision;

namespace SengokuScroll.Strategy.Hosting;

/// <summary>策略仿真 DI 引导：与集成测试/WebApi 共用同一套注册顺序。</summary>
public static class StrategySimulationBootstrap
{
    /// <summary>为指定 <see cref="GameWorld"/> 构建策略模式仿真作用域。</summary>
    public static StrategySimulationScope CreateScope(
        GameWorld world,
        StrategyScenarioMeta scenarioMeta,
        StrategyDayDebugOptions? dayDebugOptions = null,
        StrategyAiTraceOptions? aiTraceOptions = null,
        ILoggerFactory? loggerFactory = null)
    {
        var services = new ServiceCollection();
        var worldContext = new GameWorldContext(world);

        // Each world must not create its own console logger/queue. The application
        // owns logging configuration; standalone simulation is silent by default.
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(loggerFactory
            ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        services.AddSengokuLocalization();
        services.AddSingleton(Options.Create(dayDebugOptions ?? new StrategyDayDebugOptions
        {
            Enabled = false,
            WriteToFile = false
        }));
        services.AddSingleton(Options.Create(aiTraceOptions ?? new StrategyAiTraceOptions()));
        services.AddSingleton<IStrategyDayDebugLog, StrategyDayDebugLog>();
        services.AddGameDomain();
        services.AddStrategyMode();
        services.AddSingleton(worldContext);
        services.AddSingleton<IGameWorldContext>(worldContext);
        services.AddSingleton(new GameRuleConfig());
        services.AddSingleton(new GameSystemConfig());
        services.AddSingleton<IGameContext, GameContext>();
        services.AddSingleton<StrategyMovementTrace>();
        services.AddSingleton<StrategyAiDecisionTrace>();
        services.AddSingleton<StrategyDayOutcomeBuffer>();
        services.AddSingleton(scenarioMeta);
        services.AddSingleton<StrategyUnitMoveTraceObserver>();
        services.AddSingleton<IUnitMoveObserver>(sp => sp.GetRequiredService<StrategyUnitMoveTraceObserver>());

        var root = services.BuildServiceProvider();
        var scope = root.CreateScope();
        try
        {
            var sp = scope.ServiceProvider;
            sp.GetRequiredService<StrategyForceLordRegistry>().Initialize(scenarioMeta);
            sp.GetRequiredService<StrategyVisibilityLedger>().Initialize(world, scenarioMeta);

            return new StrategySimulationScope(
                root,
                scope,
                world,
                scenarioMeta,
                sp.GetRequiredKeyedService<IGameEngine>(ServiceConstants.StrategyGameEngine),
                sp.GetRequiredService<IGameContext>(),
                sp.GetRequiredService<StrategyMovementTrace>());
        }
        catch
        {
            scope.Dispose();
            root.Dispose();
            throw;
        }
    }
}

/// <summary>一次策略仿真会话的 DI 作用域与引擎引用。</summary>
public sealed class StrategySimulationScope(
    ServiceProvider root,
    IServiceScope scope,
    GameWorld world,
    StrategyScenarioMeta scenarioMeta,
    IGameEngine engine,
    IGameContext gameContext,
    StrategyMovementTrace movementTrace) : IDisposable
{
    public GameWorld World { get; } = world;

    public StrategyScenarioMeta ScenarioMeta { get; } = scenarioMeta;

    public IGameEngine Engine { get; } = engine;

    public IGameContext GameContext { get; } = gameContext;

    public StrategyMovementTrace MovementTrace { get; } = movementTrace;

    public IServiceProvider Services => scope.ServiceProvider;

    public void Dispose()
    {
        scope.Dispose();
        root.Dispose();
    }
}
