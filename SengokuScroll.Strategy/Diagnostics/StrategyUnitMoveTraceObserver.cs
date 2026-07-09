using Microsoft.Extensions.Logging;
using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Diagnostics;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Rules;

namespace SengokuScroll.Strategy.Diagnostics;

/// <summary>将单位逐步移动写入 <see cref="StrategyMovementTrace"/> 与 ILogger。</summary>
public sealed class StrategyUnitMoveTraceObserver(
    StrategyMovementTrace trace,
    MovementRules movementRules,
    ILogger<StrategyUnitMoveTraceObserver> logger) : IUnitMoveObserver
{
    public void OnMoveStepEvaluated(Unit unit, Point2 target, GameResult result)
    {
        var cost = movementRules.GetTileMovementApCost(unit, target);
        var strongholdExtra = movementRules.RequiresStrongholdEntryAp(unit, target);
        var detail =
            $"AP={unit.Ap} cost={cost} strongholdExtra={strongholdExtra} ok={result.IsSuccess} err={result.Error?.Code ?? "-"}";

        trace.Log("MoveEval", "评估移动", unit.Id, unit.Location, target, detail);
        logger.LogInformation(
            "[StrategyMove] Unit {UnitId} eval ({From})->({Target}) {Detail}",
            unit.Id, unit.Location, target, detail);

        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "[StrategyMove] Unit {UnitId} BLOCKED at ({From})->({Target}) reason={Reason}",
                unit.Id, unit.Location, target, result.Error?.Code);
        }
    }

    public void OnMoveStepCompleted(Unit unit, Point2 from, Point2 to, int apRemaining, int routeRemaining)
    {
        var detail = $"AP_left={apRemaining} route_left={routeRemaining} status={unit.Status}";
        trace.Log("MoveDone", "移动完成", unit.Id, from, to, detail);
        logger.LogInformation(
            "[StrategyMove] Unit {UnitId} moved ({From})->({To}) {Detail}",
            unit.Id, from, to, detail);
    }

    public void OnMoveSkipped(Unit unit, string reason)
    {
        trace.Log("MoveSkip", reason, unit.Id, unit.Location, detail: $"status={unit.Status} route={unit.ActionTarget.RoutePoints.Count}");
        logger.LogInformation("[StrategyMove] Unit {UnitId} skip: {Reason} status={Status} route={RouteCount}",
            unit.Id, reason, unit.Status, unit.ActionTarget.RoutePoints.Count);
    }
}
