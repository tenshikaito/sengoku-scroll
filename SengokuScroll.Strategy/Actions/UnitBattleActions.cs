using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Unit;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Rules;

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
        BattlefieldEngagementRules.EnterBattlefield(attacker, defenderUnitId);
    }

    /// <summary>应用伤亡；兵数为 0 时进入混乱状态。有编制时同步分摊到 SubUnit。</summary>
    public static void ApplyCasualties(Unit unit, int casualties, GameData? gameData = null)
    {
        if (casualties <= 0)
            return;

        // 业务：有编制时按子队分摊伤亡，避免主单位单独扣减
        if (gameData is not null && unit.SubUnitIds.Count > 0)
        {
            TacticalBattleSimulator.DistributeCasualtiesToSubUnits(unit, casualties, gameData);
            if (unit.Soldier == 0)
                unit.Status = UnitStatus.Chaos;
            return;
        }

        unit.Soldier = Math.Max(0, unit.Soldier - casualties);
        if (unit.Soldier == 0)
            unit.Status = UnitStatus.Chaos;
    }
}
