using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>信使投递相关的单步状态变更。</summary>
public static class MessengerActions
{
    /// <summary>将方针立即写入目标单位（同格免信使时使用）。</summary>
    public static void ApplyPolicyChange(Unit targetUnit, UnitDirective directive)
        => targetUnit.Directive = directive;

    /// <summary>
    /// 将信使携带的假情报作用于运输队：进入迷惑状态、清空原路径、设置误导目标。
    /// </summary>
    public static void ApplyFalseIntelligence(SupplyConvoy convoy, Messenger messenger)
    {
        convoy.IsDeceived = true;
        convoy.Status = SupplyConvoyStatus.Deceived;
        convoy.DeceivedHoldDaysRemaining = Constants.LogisticsConstants.FalseIntelligenceHoldDays;
        convoy.DeceivedRedirect = messenger.Location;
        convoy.RoutePoints.Clear();
    }

    /// <summary>信使抵达后，将待投递方针写入目标单位。</summary>
    public static void DeliverPendingPolicy(Messenger messenger, Unit targetUnit)
    {
        if (messenger.PendingDirective is not { } directive)
            return;

        ApplyPolicyChange(targetUnit, directive);
        messenger.PendingDirective = null;
    }
}
