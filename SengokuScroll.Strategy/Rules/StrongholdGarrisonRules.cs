using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 据点驻军：城内兵数（<see cref="Stronghold.ForceActor"/>.Soldier）与守城方针单位（<see cref="UnitDirective.Support"/>）。
/// </summary>
public static class StrongholdGarrisonRules
{
    /// <summary>据点城内守军兵数（未编入地图单位的部分）。</summary>
    public static int GetCityGarrisonSoldiers(Stronghold stronghold)
        => Math.Max(0, stronghold.ForceActor.Soldier);

    /// <summary>城内是否仍有可作战的守军。</summary>
    public static bool HasCityGarrison(Stronghold stronghold)
        => GetCityGarrisonSoldiers(stronghold) > 0;

    /// <summary>城内驻军是否已溃（兵数或士气归零，无法继续守城）。</summary>
    public static bool IsCityGarrisonBroken(Stronghold stronghold)
        => GetCityGarrisonSoldiers(stronghold) <= 0 || stronghold.ForceActor.Morale <= 0;

    /// <summary>是否为据守该城的守城单位（含已溃但尚未移除的单位）。</summary>
    public static bool WasGarrisonUnit(Unit unit, Stronghold stronghold)
        => unit.ForceId == stronghold.ForceId
           && unit.Directive == UnitDirective.Support
           && unit.Location.IsSameTile(stronghold.Location);

    /// <summary>支援该据点的部队（同格/邻格/目标指向据点）。</summary>
    public static bool IsReliefSupportUnit(Unit unit, Stronghold stronghold)
        => unit.ForceId == stronghold.ForceId
           && unit.Directive == UnitDirective.Support
           && (unit.Location.IsSameTile(stronghold.Location)
               || unit.Location.IsAdjacent(stronghold.Location)
               || unit.ActionTarget.StrongholdId == stronghold.Id
               || unit.DirectiveTargetId == stronghold.Id);

    /// <summary>是否为据守该城的守城单位（普通 Unit，方针 Support，同格）。</summary>
    public static bool IsGarrisonUnit(Unit unit, Stronghold stronghold)
        => WasGarrisonUnit(unit, stronghold)
           && unit.IsMilitary
           && unit.Soldier > 0;

    /// <summary>查找据点上守城方针的军事单位。</summary>
    public static Unit? FindGarrisonUnit(Stronghold stronghold, GameData gameData)
        => gameData.Units.Values.FirstOrDefault(u => IsGarrisonUnit(u, stronghold));

    /// <summary>据点是否仍有守军（城内兵数或守城单位）。</summary>
    public static bool IsAnyGarrisonPresent(Stronghold stronghold, GameData gameData)
        => FindGarrisonUnit(stronghold, gameData) is not null || HasCityGarrison(stronghold);

    /// <summary>统计据点总守军：守城单位兵数 + 城内兵数。</summary>
    public static int CountTotalGarrisonAt(Stronghold stronghold, GameData gameData)
    {
        var onTile = FindGarrisonUnit(stronghold, gameData)?.Soldier ?? 0;
        return onTile + GetCityGarrisonSoldiers(stronghold);
    }

    /// <summary>守城单位士气/训练同步回据点 ForceActor。</summary>
    public static void SyncCityMoraleFromUnit(Stronghold stronghold, Unit unit)
    {
        stronghold.ForceActor.Morale = unit.Morale;
        stronghold.ForceActor.Training = unit.Training;
    }

    /// <summary>将兵数并入城内驻军（如剧本加载、战后回收等）。</summary>
    public static void AbsorbSoldiersIntoCity(Stronghold stronghold, int soldiers)
    {
        if (soldiers <= 0)
            return;

        stronghold.ForceActor.Soldier += soldiers;
    }
}
