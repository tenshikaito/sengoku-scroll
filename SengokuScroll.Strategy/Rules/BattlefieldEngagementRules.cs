using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>战场接敌与对峙状态：进入、维持、脱离（兼容一对一对手 Id；真实归属在 Battlefield）。</summary>
public static class BattlefieldEngagementRules
{
    /// <summary>部队是否处于与指定敌军的对峙战场状态。</summary>
    public static bool IsInBattlefield(Unit unit)
        => unit.BattlefieldId > 0
           || (unit.Status == UnitStatus.Standoff && unit.ActionTarget.UnitId > 0);

    /// <summary>两军接敌：进入攻击姿态并锁定对手，非溃乱/恐惧/被围部队进入对峙。</summary>
    public static void EnterBattlefield(Unit unit, int opponentUnitId)
    {
        unit.Stance = UnitStance.Attacking;
        unit.ActionTarget.UnitId = opponentUnitId;
        unit.ActionTarget.RoutePoints.Clear();

        // 业务：溃乱、恐惧、被包围、溃逃部队保持原状态，不进入正常对峙
        if (unit.Status is UnitStatus.Chaos or UnitStatus.Fearful or UnitStatus.BeingSurround or UnitStatus.Routing)
            return;

        unit.Status = UnitStatus.Standoff;
    }

    /// <summary>对峙日续：维持攻击姿态与 Standoff 状态。</summary>
    public static void MaintainStandoff(Unit unit, int opponentUnitId)
    {
        unit.Stance = UnitStance.Attacking;
        unit.ActionTarget.UnitId = opponentUnitId;
        unit.Status = UnitStatus.Standoff;
    }

    /// <summary>脱离战场：恢复常态，清除攻击目标与战场 Id。</summary>
    public static void LeaveBattlefield(Unit unit)
        => BattlefieldContainerRules.LeaveBattlefield(unit);
}
