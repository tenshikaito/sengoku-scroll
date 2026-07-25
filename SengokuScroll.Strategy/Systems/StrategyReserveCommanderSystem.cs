using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>日初扫描 InStronghold 单位：备队大将占位与将领就位/升阶替换。</summary>
public interface IStrategyReserveCommanderSystem : IGameSystem
{
}

public class StrategyReserveCommanderSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta) : IStrategyReserveCommanderSystem
{
    public int Order { get; } = 6;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        foreach (var stronghold in context.GameWorldContext.EachStronghold())
        {
            foreach (var unit in gameData.Units.Values)
            {
                if (!StrongholdGarrisonRules.IsInStrongholdAt(unit, stronghold))
                    continue;

                ReserveCommanderRules.TryUpgradeCommanderIfBetter(
                    unit, stronghold, gameData, scenarioMeta);
            }
        }
    }
}
