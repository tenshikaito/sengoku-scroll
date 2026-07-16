using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Battle;

/// <summary>决战时执行的战斗方针（由单位 Directive/Stance 推断，见 §7）。</summary>
public enum BattleCombatDirective
{
    /// <summary>坚守：尽可能抵抗（默认守方）。</summary>
    HoldLine,

    /// <summary>死守：战斗到底，防御大幅提升。</summary>
    FightToDeath,

    /// <summary>迎击：主动出击，攻强守弱。</summary>
    CounterAttack,

    /// <summary>逃跑：尝试脱离，不主动纠缠。</summary>
    AttemptRetreat
}

/// <summary>从单位方针/姿态推断决战战斗方针，并写入 <see cref="BattleFactorBreakdown"/>。</summary>
public static class BattleDirectiveRules
{
    /// <summary>根据单位方针、姿态与攻守角色推断决战战斗方针。</summary>
    public static BattleCombatDirective ResolveCombatDirective(Unit unit, bool isDefenderRole)
    {
        // 业务：撤退方针优先，决战阶段不主动接敌
        if (unit.Directive == UnitDirective.Retreat)
            return BattleCombatDirective.AttemptRetreat;

        // 业务：坚守姿态视为死守，大幅提升守方防御
        if (unit.Stance == UnitStance.Hold)
            return BattleCombatDirective.FightToDeath;

        // 业务：占领/袭扰方针倾向主动出击
        if (unit.Directive is UnitDirective.Occupy or UnitDirective.Raid)
            return BattleCombatDirective.CounterAttack;

        if (isDefenderRole)
            return BattleCombatDirective.HoldLine;

        return unit.Directive == UnitDirective.Support
            ? BattleCombatDirective.HoldLine
            : BattleCombatDirective.CounterAttack;
    }

    /// <summary>将双方推断出的战斗方针转化为战力、胜率与伤亡修正。</summary>
    public static void ApplyCombatDirectives(
        BattleEvaluationContext ctx,
        BattleFactorBreakdown b)
    {
        ApplyDirectiveEffects(
            ctx,
            ResolveCombatDirective(ctx.Attacker, isDefenderRole: false),
            isAttacker: true,
            b);
        ApplyDirectiveEffects(
            ctx,
            ResolveCombatDirective(ctx.Defender, isDefenderRole: true),
            isAttacker: false,
            b);
    }

    private static void ApplyDirectiveEffects(
        BattleEvaluationContext ctx,
        BattleCombatDirective directive,
        bool isAttacker,
        BattleFactorBreakdown b)
    {
        switch (directive)
        {
            // 业务：死守——守方战力 +20%、胜率 +5%，双方伤亡均略增
            case BattleCombatDirective.FightToDeath when !isAttacker:
                b.DefenderPowerScale *= 1.20;
                b.DefenderCasualtyScale *= 1.12;
                b.AttackerCasualtyScale *= 1.05;
                b.DefenderWinRateDelta += 5;
                b.Add("directive.fight_to_death", "死守", 0, 5, "守方");
                break;

            // 业务：坚守——守方战力 +8%、胜率 +2%
            case BattleCombatDirective.HoldLine when !isAttacker:
                b.DefenderPowerScale *= 1.08;
                b.DefenderWinRateDelta += 2;
                b.Add("directive.hold_line", "坚守", 0, 2, "守方");
                break;

            // 业务：迎击——主动方战力 +8%、胜率 +3%
            case BattleCombatDirective.CounterAttack when isAttacker:
                b.AttackerPowerScale *= 1.08;
                b.AttackerWinRateDelta += 3;
                b.Add("directive.counter_attack", "迎击", 3, detail: "攻方");
                break;

            case BattleCombatDirective.CounterAttack when !isAttacker:
                b.DefenderPowerScale *= 1.08;
                b.DefenderWinRateDelta += 3;
                b.Add("directive.counter_attack", "迎击", 0, 3, "守方");
                break;

            // 业务：撤退——胜率 -15%、伤亡 -25%，Commit 阶段禁止强袭
            case BattleCombatDirective.AttemptRetreat:
                if (isAttacker)
                {
                    b.AttackerWinRateDelta -= 15;
                    if (ctx.Phase == BattleEvaluationPhase.Commit)
                        b.BlockCommit = true;
                    b.AttackerCasualtyScale *= 0.75;
                }
                else
                {
                    b.DefenderWinRateDelta -= 15;
                    if (ctx.Phase == BattleEvaluationPhase.Commit)
                        b.BlockCommit = true;
                    b.DefenderCasualtyScale *= 0.75;
                }

                b.Add("directive.retreat", "撤退", isAttacker ? -15 : 0, isAttacker ? 0 : -15);
                break;
        }
    }
}
