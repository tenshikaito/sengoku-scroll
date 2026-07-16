using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Localization;
using SengokuScroll.Localization.Abstractions;
using SengokuScroll.Strategy.Rules;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Battle;

/// <summary>自动战斗接敌类型（野战 / 伏击 / 攻城）。</summary>
public enum BattleEngagementKind
{
    /// <summary>野战——开阔地正面交锋。</summary>
    FieldBattle,
    /// <summary>伏击——埋伏状态触发，攻方占优。</summary>
    Ambush,
    /// <summary>攻城——守方依托城寨防御。</summary>
    Siege
}

/// <summary>判定接敌类型；攻城/伏击修正系数，完整攻城占领待扩展。</summary>
public static class BattleEngagementClassifier
{
    /// <summary>根据单位状态与位置判定接敌类型（伏击优先于攻城）。</summary>
    public static BattleEngagementKind Classify(Unit attacker, Unit defender, GameData gameData)
    {
        if (attacker.Status == UnitStatus.Ambushing
            || defender.Status == UnitStatus.Ambushing)
            return BattleEngagementKind.Ambush;

        if (SiegeBattleRules.IsSiegeEngagement(attacker, defender, gameData))
            return BattleEngagementKind.Siege;

        // 业务：守军据守城格、攻方在邻格持占领/劫掠方针 → 城下攻城（非普通野战）
        if (SiegeBattleRules.IsStrongholdGarrison(defender, gameData)
            && attacker.Directive is UnitDirective.Occupy or UnitDirective.Raid
            && attacker.Location.IsAdjacent(defender.Location)
            && SiegeBattleRules.ResolveDefenderStronghold(defender, gameData) is not null)
            return BattleEngagementKind.Siege;

        return BattleEngagementKind.FieldBattle;
    }

    /// <summary>按接敌类型施加攻方胜率与守方战力修正（攻城叠加城寨因素）。</summary>
    public static void ApplyEngagementKind(
        BattleEngagementKind kind,
        BattleFactorBreakdown b,
        Unit? defender = null,
        GameData? gameData = null)
    {
        switch (kind)
        {
            case BattleEngagementKind.Ambush:
                // 业务：伏击战攻方胜率 +6%
                b.Add("engagement.ambush", "伏击战", 6, detail: "接敌类型");
                break;
            case BattleEngagementKind.Siege:
                // 业务：攻城战守方战力 ×1.12、胜率 +6%，再叠加城寨防御
                b.DefenderPowerScale *= 1.12;
                b.DefenderWinRateDelta += 6;
                b.Add("engagement.siege", "攻城战", 0, 6, "守方依托城寨");

                if (defender is not null && gameData is not null
                    && SiegeBattleRules.ResolveDefenderStronghold(defender, gameData) is { } stronghold)
                    SiegeBattleRules.ApplySiegeStrongholdFactors(stronghold, b);
                break;
        }
    }

    /// <summary>接敌类型的显示标签（默认中文硬编码，兼容旧调用）。</summary>
    public static string ToDisplayLabel(BattleEngagementKind kind) => kind switch
    {
        BattleEngagementKind.Ambush => "伏击",
        BattleEngagementKind.Siege => "攻城战",
        _ => "野战"
    };

    /// <summary>接敌类型的本地化显示标签。</summary>
    public static string ToDisplayLabel(BattleEngagementKind kind, ITextLocalizer localizer) => kind switch
    {
        BattleEngagementKind.Ambush => localizer.GetString(LocalizationKeys.Battle.EngagementAmbush),
        BattleEngagementKind.Siege => localizer.GetString(LocalizationKeys.Battle.EngagementSiege),
        _ => localizer.GetString(LocalizationKeys.Battle.EngagementField)
    };
}
