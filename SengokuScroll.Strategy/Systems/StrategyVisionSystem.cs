using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Data.Models;

namespace SengokuScroll.Strategy.Vision;

/// <summary>日推进后重算玩家势力 visible / explored。</summary>
public interface IStrategyVisionSystem : IGameSystem
{
}

public sealed class StrategyVisionSystem(
    IGameContext context,
    StrategyVisibilityLedger visibilityLedger,
    StrategyEspionageIntelLedger espionageLedger,
    StrategyScenarioMeta scenarioMeta) : IStrategyVisionSystem
{
    public int Order { get; } = 25;

    public void Update()
    {
        // 业务：日初剔除过期谍报，遮蔽恢复「未知」
        espionageLedger.PruneExpired(context.GameWorldContext.GameWorld.GameData.GameDate);
        visibilityLedger.Recompute(context.GameWorldContext.GameWorld, scenarioMeta);
    }
}
