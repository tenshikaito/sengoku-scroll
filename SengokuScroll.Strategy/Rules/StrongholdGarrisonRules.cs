using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 据点驻军：城内 SubUnit 池与 <see cref="Unit.InStronghold"/> 单位。
/// </summary>
public static class StrongholdGarrisonRules
{
    /// <summary>据点城内农兵池（未编入 SubUnit / Unit 的部分）。</summary>
    public static int GetCityGarrisonSoldiers(Stronghold stronghold)
        => Math.Max(0, stronghold.ForceActor.Soldier);

    /// <summary>城内是否仍有未编入 Unit 的备兵（农兵池 + 空闲 SubUnit）。</summary>
    public static bool HasCityGarrison(Stronghold stronghold, GameData gameData)
        => CountUnassignedGarrisonSoldiers(stronghold, gameData) > 0;

    /// <summary>兼容旧签名：仅看农兵池。</summary>
    public static bool HasCityGarrison(Stronghold stronghold)
        => GetCityGarrisonSoldiers(stronghold) > 0;

    /// <summary>城内驻军是否已溃（无守军且士气归零）。</summary>
    public static bool IsCityGarrisonBroken(Stronghold stronghold, GameData gameData)
        => CountTotalGarrisonAt(stronghold, gameData) <= 0 || stronghold.ForceActor.Morale <= 0;

    /// <summary>兼容旧签名：仅看农兵池与士气。</summary>
    public static bool IsCityGarrisonBroken(Stronghold stronghold)
        => GetCityGarrisonSoldiers(stronghold) <= 0 || stronghold.ForceActor.Morale <= 0;

    public static bool IsInStrongholdAt(Unit unit, Stronghold stronghold)
        => unit.InStronghold
           && unit.LocationStrongholdId == stronghold.Id
           && unit.IsMilitary
           && unit.Soldier > 0;

    /// <summary>是否曾为/现为该城城主方 InStronghold 守军。</summary>
    public static bool WasGarrisonUnit(Unit unit, Stronghold stronghold)
        => unit.ForceId == stronghold.ForceId && IsInStrongholdAt(unit, stronghold);

    /// <summary>支援该据点的部队（InStronghold 协防或 Support 方针指向据点）。</summary>
    public static bool IsReliefSupportUnit(Unit unit, Stronghold stronghold)
        => unit.Directive == UnitDirective.Support
           && (IsInStrongholdAt(unit, stronghold)
               || unit.ActionTarget.StrongholdId == stronghold.Id
               || unit.DirectiveTargetId == stronghold.Id
               || unit.Location.IsAdjacent(stronghold.Location));

    /// <summary>城主方 InStronghold 守军。</summary>
    public static bool IsGarrisonUnit(Unit unit, Stronghold stronghold)
        => unit.ForceId == stronghold.ForceId && IsInStrongholdAt(unit, stronghold);

    /// <summary>查找城主方 InStronghold 军事单位。</summary>
    public static Unit? FindGarrisonUnit(Stronghold stronghold, GameData gameData)
        => gameData.Units.Values.FirstOrDefault(u => IsGarrisonUnit(u, stronghold));

    /// <summary>城主方 InStronghold 或同格地图守军。</summary>
    public static Unit? FindActiveDefenderUnit(Stronghold stronghold, GameData gameData)
        => FindGarrisonUnit(stronghold, gameData)
           ?? gameData.Units.Values.FirstOrDefault(u =>
               UnitStrongholdPresenceRules.IsOwnerMapDefenderOnTile(u, stronghold));

    /// <summary>查找指定势力在该据点的 InStronghold 单位。</summary>
    public static Unit? FindInStrongholdUnit(Stronghold stronghold, int forceId, GameData gameData)
        => gameData.Units.Values.FirstOrDefault(u =>
            u.ForceId == forceId && IsInStrongholdAt(u, stronghold));

    /// <summary>据点是否仍有守军（InStronghold 单位或未编 SubUnit/农兵池）。</summary>
    public static bool IsAnyGarrisonPresent(Stronghold stronghold, GameData gameData)
        => gameData.Units.Values.Any(u => IsInStrongholdAt(u, stronghold))
           || CountUnassignedGarrisonSoldiers(stronghold, gameData) > 0;

    /// <summary>统计据点总守军：所有 InStronghold 单位 + 未编池。</summary>
    public static int CountTotalGarrisonAt(Stronghold stronghold, GameData gameData)
    {
        var inStronghold = gameData.Units.Values
            .Where(u => IsInStrongholdAt(u, stronghold))
            .Sum(u => u.Soldier);
        return inStronghold + CountUnassignedGarrisonSoldiers(stronghold, gameData);
    }

    public static int CountUnassignedGarrisonSoldiers(Stronghold stronghold, GameData gameData)
    {
        var pool = GetCityGarrisonSoldiers(stronghold);
        foreach (var subId in stronghold.ForceActor.SubUnitIds)
        {
            if (gameData.SubUnits.TryGetValue(subId, out var sub) && sub.UnitId == 0)
                pool += Math.Max(0, sub.Soldier);
        }

        return pool;
    }

    /// <summary>守城单位士气/训练同步回据点 ForceActor。</summary>
    public static void SyncCityMoraleFromUnit(Stronghold stronghold, Unit unit)
    {
        stronghold.ForceActor.Morale = unit.Morale;
        stronghold.ForceActor.Training = unit.Training;
    }

    /// <summary>将兵数并入城内农兵池（战后回收等）。</summary>
    public static void AbsorbSoldiersIntoCity(Stronghold stronghold, int soldiers)
    {
        if (soldiers <= 0)
            return;

        stronghold.ForceActor.Soldier += soldiers;
    }
}
