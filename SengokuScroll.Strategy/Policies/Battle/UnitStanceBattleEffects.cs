using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Policies.Battle;

/// <summary>单位姿态对战斗因子的修正。</summary>
public interface IUnitStanceBattleEffect
{
    UnitStance Stance { get; }

    void Apply(bool isAttacker, BattleFactorBreakdown breakdown);
}

internal sealed class AttackingStanceEffect : IUnitStanceBattleEffect
{
    public static readonly AttackingStanceEffect Instance = new();
    public UnitStance Stance => UnitStance.Attacking;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker)
            b.AttackerPowerScale *= 1.05;
    }
}

internal sealed class HoldStanceEffect : IUnitStanceBattleEffect
{
    public static readonly HoldStanceEffect Instance = new();
    public UnitStance Stance => UnitStance.Hold;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (!isAttacker)
        {
            b.DefenderPowerScale *= 1.12;
            b.DefenderWinRateDelta += 4;
        }
    }
}

internal sealed class AlertStanceEffect : IUnitStanceBattleEffect
{
    public static readonly AlertStanceEffect Instance = new();
    public UnitStance Stance => UnitStance.Alert;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (!isAttacker)
            b.DefenderWinRateDelta += 2;
    }
}

internal sealed class ManeuverStanceEffect : IUnitStanceBattleEffect
{
    public static readonly ManeuverStanceEffect Instance = new();
    public UnitStance Stance => UnitStance.Maneuver;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker)
            b.AttackerWinRateDelta += 2;
    }
}

internal sealed class SurroundingStanceEffect : IUnitStanceBattleEffect
{
    public static readonly SurroundingStanceEffect Instance = new();
    public UnitStance Stance => UnitStance.Surrounding;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker)
        {
            b.DefenderWinRateDelta -= 6;
            b.DefenderCasualtyScale *= 1.1;
        }
    }
}

public static class UnitStanceBattleEffectRegistry
{
    private static readonly Dictionary<UnitStance, IUnitStanceBattleEffect> ByStance =
        new IUnitStanceBattleEffect[]
        {
            AttackingStanceEffect.Instance,
            HoldStanceEffect.Instance,
            AlertStanceEffect.Instance,
            ManeuverStanceEffect.Instance,
            SurroundingStanceEffect.Instance
        }.ToDictionary(e => e.Stance);

    public static void Apply(Unit unit, bool isAttacker, BattleFactorBreakdown breakdown)
    {
        if (ByStance.TryGetValue(unit.Stance, out var effect))
            effect.Apply(isAttacker, breakdown);
    }
}
