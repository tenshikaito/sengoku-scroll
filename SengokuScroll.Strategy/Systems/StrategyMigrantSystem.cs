using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Systems;

/// <summary>移民队系统接口。</summary>
public interface IStrategyMigrantSystem : IGameSystem
{
}

/// <summary>移民队：低民心/治安据点向更好据点迁出人口。</summary>
public class StrategyMigrantSystem(
    IGameContext context,
    MigrantDispatchHelper migrantDispatchHelper) : IStrategyMigrantSystem
{
    public int Order { get; } = 14;

    public void Update()
        => migrantDispatchHelper.EvaluateAndDispatchDailyMigrations();
}
