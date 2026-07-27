using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

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
        var inbound = GetInboundTransports(unit.Id, gameData);

        if (unit.Food <= 0 && !HasUsableInboundCargo(inbound))
            return CutOff;

        if (inbound.Any(TransportUnitRules.IsDeceivedTransport))
            return Strained;

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

        foreach (var transport in GetInboundTransports(unit.Id, gameData))
        {
            if (transport.IsReturningToOrigin || transport.Food <= 0)
                continue;

            list.Add(new InTransitSupplySummary
            {
                ConvoyId = transport.Id,
                CargoFoodGo = transport.Food,
                EstimatedDays = EstimateArrivalDays(transport),
                IsDeceived = TransportUnitRules.IsDeceivedTransport(transport),
                OriginStrongholdId = transport.TransportOriginStrongholdId
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

    private static IEnumerable<Unit> GetInboundTransports(int unitId, GameData gameData)
        => TransportUnitRules.GetInboundTransportsForUnit(unitId, gameData)
            .Where(TransportUnitRules.IsActiveTransport);

    private static bool HasUsableInboundCargo(IEnumerable<Unit> inbound)
        => inbound.Any(t => !t.IsReturningToOrigin && t.Food > 0);

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

    private static int EstimateArrivalDays(Unit transport)
    {
        var days = transport.ActionTarget.RoutePoints.Count;
        if (transport.IsDeceived && transport.DeceivedHoldDaysRemaining > 0)
            days += transport.DeceivedHoldDaysRemaining;

        return Math.Max(days, TransportUnitRules.HasArrived(transport) ? 0 : 1);
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
