namespace SengokuScroll.Strategy.Policies.Battle;

using SengokuScroll.Strategy.Battle;

/// <summary>决战战斗方针对战力/胜率/伤亡的修正。</summary>
public interface IBattleCombatDirectiveEffect
{
    BattleCombatDirective Directive { get; }

    void Apply(BattleEvaluationContext ctx, bool isAttacker, BattleFactorBreakdown breakdown);
}

internal sealed class FightToDeathDirectiveEffect : IBattleCombatDirectiveEffect
{
    public static readonly FightToDeathDirectiveEffect Instance = new();
    public BattleCombatDirective Directive => BattleCombatDirective.FightToDeath;

    public void Apply(BattleEvaluationContext ctx, bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker) return;
        b.DefenderPowerScale *= 1.20;
        b.DefenderCasualtyScale *= 1.12;
        b.AttackerCasualtyScale *= 1.05;
        b.DefenderWinRateDelta += 5;
        b.Add("directive.fight_to_death", "死守", 0, 5, "守方");
    }
}

internal sealed class HoldLineDirectiveEffect : IBattleCombatDirectiveEffect
{
    public static readonly HoldLineDirectiveEffect Instance = new();
    public BattleCombatDirective Directive => BattleCombatDirective.HoldLine;

    public void Apply(BattleEvaluationContext ctx, bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker) return;
        b.DefenderPowerScale *= 1.08;
        b.DefenderWinRateDelta += 2;
        b.Add("directive.hold_line", "坚守", 0, 2, "守方");
    }
}

internal sealed class CounterAttackDirectiveEffect : IBattleCombatDirectiveEffect
{
    public static readonly CounterAttackDirectiveEffect Instance = new();
    public BattleCombatDirective Directive => BattleCombatDirective.CounterAttack;

    public void Apply(BattleEvaluationContext ctx, bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker)
        {
            b.AttackerPowerScale *= 1.08;
            b.AttackerWinRateDelta += 3;
            b.Add("directive.counter_attack", "迎击", 3, detail: "攻方");
            return;
        }

        b.DefenderPowerScale *= 1.08;
        b.DefenderWinRateDelta += 3;
        b.Add("directive.counter_attack", "迎击", 0, 3, "守方");
    }
}

internal sealed class AttemptRetreatDirectiveEffect : IBattleCombatDirectiveEffect
{
    public static readonly AttemptRetreatDirectiveEffect Instance = new();
    public BattleCombatDirective Directive => BattleCombatDirective.AttemptRetreat;

    public void Apply(BattleEvaluationContext ctx, bool isAttacker, BattleFactorBreakdown b)
    {
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
    }
}

public static class BattleCombatDirectiveEffectRegistry
{
    private static readonly IBattleCombatDirectiveEffect[] All =
    [
        FightToDeathDirectiveEffect.Instance,
        HoldLineDirectiveEffect.Instance,
        CounterAttackDirectiveEffect.Instance,
        AttemptRetreatDirectiveEffect.Instance
    ];

    private static readonly Dictionary<BattleCombatDirective, IBattleCombatDirectiveEffect> ByDirective =
        All.ToDictionary(e => e.Directive);

    public static void Apply(
        BattleEvaluationContext ctx,
        BattleCombatDirective directive,
        bool isAttacker,
        BattleFactorBreakdown breakdown)
    {
        if (ByDirective.TryGetValue(directive, out var effect))
            effect.Apply(ctx, isAttacker, breakdown);
    }
}
