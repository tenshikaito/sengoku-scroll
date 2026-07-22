using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Policies.Battle;

/// <summary>单位方针对战斗因子的修正（非决战方针推断）。</summary>
public interface IUnitDirectiveBattleEffect
{
    UnitDirective Directive { get; }

    void Apply(bool isAttacker, BattleFactorBreakdown breakdown);
}

internal sealed class RetreatDirectiveBattleEffect : IUnitDirectiveBattleEffect
{
    public static readonly RetreatDirectiveBattleEffect Instance = new();
    public UnitDirective Directive => UnitDirective.Retreat;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
        => b.BlockCommit = true;
}

internal sealed class RaidDirectiveBattleEffect : IUnitDirectiveBattleEffect
{
    public static readonly RaidDirectiveBattleEffect Instance = new();
    public UnitDirective Directive => UnitDirective.Raid;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker)
            b.AttackerCasualtyScale *= 1.08;
    }
}

public static class UnitDirectiveBattleEffectRegistry
{
    private static readonly Dictionary<UnitDirective, IUnitDirectiveBattleEffect> ByDirective =
        new IUnitDirectiveBattleEffect[]
        {
            RetreatDirectiveBattleEffect.Instance,
            RaidDirectiveBattleEffect.Instance
        }.ToDictionary(e => e.Directive);

    public static void Apply(Unit unit, bool isAttacker, BattleFactorBreakdown breakdown)
    {
        if (ByDirective.TryGetValue(unit.Directive, out var effect))
            effect.Apply(isAttacker, breakdown);
    }
}
