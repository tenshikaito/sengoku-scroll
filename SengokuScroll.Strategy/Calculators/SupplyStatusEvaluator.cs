using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Calculators;

/// <summary>单位补给三态（充足 / 紧张 / 断绝）衍生评估（M3-c）。</summary>
public static class SupplyStatusEvaluator
{
    /// <summary>补给充足——携粮与在途补给均充裕。</summary>
    public const string Sufficient = "Sufficient";
    /// <summary>补给紧张——携粮偏低或在途补给被迷惑。</summary>
    public const string Strained = "Strained";
    /// <summary>补给断绝——无粮且无可用在途/后方粮源。</summary>
    public const string CutOff = "CutOff";

    /// <summary>评估单位当前补给状态。</summary>
    public static string EvaluateStatus(Unit unit, GameData gameData)
    {
        var inbound = GetInboundConvoys(unit.Id, gameData);

        // 业务：无携粮且无在途载货 → 断绝
        if (unit.Food <= 0 && !HasUsableInboundCargo(inbound))
            return CutOff;

        // 业务：在途补给队被迷惑 → 紧张
        if (inbound.Any(c => c.Status is SupplyConvoyStatus.Deceived || c.IsDeceived))
            return Strained;

        // 业务：携粮低于阈值时，有在途或后方粮源为紧张，否则断绝
        if (unit.Food < SupplyDispatchConstants.UnitFoodThresholdGo)
        {
            if (HasUsableInboundCargo(inbound))
                return Strained;

            return HasResupplySource(unit, gameData) ? Strained : CutOff;
        }

        return Sufficient;
    }

    /// <summary>目标单位在途补给摘要（不含返程空载队）。</summary>
    public static IReadOnlyList<InTransitSupplySummary> GetInTransitSummaries(Unit unit, GameData gameData)
    {
        var list = new List<InTransitSupplySummary>();

        foreach (var convoy in GetInboundConvoys(unit.Id, gameData))
        {
            if (convoy.IsReturningToOrigin || convoy.CargoFoodGo <= 0)
                continue;

            list.Add(new InTransitSupplySummary
            {
                ConvoyId = convoy.Id,
                CargoFoodGo = convoy.CargoFoodGo,
                EstimatedDays = EstimateArrivalDays(convoy),
                IsDeceived = convoy.IsDeceived || convoy.Status is SupplyConvoyStatus.Deceived,
                OriginStrongholdId = convoy.OriginStrongholdId
            });
        }

        return list;
    }

    /// <summary>携带粮可供维持的天数（至少 0）。</summary>
    public static int EstimateFoodDaysRemaining(Unit unit)
    {
        var daily = LogisticsCalculator.CalculateUnitDailyFoodConsumption(unit.Soldier);
        if (daily <= 0)
            return unit.Food > 0 ? int.MaxValue : 0;

        return Math.Max(0, unit.Food / daily);
    }

    private static IEnumerable<SupplyConvoy> GetInboundConvoys(int unitId, GameData gameData)
        => gameData.SupplyConvoys.Values.Where(c =>
            c.TargetUnitId == unitId
            && c.Status is not SupplyConvoyStatus.Destroyed);

    private static bool HasUsableInboundCargo(IEnumerable<SupplyConvoy> inbound)
        => inbound.Any(c => !c.IsReturningToOrigin && c.CargoFoodGo > 0);

    private static bool HasResupplySource(Unit unit, GameData gameData)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != unit.ForceId)
                continue;

            if (stronghold.ForceActor.Food >= SupplyDispatchConstants.StrongholdMinFoodGo)
                return true;
        }

        return false;
    }

    private static int EstimateArrivalDays(SupplyConvoy convoy)
    {
        var days = convoy.RoutePoints.Count;
        if (convoy.IsDeceived && convoy.DeceivedHoldDaysRemaining > 0)
            days += convoy.DeceivedHoldDaysRemaining;

        return Math.Max(days, convoy.Status is SupplyConvoyStatus.Arrived ? 0 : 1);
    }

    public sealed record InTransitSupplySummary
    {
        public required int ConvoyId { get; init; }

        public required int CargoFoodGo { get; init; }

        /// <summary>预计到达天数（含迷惑滞留）。</summary>
        public required int EstimatedDays { get; init; }

        public required bool IsDeceived { get; init; }

        public required int OriginStrongholdId { get; init; }
    }
}
