using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Policies.UnitAi;

/// <summary>判定单位是否跳过每日 AI，并给出原因文案。</summary>
public interface IUnitAiSkipBehavior
{
    bool AppliesTo(Unit unit);

    string Reason { get; }
}

internal sealed class NonMilitaryUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly NonMilitaryUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => !unit.IsMilitary;

    public string Reason => "非军事单位";
}

internal sealed class ZeroSoldiersUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly ZeroSoldiersUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => unit.Soldier <= 0;

    public string Reason => "兵力为 0";
}

internal sealed class AttackingStanceUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly AttackingStanceUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => unit.Stance == UnitStance.Attacking;

    public string Reason => "已在攻击姿态，本日由战斗系统处理";
}

internal sealed class ChaosStatusUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly ChaosStatusUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => unit.Status == UnitStatus.Chaos;

    public string Reason => "混乱中无法行动";
}

internal sealed class BeingSurroundStatusUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly BeingSurroundStatusUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => unit.Status == UnitStatus.BeingSurround;

    public string Reason => "被包围中无法行动";
}

internal sealed class StandoffStatusUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly StandoffStatusUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => unit.Status == UnitStatus.Standoff;

    public string Reason => "战场对峙中无法行动";
}

internal sealed class SiegeLockedUnitAiSkipBehavior : IUnitAiSkipBehavior
{
    public static readonly SiegeLockedUnitAiSkipBehavior Instance = new();

    public bool AppliesTo(Unit unit) => SiegeOrderRules.IsSiegeMovementLocked(unit);

    public string Reason => "攻城令期间不可另行移动";
}

public static class UnitAiSkipBehaviorRegistry
{
    private static readonly IUnitAiSkipBehavior[] Behaviors =
    [
        NonMilitaryUnitAiSkipBehavior.Instance,
        ZeroSoldiersUnitAiSkipBehavior.Instance,
        AttackingStanceUnitAiSkipBehavior.Instance,
        ChaosStatusUnitAiSkipBehavior.Instance,
        BeingSurroundStatusUnitAiSkipBehavior.Instance,
        StandoffStatusUnitAiSkipBehavior.Instance,
        SiegeLockedUnitAiSkipBehavior.Instance,
    ];

    public static bool ShouldSkipDailyAi(Unit unit)
        => Behaviors.Any(b => b.AppliesTo(unit));

    public static string? DescribeSkipReason(Unit unit)
        => Behaviors.FirstOrDefault(b => b.AppliesTo(unit))?.Reason;
}
