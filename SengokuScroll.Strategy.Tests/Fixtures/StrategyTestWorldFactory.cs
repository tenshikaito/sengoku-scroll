using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Hosting;
using SengokuScroll.Strategy.Time;

namespace SengokuScroll.Strategy.Tests.Fixtures;

/// <summary>
/// 构建策略模式集成测试用的最小游戏世界与 DI 容器。
/// </summary>
public static class StrategyTestWorldFactory
{
    /// <summary>默认测试地图边长（格）。</summary>
    public const int DefaultMapSize = 10;

    /// <summary>创建带策略模式 DI 的测试环境。</summary>
    public static StrategyTestContext Create()
        => CreateFromWorld(StrategyTestWorldBuilder.BuildMinimalWorld());

    /// <summary>创建含据点与低粮单位的策略测试环境（M1-c）。</summary>
    public static StrategyTestContext CreateLogisticsWorld(
        Point3? unitLocation = null,
        int unitFood = 100)
        => CreateFromWorld(StrategyTestWorldBuilder.BuildLogisticsWorld(unitLocation, unitFood));

    /// <summary>创建含策略模式 DI 的测试环境；可传入 debug / AI trace 选项。</summary>
    public static StrategyTestContext CreateFromWorld(
        GameWorld world,
        StrategyScenarioMeta? scenarioMeta = null,
        StrategyDayDebugOptions? dayDebugOptions = null,
        StrategyAiTraceOptions? aiTraceOptions = null)
    {
        var meta = scenarioMeta ?? new StrategyScenarioMeta
        {
            PlayerForceId = 1,
            LordUnitId = 1,
            LordName = "测试当主"
        };
        var simulation = StrategySimulationBootstrap.CreateScope(world, meta, dayDebugOptions, aiTraceOptions);
        return new StrategyTestContext(
            simulation.World,
            simulation,
            simulation.Engine,
            new StrategyTimeController());
    }
}

/// <summary>策略集成测试上下文（世界 + 仿真作用域 + 引擎 + 时间控制器）。</summary>
public sealed class StrategyTestContext(
    GameWorld world,
    StrategySimulationScope simulation,
    IGameEngine engine,
    StrategyTimeController timeController) : IDisposable
{
    public GameWorld World { get; } = world;

    public IGameEngine Engine { get; } = engine;

    public StrategyTimeController TimeController { get; } = timeController;

    public IServiceProvider Services => simulation.Services;

    public void Dispose() => simulation.Dispose();
}
