using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Policies.Battle;

/// <summary>单位状态对战斗因子的修正。</summary>
public interface IUnitStatusBattleEffect
{
    UnitStatus Status { get; }

    void Apply(bool isAttacker, BattleFactorBreakdown breakdown);
}

internal sealed class InspiringStatusEffect : IUnitStatusBattleEffect
{
    public static readonly InspiringStatusEffect Instance = new();
    public UnitStatus Status => UnitStatus.Inspiring;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker) b.AttackerWinRateDelta += 8;
        else b.DefenderWinRateDelta += 8;
    }
}

internal sealed class FearfulStatusEffect : IUnitStatusBattleEffect
{
    public static readonly FearfulStatusEffect Instance = new();
    public UnitStatus Status => UnitStatus.Fearful;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker) b.AttackerWinRateDelta -= 12;
        else b.DefenderWinRateDelta -= 12;
    }
}

internal sealed class ChaosStatusEffect : IUnitStatusBattleEffect
{
    public static readonly ChaosStatusEffect Instance = new();
    public UnitStatus Status => UnitStatus.Chaos;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker) b.AttackerPowerScale *= 0.35;
        else b.DefenderPowerScale *= 0.35;
        b.BlockCommit = true;
    }
}

internal sealed class AmbushingStatusEffect : IUnitStatusBattleEffect
{
    public static readonly AmbushingStatusEffect Instance = new();
    public UnitStatus Status => UnitStatus.Ambushing;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (!isAttacker) return;
        b.AttackerWinRateDelta += 18;
        b.AttackerCasualtyScale *= 0.85;
    }
}

internal sealed class BeingSurroundStatusEffect : IUnitStatusBattleEffect
{
    public static readonly BeingSurroundStatusEffect Instance = new();
    public UnitStatus Status => UnitStatus.BeingSurround;

    public void Apply(bool isAttacker, BattleFactorBreakdown b)
    {
        if (isAttacker) return;
        b.DefenderWinRateDelta -= 10;
        b.DefenderCasualtyScale *= 1.15;
    }
}

public static class UnitStatusBattleEffectRegistry
{
    private static readonly Dictionary<UnitStatus, IUnitStatusBattleEffect> ByStatus =
        new IUnitStatusBattleEffect[]
        {
            InspiringStatusEffect.Instance,
            FearfulStatusEffect.Instance,
            ChaosStatusEffect.Instance,
            AmbushingStatusEffect.Instance,
            BeingSurroundStatusEffect.Instance
        }.ToDictionary(e => e.Status);

    public static void Apply(Unit unit, bool isAttacker, BattleFactorBreakdown breakdown)
    {
        if (ByStatus.TryGetValue(unit.Status, out var effect))
            effect.Apply(isAttacker, breakdown);
    }
}
