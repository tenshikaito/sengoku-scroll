using Microsoft.Extensions.Logging;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Localization;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式 AI 系统接口。</summary>
public interface IStrategyAISystem : IAISystem
{
}

/// <summary>
/// 策略 AI：全势力军事单位按当前方针、状态与战局自主行军/接敌；
/// 决策结果写入 <see cref="StrategyAiDecisionTrace"/> 供 debug。
/// </summary>
public class StrategyAISystem(
    IGameContext context,
    StrategyScenarioMeta scenarioMeta,
    IPathfindingService pathfinding,
    StrategyAiDecisionTrace aiTrace,
    IStrategyDayDebugLog dayDebugLog,
    BattleReportDeliveryHelper battleReportDeliveryHelper,
    ILogger<StrategyAISystem> logger) : IStrategyAISystem
{
    /// <summary>在单位移动之前决策（便于同日接敌结算）。</summary>
    public int Order { get; } = 18;

    public void Update()
    {
        var worldContext = context.GameWorldContext;
        var gameData = worldContext.GameWorld.GameData;
        var mapMaster = worldContext.GameWorld.GameMapMasterData;
        var rules = context.GameRuleConfig;
        var playerForceId = scenarioMeta.PlayerForceId;

        // 业务：日初先于单位 AI——威胁逼近时占格守城，封锁时抽象出击
        foreach (var stronghold in worldContext.EachStronghold())
        {
            if (GarrisonBehaviorRules.TryExecuteStrongholdDefense(
                    worldContext,
                    stronghold,
                    gameData,
                    scenarioMeta,
                    out var defenseCode))
            {
                var thought = new StrategyAiThought().Add(defenseCode!);
                var defenseDecision = StrategyAiDecision.Ok(
                    defenseCode!, $"{stronghold.Name} 守军防御", thought);
                var garrison = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData);
                if (garrison is not null)
                    defenseDecision = StrategyAiDecision.WithUnitContext(defenseDecision, garrison);

                aiTrace.LogAction(
                    garrison?.Id ?? stronghold.Id,
                    garrison?.Name ?? stronghold.Name,
                    stronghold.ForceId,
                    "Support",
                    defenseDecision);

                dayDebugLog.LogLocalized(
                    "Garrison",
                    LocalizationKeys.Debug.GarrisonDefense,
                    stronghold.Id,
                    stronghold.Name,
                    defenseCode!);

                if (defenseCode == "GarrisonOccupyTile")
                {
                    var threats = GarrisonBehaviorRules.FindFieldBattleProximityThreats(stronghold, gameData);
                    var threatDesc = string.Join(", ",
                        threats.Select(t =>
                            $"{t.Name}#{t.Id}({t.Location.X},{t.Location.Y}) dist={Math.Abs(t.Location.X - stronghold.Location.X) + Math.Abs(t.Location.Y - stronghold.Location.Y)}"));
                    dayDebugLog.LogLine(
                        "Garrison",
                        $"守军占格触发：{stronghold.Name}({stronghold.Location.X},{stronghold.Location.Y}) " +
                        $"威胁半径={GarrisonBehaviorRules.ThreatManhattanDistance} | 威胁=[{threatDesc}]");
                }
            }

            if (GarrisonBehaviorRules.TryDissolveGarrisonWhenSafe(worldContext, stronghold, gameData))
            {
                dayDebugLog.LogLine(
                    "Garrison",
                    $"守军解散回城：{stronghold.Name} (id={stronghold.Id})");
            }
        }

        foreach (var force in gameData.Forces.Values)
        {
            if (force.Id == playerForceId)
                continue;

            if (StrategyUnitAIRules.TryDispatchLordRelief(
                    force.Id,
                    gameData,
                    scenarioMeta,
                    pathfinding,
                    worldContext))
            {
                dayDebugLog.LogLine(
                    "AI",
                    $"居城援防派出：势力 {force.Id} ({force.Name})");
            }
        }

        var militaryUnits = worldContext.EachUnit().ToList();
        foreach (var unit in militaryUnits)
        {
            if (StrategyUnitAIRules.ShouldSkipDailyAi(unit))
            {
                var reason = StrategyUnitAIRules.DescribeSkipReason(unit) ?? "跳过";
                aiTrace.LogSkip(unit.Id, unit.Name, unit.ForceId, reason);
                continue;
            }

            var directiveDecision = StrategyUnitAIRules.EvaluateDirective(
                unit, gameData, playerForceId, mapMaster);
            aiTrace.LogDirective(unit.Id, unit.Name, unit.ForceId, directiveDecision);

            if (directiveDecision.Changed)
            {
                logger.LogDebug(
                    "[StrategyAI] Unit {UnitId} {Name} directive {From}→{To}: {Message}",
                    unit.Id, unit.Name, directiveDecision.FromDirective, directiveDecision.ToDirective,
                    directiveDecision.Message);
            }

            var hostileUnits = StrategyUnitAIRules.ResolveHostileUnits(unit, gameData);
            var hostileStrongholds = StrategyUnitAIRules.ResolveHostileStrongholds(unit, gameData);

            var action = StrategyUnitAIRules.ExecuteDailyAction(
                unit,
                gameData,
                pathfinding,
                hostileUnits,
                hostileStrongholds,
                worldContext,
                rules,
                scenarioMeta,
                mapMaster);

            aiTrace.LogAction(
                unit.Id,
                unit.Name,
                unit.ForceId,
                unit.Directive.ToString(),
                StrategyAiDecision.WithUnitContext(action, unit));

            if (action.Code is "SiegeAssault" or "SiegeEncircle"
                && unit.ActionTarget.StrongholdId > 0
                && gameData.Strongholds.TryGetValue(unit.ActionTarget.StrongholdId, out var siegeTarget))
            {
                var siegeDefender = StrongholdGarrisonRules.FindGarrisonUnit(siegeTarget, gameData);
                battleReportDeliveryHelper.DeliverSiegeOrderStartedReport(
                    unit,
                    siegeTarget,
                    unit.SiegeMode,
                    gameData,
                    siegeDefender);
            }

            logger.LogDebug(
                "[StrategyAI] Unit {UnitId} {Name} action {Code} ok={Ok}: {Message} steps={StepCount}",
                unit.Id, unit.Name, action.Code, action.IsSuccess, action.Message, action.Steps.Count);
        }
    }
}