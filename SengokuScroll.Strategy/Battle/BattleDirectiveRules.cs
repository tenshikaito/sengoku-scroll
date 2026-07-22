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
        => Policies.Battle.BattleCombatDirectiveEffectRegistry.Apply(ctx, directive, isAttacker, b);
}
