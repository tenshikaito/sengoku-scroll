using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Policies.Battle;

/// <summary>接敌类型对战斗因子的修正。</summary>
public interface IEngagementKindEffect
{
    BattleEngagementKind Kind { get; }

    void Apply(BattleFactorBreakdown breakdown, Unit? defender, GameData? gameData);
}

internal sealed class FieldBattleEngagementEffect : IEngagementKindEffect
{
    public static readonly FieldBattleEngagementEffect Instance = new();
    public BattleEngagementKind Kind => BattleEngagementKind.FieldBattle;
    public void Apply(BattleFactorBreakdown breakdown, Unit? defender, GameData? gameData) { }
}

internal sealed class AmbushEngagementEffect : IEngagementKindEffect
{
    public static readonly AmbushEngagementEffect Instance = new();
    public BattleEngagementKind Kind => BattleEngagementKind.Ambush;

    public void Apply(BattleFactorBreakdown breakdown, Unit? defender, GameData? gameData)
        => breakdown.Add("engagement.ambush", "伏击战", 6, detail: "接敌类型");
}

internal sealed class SiegeEngagementEffect : IEngagementKindEffect
{
    public static readonly SiegeEngagementEffect Instance = new();
    public BattleEngagementKind Kind => BattleEngagementKind.Siege;

    public void Apply(BattleFactorBreakdown breakdown, Unit? defender, GameData? gameData)
    {
        breakdown.DefenderPowerScale *= 1.12;
        breakdown.DefenderWinRateDelta += 6;
        breakdown.Add("engagement.siege", "攻城战", 0, 6, "守方依托城寨");

        if (defender is not null
            && gameData is not null
            && SiegeBattleRules.ResolveDefenderStronghold(defender, gameData) is { } stronghold)
            SiegeBattleRules.ApplySiegeStrongholdFactors(stronghold, breakdown);
    }
}

public static class EngagementKindEffectRegistry
{
    private static readonly IEngagementKindEffect[] All =
    [
        FieldBattleEngagementEffect.Instance,
        AmbushEngagementEffect.Instance,
        SiegeEngagementEffect.Instance
    ];

    private static readonly Dictionary<BattleEngagementKind, IEngagementKindEffect> ByKind =
        All.ToDictionary(e => e.Kind);

    public static void Apply(
        BattleEngagementKind kind,
        BattleFactorBreakdown breakdown,
        Unit? defender = null,
        GameData? gameData = null)
    {
        if (ByKind.TryGetValue(kind, out var effect))
            effect.Apply(breakdown, defender, gameData);
    }
}
