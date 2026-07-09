using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Actions;

/// <summary>瞬间战后对单位状态的变更（M3-a）。</summary>
public static class UnitBattleActions
{
    /// <summary>扣除攻击 AP 并将姿态设为攻击中。</summary>
    public static void MarkAttacked(Unit attacker, GameRuleConfig rules)
    {
        attacker.Ap = Math.Max(0, attacker.Ap - rules.AttackAp);
        attacker.Status = UnitStatus.Waiting;
        attacker.ActionTarget.RoutePoints.Clear();
    }

    /// <summary>下达攻击命令（日推进后结算，不立即开战）。</summary>
    public static void QueueAttack(Unit attacker, int defenderUnitId)
    {
        attacker.Stance = UnitStance.Attacking;
        attacker.ActionTarget.UnitId = defenderUnitId;
        attacker.ActionTarget.RoutePoints.Clear();
    }

    /// <summary>应用伤亡；兵数为 0 时进入混乱状态。</summary>
    public static void ApplyCasualties(Unit unit, int casualties)
    {
        unit.Soldier = Math.Max(0, unit.Soldier - casualties);
        if (unit.Soldier == 0)
            unit.Status = UnitStatus.Chaos;
    }
}
