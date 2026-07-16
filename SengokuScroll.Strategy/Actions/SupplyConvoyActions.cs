using SengokuScroll.Common.Types;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Calculators;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>运输队在途与到达相关的单步状态变更。</summary>
public static class SupplyConvoyActions
{
    /// <summary>
    /// 按人夫与护卫数量，从运输队载粮中扣除一日在途自耗。
    /// </summary>
    public static void ApplyDailyTransitConsumption(SupplyConvoy convoy)
    {
        var consumption = LogisticsCalculator.CalculateDailyTransitConsumption(
            convoy.PorterCount,
            convoy.EscortSoldierCount);

        convoy.CargoFoodGo = Math.Max(0, convoy.CargoFoodGo - consumption);
    }

    /// <summary>沿路径推进一格；返回是否已抵达终点。</summary>
    public static bool AdvanceOneStep(SupplyConvoy convoy)
    {
        // 业务：路径已走完则标记抵达
        if (convoy.RoutePoints.Count == 0)
        {
            convoy.Status = SupplyConvoyStatus.Arrived;
            return true;
        }

        convoy.Location = convoy.RoutePoints.Dequeue();

        if (convoy.RoutePoints.Count == 0)
        {
            convoy.Status = SupplyConvoyStatus.Arrived;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 运输队抵达目标部队后卸粮；不改变返程标记（由调度器安排返程）。
    /// </summary>
    public static void DeliverCargoToUnit(SupplyConvoy convoy, Unit targetUnit)
    {
        if (convoy.CargoFoodGo > 0)
            targetUnit.Food += convoy.CargoFoodGo;

        convoy.CargoFoodGo = 0;
        convoy.CargoMoney = 0;
        convoy.Status = SupplyConvoyStatus.Arrived;
    }

    /// <summary>运输队抵达目标据点后入库。</summary>
    public static void DeliverCargoToStronghold(SupplyConvoy convoy, Stronghold stronghold)
    {
        if (convoy.CargoFoodGo > 0)
            stronghold.ForceActor.Food += convoy.CargoFoodGo;

        if (convoy.CargoMoney > 0)
            stronghold.ForceActor.Money += convoy.CargoMoney;

        convoy.CargoFoodGo = 0;
        convoy.CargoMoney = 0;
        convoy.Status = SupplyConvoyStatus.Arrived;
    }

    /// <summary>
    /// 卸粮后开始返程：沿路径返回 <see cref="SupplyConvoy.OriginStrongholdId"/> 据点。
    /// </summary>
    public static void BeginReturnToOrigin(SupplyConvoy convoy, Queue<Point3> returnRoute)
    {
        convoy.IsReturningToOrigin = true;
        convoy.CargoFoodGo = 0;
        convoy.Ap = 0;
        convoy.Status = SupplyConvoyStatus.Moving;
        convoy.RoutePoints = returnRoute;
    }
}
