using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Strategy.Battle;
using SengokuScroll.Strategy.Constants;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 攻城战：攻方进入守方据点格后与城内驻军接敌；
/// 守军在己方据点格上、敌从城外相邻进攻时为城野战（不算攻城）。
/// </summary>
public static class SiegeBattleRules
{
    /// <summary>守军是否与己方据点同格。</summary>
    public static Stronghold? ResolveDefenderStronghold(Unit defender, GameData gameData)
    {
        foreach (var stronghold in gameData.Strongholds.Values)
        {
            if (stronghold.ForceId != defender.ForceId)
                continue;

            if (stronghold.Location.X == defender.Location.X
                && stronghold.Location.Y == defender.Location.Y)
                return stronghold;
        }

        return null;
    }

    /// <summary>守军是否依托己方据点格（含城下野战上下文）。</summary>
    public static bool IsStrongholdGarrison(Unit defender, GameData gameData)
        => ResolveDefenderStronghold(defender, gameData) is not null;

    /// <summary>攻方已进入守方据点格，且守军据守该据点 → 攻城战。</summary>
    public static bool IsSiegeEngagement(Unit attacker, Unit defender, GameData gameData)
    {
        var stronghold = ResolveDefenderStronghold(defender, gameData);
        if (stronghold is null)
            return false;

        return attacker.Location.IsSameTile(stronghold.Location);
    }

    /// <summary>驻军是否已无法继续守城（兵数或士气归零）。</summary>
    public static bool IsGarrisonBroken(Unit defender)
        => defender.Soldier <= 0 || defender.Morale <= 0;

    /// <summary>攻城时用于「大军对峙」阈值的有效兵数（含城防折算）。</summary>
    public static int EffectiveSiegeSoldierCount(Unit attacker, Unit defender, GameData gameData)
    {
        if (!IsSiegeEngagement(attacker, defender, gameData))
            return defender.Soldier;

        var stronghold = ResolveDefenderStronghold(defender, gameData);
        if (stronghold is null)
            return defender.Soldier;

        // 业务：城防值每点折算 30 名等效守军，用于大军对峙与决战判定
        var defenseBonus = stronghold.Defense * 30;
        return defender.Soldier + defenseBonus;
    }

    /// <summary>将据点城防加成写入战斗因素分解（守方战力与胜率修正）。</summary>
    public static void ApplySiegeStrongholdFactors(Stronghold stronghold, BattleFactorBreakdown breakdown)
    {
        var defense = stronghold.Defense;
        // 业务：城防越高，守方胜率加成 2–12 点，战力按 defense/150 比例放大
        var winDelta = Math.Clamp(defense / 8, 2, 12);
        var powerScale = 1.0 + defense / 150.0;

        breakdown.DefenderPowerScale *= powerScale;
        breakdown.DefenderWinRateDelta += winDelta;
        breakdown.Add("siege.stronghold_defense", $"城防{defense}", 0, winDelta, stronghold.Name);
    }

    /// <summary>攻城决战：守方依托城寨时更难被一方拖入不对等强袭。</summary>
    public static bool ShouldPreferSiegeStandoff(
        Unit attacker,
        Unit defender,
        GameData gameData,
        int standoffDays)
    {
        if (!IsSiegeEngagement(attacker, defender, gameData))
            return false;

        var combined = attacker.Soldier + EffectiveSiegeSoldierCount(attacker, defender, gameData);
        // 业务：双方合计未达「大军」阈值时不适用攻城对峙拖延
        if (combined < BattleConstants.LargeArmySoldierThreshold)
            return false;

        // 业务：大军攻城在对峙天数未达强制决战上限前，优先维持对峙而非强袭
        return standoffDays < BattleConstants.StandoffForceBattleDays;
    }
}
