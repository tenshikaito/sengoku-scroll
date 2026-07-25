using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>从居城 SubUnit 池组建 Unit；兼容旧「出征」API。</summary>
public static class UnitDeploymentActions
{
    public static GameResult<Unit> DeployFromStronghold(
        IGameWorldContext context,
        Stronghold stronghold,
        StrategyScenarioMeta meta,
        GameData gameData,
        int playerForceId,
        string unitName,
        int commanderId,
        IReadOnlyList<StrategyDeployCompositionEntry> composition,
        int? food = null,
        int? money = null,
        bool deployToMap = false)
        => UnitComposeActions.ComposeFromStronghold(
            context,
            stronghold,
            meta,
            gameData,
            playerForceId,
            unitName,
            commanderId,
            composition,
            deployToMap,
            food,
            money);
}
