using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>文书载体投递相关的单步状态变更。</summary>
public static class MessageCarrierActions
{
    /// <summary>将方针立即写入目标单位（同格免载体时使用）。</summary>
    public static void ApplyPolicyChange(Unit targetUnit, UnitDirective directive)
        => targetUnit.Directive = directive;

    /// <summary>
    /// 将载体携带的假情报作用于运输队：进入迷惑状态、清空原路径、设置误导目标。
    /// </summary>
    public static void ApplyFalseIntelligence(SupplyConvoy convoy, MessageCarrier carrier)
    {
        convoy.IsDeceived = true;
        convoy.Status = SupplyConvoyStatus.Deceived;
        convoy.DeceivedHoldDaysRemaining = Constants.LogisticsConstants.FalseIntelligenceHoldDays;
        convoy.DeceivedRedirect = carrier.Location;
        convoy.RoutePoints.Clear();
    }

    /// <summary>载体抵达后，将待投递方针写入目标单位。</summary>
    public static void DeliverPendingPolicy(MessageCarrier carrier, Unit targetUnit)
    {
        if (carrier.Payload.PendingDirective is not { } directive)
            return;

        ApplyPolicyChange(targetUnit, directive);
        carrier.Payload.PendingDirective = null;
    }

    /// <summary>将税率变更立即写入据点（直辖城或同格免载体）。</summary>
    public static bool ApplyTaxRateChange(
        Stronghold stronghold,
        PendingStrongholdTaxChange change,
        out string? error)
        => StrongholdDomesticActions.TrySetTaxRates(
            stronghold,
            change.PollTaxRate,
            change.AgricultureTaxRate,
            change.CommerceTaxRate,
            change.TariffTaxRate,
            out error);

    /// <summary>载体抵达后，将待投递税率写入目标据点。</summary>
    public static bool DeliverPendingTaxChange(MessageCarrier carrier, Stronghold stronghold)
    {
        if (carrier.Payload.PendingTaxChange is not { } change)
            return false;

        var ok = ApplyTaxRateChange(stronghold, change, out _);
        if (ok)
            carrier.Payload.PendingTaxChange = null;

        return ok;
    }
}
