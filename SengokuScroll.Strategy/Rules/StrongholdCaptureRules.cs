using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>
/// 据点归属变更：须持攻城指令且守军（城内兵/守城单位）兵数或士气已无法继续抵抗。
/// </summary>
public static class StrongholdCaptureRules
{
    /// <summary>是否已对目标据点下达攻城指令（强攻 / 包围）。</summary>
    public static bool HasActiveSiegeOrder(Unit captor, Stronghold stronghold)
        => captor.SiegeMode != UnitSiegeMode.None
           && captor.ActionTarget.StrongholdId == stronghold.Id;

    /// <summary>据点守备是否已崩溃：城内兵=0 或士气=0，且无作战中的守城地图单位。</summary>
    public static bool IsStrongholdDefenseBroken(Stronghold stronghold, GameData gameData)
    {
        if (StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData) is not null)
            return false;

        foreach (var unit in gameData.Units.Values)
        {
            if (unit.ForceId != stronghold.ForceId || !unit.IsMilitary || unit.Soldier <= 0)
                continue;

            if (!unit.Location.IsSameTile(stronghold.Location))
                continue;

            if (StrongholdGarrisonRules.WasGarrisonUnit(unit, stronghold))
                return false;
        }

        return StrongholdGarrisonRules.IsCityGarrisonBroken(stronghold);
    }

    /// <summary>攻方位置是否满足当前攻城模式（强攻须在同格，包围须在相邻格）。</summary>
    public static bool IsCaptorInValidSiegePosition(Unit captor, Stronghold stronghold)
    {
        return captor.SiegeMode switch
        {
            UnitSiegeMode.Assault => captor.Location.IsSameTile(stronghold.Location),
            UnitSiegeMode.Encircle => captor.Location.IsSameTile(stronghold.Location)
                                      || captor.Location.IsAdjacent(stronghold.Location),
            _ => false
        };
    }

    /// <summary>是否允许变更据点归属。</summary>
    public static bool CanTransferOwnership(
        Unit captor,
        Stronghold stronghold,
        GameData gameData,
        out string rejectReason)
    {
        rejectReason = string.Empty;

        if (stronghold.ForceId == captor.ForceId)
        {
            rejectReason = "already_owned";
            return false;
        }

        if (!captor.IsMilitary || captor.Soldier <= 0)
        {
            rejectReason = "captor_invalid";
            return false;
        }

        if (!HasActiveSiegeOrder(captor, stronghold))
        {
            rejectReason = "no_siege_order";
            return false;
        }

        if (!IsCaptorInValidSiegePosition(captor, stronghold))
        {
            rejectReason = "invalid_siege_position";
            return false;
        }

        if (!IsStrongholdDefenseBroken(stronghold, gameData))
        {
            rejectReason = "defense_intact";
            return false;
        }

        return true;
    }

    /// <summary>占城诊断快照（调试 / 事件日志）。</summary>
    public static string DescribeDefenseState(Stronghold stronghold, GameData gameData)
    {
        var garrison = StrongholdGarrisonRules.FindGarrisonUnit(stronghold, gameData);
        var city = StrongholdGarrisonRules.GetCityGarrisonSoldiers(stronghold);
        var total = StrongholdGarrisonRules.CountTotalGarrisonAt(stronghold, gameData);
        return $"total={total} city={city} cityMorale={stronghold.ForceActor.Morale} " +
               $"garrisonUnit={(garrison?.Soldier.ToString() ?? "none")} " +
               $"defenseBroken={IsStrongholdDefenseBroken(stronghold, gameData)}";
    }
}
