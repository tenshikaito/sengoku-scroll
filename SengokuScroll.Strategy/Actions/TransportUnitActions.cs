using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Contexts;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>运输 Unit 在途与到达相关的单步状态变更。</summary>
public static class TransportUnitActions
{
    /// <summary>按人夫与护卫数量，从运输 Unit 载粮中扣除一日在途自耗。</summary>
    public static void ApplyDailyTransitConsumption(Unit unit)
    {
        var consumption = LogisticsCalculator.CalculateDailyTransitConsumption(
            unit.PorterCount,
            unit.EscortSoldierCount);

        unit.Food = Math.Max(0, unit.Food - consumption);
    }

    /// <summary>沿路径推进一格；返回是否已抵达终点。</summary>
    public static bool AdvanceOneStep(IGameWorldContext context, Unit unit)
    {
        if (unit.ActionTarget.RoutePoints.Count == 0)
        {
            unit.Status = UnitStatus.Waiting;
            return true;
        }

        var next = unit.ActionTarget.RoutePoints.Dequeue();
        MapLocationActions.SetUnitLocation(context, unit, new Point3(next.X, next.Y, unit.Location.Z));

        if (unit.ActionTarget.RoutePoints.Count == 0)
        {
            unit.Status = UnitStatus.Waiting;
            return true;
        }

        return false;
    }

    /// <summary>运输 Unit 抵达目标部队后卸粮。</summary>
    public static void DeliverCargoToUnit(Unit transport, Unit targetUnit)
    {
        if (transport.Food > 0)
            targetUnit.Food += transport.Food;

        transport.Food = 0;
        transport.Money = 0;
        transport.Status = UnitStatus.Waiting;
    }

    /// <summary>运输 Unit 抵达目标据点后入库。</summary>
    public static void DeliverCargoToStronghold(Unit transport, Stronghold stronghold)
    {
        if (transport.Food > 0)
            stronghold.ForceActor.Food += transport.Food;

        if (transport.Money > 0)
            stronghold.ForceActor.Money += transport.Money;

        transport.Food = 0;
        transport.Money = 0;
        transport.Status = UnitStatus.Waiting;
    }

    /// <summary>卸货后开始返程：沿路径返回出发据点。</summary>
    public static void BeginReturnToOrigin(Unit transport, Queue<Point2> returnRoute)
    {
        transport.IsReturningToOrigin = true;
        transport.Food = 0;
        transport.Money = 0;
        transport.Ap = 0;
        transport.Status = UnitStatus.Moving;
        transport.ActionTarget.RoutePoints = returnRoute;
    }

    /// <summary>标记运输 Unit 迷惑状态。</summary>
    public static void ApplyDeceivedHold(Unit transport, int holdDays)
    {
        transport.IsDeceived = true;
        transport.DeceivedHoldDaysRemaining = holdDays;
        transport.ActionTarget.RoutePoints.Clear();
    }

    /// <summary>从地图移除被毁运输 Unit。</summary>
    public static void DestroyTransport(IGameWorldContext context, Unit transport)
    {
        if (!TransportUnitRules.IsTransportUnit(transport))
            return;

        MapLocationActions.RemoveUnit(context, transport);
    }
}
