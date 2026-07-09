using Microsoft.Extensions.Logging;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.Systems;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Diagnostics;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Systems;

/// <summary>策略模式日初时间系统接口。</summary>
public interface IStrategyTimeSystem : IGameSystem
{
}

/// <summary>
/// 日初处理：恢复军事单位行动力，并允许其在本日移动。
/// 在每日 <see cref="Time.StrategyTimeController.AdvanceDay"/> 触发的系统链中最先执行。
/// </summary>
public class StrategyTimeSystem(
    IGameContext context,
    StrategyMovementTrace trace,
    ILogger<StrategyTimeSystem> logger) : IStrategyTimeSystem
{
    /// <summary>日循环最先执行。</summary>
    public int Order { get; } = 0;

    /// <inheritdoc />
    public void Update()
    {
        var recovery = context.GameRuleConfig.NextTurnApRecovery;

        foreach (var unit in context.GameWorldContext.EachUnit())
        {
            var apBefore = unit.Ap;
            var statusBefore = unit.Status;
            var routeCount = unit.ActionTarget.RoutePoints.Count;

            unit.Ap = Math.Min(unit.Movement, unit.Ap + recovery);
            unit.IsReadyToMove = true;

            if (unit.Status == UnitStatus.Waiting && unit.ActionTarget.RoutePoints.Count > 0)
                unit.Status = UnitStatus.Moving;

            if (unit.Status == UnitStatus.Moving && unit.ActionTarget.RoutePoints.Count == 0)
                unit.Status = UnitStatus.Waiting;

            if (statusBefore != unit.Status || apBefore != unit.Ap || routeCount > 0)
            {
                var detail =
                    $"AP {apBefore}->{unit.Ap} status {statusBefore}->{unit.Status} route={routeCount} peek={PeekRoute(unit)}";
                trace.Log("DayStart", "日初单位状态", unit.Id, unit.Location, detail: detail);
                logger.LogInformation(
                    "[StrategyMove] DayStart Unit {Id} at {Loc} {Detail}",
                    unit.Id, unit.Location, detail);
            }
        }

        var gameData = context.GameWorldContext.GameWorld.GameData;
        foreach (var convoy in gameData.SupplyConvoys.Values)
        {
            if (convoy.Status is not (SupplyConvoyStatus.Moving or SupplyConvoyStatus.Deceived))
                continue;

            var movement = convoy.Movement > 0 ? convoy.Movement : LogisticsConstants.ConvoyDailyAp;
            convoy.Movement = movement;
            convoy.Ap = Math.Min(movement, convoy.Ap + recovery);
        }
    }

    private static string PeekRoute(Unit unit)
    {
        if (!unit.ActionTarget.RoutePoints.TryPeek(out var p))
            return "-";
        return p.ToString();
    }
}
