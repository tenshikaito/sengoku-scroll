using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>强攻日更：攻方伤亡、城防折损（与城防值挂钩）。</summary>
public static class SiegeAssaultRules
{
    /// <summary>攻方日伤亡率基点：基础 0.8% + 城防每点 +0.06%（上限约 7%）。</summary>
    public static int CalculateDailyAttackerCasualties(Unit attacker, int totalDefense)
    {
        if (attacker.Soldier <= 0)
            return 0;

        var rateBp = 80 + Math.Min(620, Math.Max(0, totalDefense) * 6);
        return Math.Max(1, (int)Math.Ceiling(attacker.Soldier * rateBp / 10000.0));
    }

    /// <summary>强攻累计若干日后是否应损毁一项城防设施。</summary>
    public static bool ShouldWearDefenseFacilityToday(int assaultDays, int totalDefense, int totalAttackerSoldiers)
    {
        if (assaultDays <= 0 || totalDefense <= 0 || totalAttackerSoldiers < 150)
            return false;

        var interval = Math.Max(2, 14 - totalDefense / 12);
        return assaultDays % interval == 0;
    }

    /// <summary>对同据点所有强攻单位施加伤亡并写回。</summary>
    public static void ApplyAttackerDailyCasualties(
        IReadOnlyList<Unit> assaultUnits,
        Stronghold target,
        GameMasterData masterData)
    {
        var totalDefense = StrongholdDefenseRules.ResolveTotalDefense(target, masterData);

        foreach (var attacker in assaultUnits)
        {
            if (attacker.Soldier <= 0)
                continue;

            var loss = CalculateDailyAttackerCasualties(attacker, totalDefense);
            attacker.Soldier = Math.Max(0, attacker.Soldier - loss);
            if (attacker.Soldier == 0)
                attacker.Status = UnitStatus.Chaos;
        }
    }
}
