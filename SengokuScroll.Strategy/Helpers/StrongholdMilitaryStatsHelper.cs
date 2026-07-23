using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Constants;
using SengokuScroll.Strategy.Rules;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>据点驻军统计：农兵池、驻城 SubUnit、总兵力缓存。</summary>
public static class StrongholdMilitaryStatsHelper
{
    /// <summary>农兵池（ForceActor.Soldier）。</summary>
    public static int GetMilitiaSoldiers(Stronghold stronghold)
        => Math.Max(0, stronghold.ForceActor.Soldier);

    /// <summary>驻城 SubUnit 可用兵力（不含伤兵 Patient）。</summary>
    public static int CalculateProfessionalGarrisonSoldiers(Stronghold stronghold, GameData gameData)
    {
        var total = 0;
        foreach (var subId in stronghold.ForceActor.SubUnitIds)
        {
            if (!gameData.SubUnits.TryGetValue(subId, out var sub)
                || sub.UnitId != 0
                || sub.Soldier <= 0)
                continue;

            total += Math.Max(0, sub.Soldier);
        }

        return total;
    }

    /// <summary>同步 ForceActor 缓存字段。</summary>
    public static void Recalculate(Stronghold stronghold, GameData gameData)
    {
        var militia = GetMilitiaSoldiers(stronghold);
        var garrison = CalculateProfessionalGarrisonSoldiers(stronghold, gameData);
        stronghold.ForceActor.GarrisonSoldiers = garrison;
        stronghold.ForceActor.TotalSoldiers = militia + garrison;
    }

    /// <summary>情报/显示用总驻军（专业队 + 农兵池）。</summary>
    public static int GetTotalAvailableSoldiers(Stronghold stronghold, GameData gameData)
    {
        Recalculate(stronghold, gameData);
        return stronghold.ForceActor.TotalSoldiers;
    }
}
