using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>运输 Unit（Convoy/Migrant/在途 Merchant）查询与状态判定。</summary>
public static class TransportUnitRules
{
    /// <summary>是否为地图运输单位（非城内军事/贸易待命）。</summary>
    public static bool IsTransportUnit(Unit unit)
        => unit.Kind switch
        {
            UnitKind.Convoy or UnitKind.Migrant => !unit.InStronghold,
            UnitKind.Merchant => !unit.InStronghold
                && (unit.TransportTargetStrongholdId > 0
                    || unit.TransportTargetUnitId > 0
                    || unit.ActionTarget.RoutePoints.Count > 0),
            _ => false
        };

    /// <summary>是否沿路径在途移动（含迷惑滞留前的在途阶段）。</summary>
    public static bool IsInTransit(Unit unit)
        => IsTransportUnit(unit)
           && unit.Status == UnitStatus.Moving
           && !(unit.IsDeceived && unit.DeceivedHoldDaysRemaining > 0);

    /// <summary>是否已抵达终点、待调度器卸货/返程。</summary>
    public static bool HasArrived(Unit unit)
        => IsTransportUnit(unit) && unit.Status == UnitStatus.Waiting;

    /// <summary>是否仍为有效运输任务（在途、迷惑或已抵达待处理）。</summary>
    public static bool IsActiveTransport(Unit unit)
        => IsTransportUnit(unit)
           && unit.Status is UnitStatus.Moving or UnitStatus.Waiting;

    /// <summary>迷惑状态（对应旧 SupplyConvoyStatus.Deceived）。</summary>
    public static bool IsDeceivedTransport(Unit unit)
        => unit.IsDeceived
           && (unit.DeceivedHoldDaysRemaining > 0 || unit.Status == UnitStatus.Moving);

    /// <summary>前端/情报兼容：映射为旧运输队 Status 字符串。</summary>
    public static string MapTransportStatusLabel(Unit unit)
    {
        if (IsDeceivedTransport(unit) && unit.DeceivedHoldDaysRemaining > 0)
            return SupplyConvoyStatus.Deceived.ToString();

        if (unit.Status == UnitStatus.Waiting)
            return SupplyConvoyStatus.Arrived.ToString();

        return SupplyConvoyStatus.Moving.ToString();
    }

    /// <summary>遍历全部有效运输 Unit。</summary>
    public static IEnumerable<Unit> EnumerateActiveTransportUnits(GameData gameData)
        => gameData.Units.Values.Where(IsActiveTransport);

    /// <summary>目标军事单位是否已有在途或返程运输任务。</summary>
    public static bool HasActiveConvoyForUnit(GameData gameData, int unitId)
        => gameData.Units.Values.Any(u =>
            IsActiveTransport(u)
            && u.TransportTargetUnitId == unitId);

    /// <summary>同出发/目标据点是否已有在途贡纳/税赋运输。</summary>
    public static bool HasActiveTributeTransport(
        GameData gameData,
        int originStrongholdId,
        int targetStrongholdId)
        => gameData.Units.Values.Any(u =>
            IsActiveTransport(u)
            && !u.IsReturningToOrigin
            && u.TransportOriginStrongholdId == originStrongholdId
            && u.TransportTargetStrongholdId == targetStrongholdId
            && u.TransportPurpose is TransportPurpose.Tribute or TransportPurpose.TaxMoney);

    /// <summary>同出发/目标据点是否已有在途贸易运输。</summary>
    public static bool HasActiveTradeTransport(
        GameData gameData,
        int originStrongholdId,
        int destinationStrongholdId)
        => gameData.Units.Values.Any(u =>
            IsActiveTransport(u)
            && !u.IsReturningToOrigin
            && u.TransportPurpose == TransportPurpose.Trade
            && u.TransportOriginStrongholdId == originStrongholdId
            && u.TransportTargetStrongholdId == destinationStrongholdId);

    /// <summary>目标单位 inbound 运输队（不含 Destroyed）。</summary>
    public static IEnumerable<Unit> GetInboundTransportsForUnit(int unitId, GameData gameData)
        => gameData.Units.Values.Where(u =>
            u.TransportTargetUnitId == unitId && IsTransportUnit(u));

    /// <summary>出发据点是否已有移民队在途。</summary>
    public static bool HasActiveMigrantFromOrigin(GameData gameData, int originStrongholdId)
        => gameData.Units.Values.Any(u =>
            IsActiveTransport(u)
            && u.TransportPurpose == TransportPurpose.Migrant
            && u.TransportOriginStrongholdId == originStrongholdId);
}
