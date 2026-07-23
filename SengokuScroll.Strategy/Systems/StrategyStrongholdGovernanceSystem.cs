using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式据点政务方针系统接口。</summary>
public interface IStrategyStrongholdGovernanceSystem : IGameSystem
{
}

/// <summary>
/// 每月 1 日按据点方针向城内待命将领自动发布任务令。
/// 本家据点遵循当主设定；旗下内藩自行评估方针。
/// </summary>
public class StrategyStrongholdGovernanceSystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta) : IStrategyStrongholdGovernanceSystem
{
    public int Order { get; } = 15;

    public void Update()
    {
        var gameData = context.GameWorldContext.GameWorld.GameData;
        if (!EconomyRules.IsMonthlySettlementDay(gameData.GameDate))
            return;

        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (StrongholdDomesticRules.IsPlayerRealmStronghold(stronghold, scenarioMeta, gameData))
            {
                StrongholdGovernanceActions.ProcessMonthlyGovernanceAssignments(
                    stronghold,
                    gameData,
                    scenarioMeta);
                continue;
            }

            if (StrongholdGovernanceRules.IsInnerVassalRealmStrongholdUnderPlayer(
                    stronghold,
                    scenarioMeta,
                    gameData))
            {
                StrongholdGovernanceActions.ProcessMonthlyGovernanceAssignments(
                    stronghold,
                    gameData,
                    scenarioMeta,
                    innerVassalSelfGoverned: true);
            }
        }
    }
}
