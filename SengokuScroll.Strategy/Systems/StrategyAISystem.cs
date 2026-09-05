using Microsoft.Extensions.Logging;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Services.Pathfinding;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Localization;
using SengokuScroll.Strategy.Actions;
using SengokuScroll.Strategy.Data.Models;
using SengokuScroll.Strategy.Diagnostics;
using SengokuScroll.Strategy.Helpers;
using SengokuScroll.Strategy.Rules;
using SengokuScroll.Strategy.Vision;
using static SengokuScroll.Domain.Entities.Unit;

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
    StrategyFieldEngagementRegistry engagementRegistry,
    IStrategyDayDebugLog dayDebugLog,
    BattleReportDeliveryHelper battleReportDeliveryHelper,
    StrategyVisibilityLedger visibilityLedger,
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

        // AI 决策前刷新各势力视野；单位可能在上一系统或测试布置中刚刚移动。
        // 只读意图基于这一快照，随后仍按固定顺序应用，保证回放确定性。
        visibilityLedger.Recompute(worldContext.GameWorld, scenarioMeta);

        // 业务：日初清理无效接敌锁定，避免 AI 永久 Skip
        engagementRegistry.PruneOrphanEngagements(gameData);

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

        }

        foreach (var force in gameData.Forces.Values)
        {
            if (!StrategyAiControlRules.IsForceAiControlled(scenarioMeta, force.Id))
                continue;

            if (StrategyUnitAIRules.TryDispatchLordRelief(
                    force.Id,
                    gameData,
                    scenarioMeta,
                    pathfinding,
                    worldContext,
                    visibilityLedger))
            {
                dayDebugLog.LogLine(
                    "AI",
                    $"居城援防派出：势力 {force.Id} ({force.Name})");
            }
        }

        if (EconomyRules.IsMonthlySettlementDay(gameData.GameDate))
        {
            foreach (var force in gameData.Forces.Values)
            {
                if (!StrategyAiControlRules.IsForceAiControlled(scenarioMeta, force.Id))
                    continue;

                foreach (var stronghold in gameData.Strongholds.Values.Where(x => x.ForceId == force.Id))
                {
                    var economicDecision = StrategyEconomicAiRules.Evaluate(stronghold, force, gameData);
                    if (economicDecision is null)
                        continue;

                    StrongholdDomesticActions.ApplyTaxRateChange(stronghold, economicDecision.Change);
                    dayDebugLog.LogLine(
                        "AI-Economy",
                        $"{force.Name}/{stronghold.Name} 税制={economicDecision.Policy} {economicDecision.Reason} " +
                        $"税率={stronghold.PollTaxRate}/{stronghold.AgricultureTaxRate}/" +
                        $"{stronghold.CommerceTaxRate}/{stronghold.TariffTaxRate}");
                }

                if (gameData.Characters.Values.Any(x => x.ForceId == force.Id && x.DiplomacyMission is not null))
                    continue;

                var peaceTargetId = StrategyDiplomacyAiRules.SelectPeaceTarget(force.Id, gameData);
                if (peaceTargetId is not int targetForceId)
                    continue;

                var envoy = DiplomacyMissionRules.ListAssignableEnvoys(gameData, scenarioMeta, force.Id)
                    .OrderByDescending(x => x.Politics + x.Charm)
                    .ThenBy(x => x.Id)
                    .FirstOrDefault();
                if (envoy is null)
                    continue;

                if (DiplomacyMissionActions.TryAssignMissionForForce(
                        gameData,
                        scenarioMeta,
                        force.Id,
                        envoy.Id,
                        targetForceId,
                        "Peace",
                        out _))
                {
                    dayDebugLog.LogLine(
                        "AI-Diplomacy",
                        $"{force.Name} 派遣 {envoy.Name} 向势力 {targetForceId} 求和");
                }
            }
        }

        var militaryUnits = worldContext.EachUnit().Where(unit => unit.IsMilitary).ToList();
        var forceRepresentatives = militaryUnits
            .GroupBy(unit => unit.ForceId)
            .OrderBy(group => group.Key)
            .Select(group => group.OrderBy(unit => unit.Id).First())
            .ToArray();
        var forceObservations = StrategyParallelWork.MapOrdered(
                forceRepresentatives,
                representative => new ForceObservation(
                    StrategyUnitAIRules.ResolveObservedHostileUnits(
                        representative,
                        gameData,
                        visibilityLedger),
                    StrategyUnitAIRules.ResolveObservedHostileStrongholds(
                        representative,
                        gameData,
                        visibilityLedger)),
                minimumParallelCount: 4)
            .Select((observation, index) => (forceRepresentatives[index].ForceId, observation))
            .ToDictionary(entry => entry.ForceId, entry => entry.observation);

        foreach (var unit in militaryUnits)
        {
            if (unit.Status == UnitStatus.Standoff)
            {
                var standoffBreak = StrategyUnitAIRules.TryResolveStandoffEngagement(
                    unit, gameData, engagementRegistry, mapMaster);
                if (standoffBreak is { } breakDecision)
                {
                    aiTrace.LogAction(
                        unit.Id,
                        unit.Name,
                        unit.ForceId,
                        unit.Directive.ToString(),
                        StrategyAiDecision.WithUnitContext(breakDecision, unit));
                    continue;
                }
            }

            if (StrategyUnitAIRules.ShouldSkipDailyAi(unit))
            {
                var reason = StrategyUnitAIRules.DescribeSkipReason(unit) ?? "跳过";
                aiTrace.LogSkip(unit.Id, unit.Name, unit.ForceId, reason);
                continue;
            }

            var observation = forceObservations[unit.ForceId];
            var hostileUnits = observation.HostileUnits;
            var hostileStrongholds = observation.HostileStrongholds;

            var directiveDecision = StrategyUnitAIRules.EvaluateDirective(
                unit,
                gameData,
                playerForceId,
                mapMaster,
                scenarioMeta,
                hostileUnits,
                hostileStrongholds);
            aiTrace.LogDirective(unit.Id, unit.Name, unit.ForceId, directiveDecision);

            if (directiveDecision.Changed)
            {
                logger.LogDebug(
                    "[StrategyAI] Unit {UnitId} {Name} directive {From}→{To}: {Message}",
                    unit.Id, unit.Name, directiveDecision.FromDirective, directiveDecision.ToDirective,
                    directiveDecision.Message);
            }

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

    private sealed record ForceObservation(
        IReadOnlyList<Domain.Entities.Unit> HostileUnits,
        IReadOnlyList<Domain.Entities.Stronghold> HostileStrongholds);
}
