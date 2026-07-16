using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>部队合并校验：同势力、相邻或同格、可作战状态。</summary>
public static class UnitMergeRules
{
    public static GameResult ValidateMerge(Unit source, Unit target, GameData gameData)
    {
        if (source.Id == target.Id)
            return GameError.DataNotFound;

        if (source.ForceId != target.ForceId)
            return GameError.DiplomacyError.NotSelfForce;

        if (!source.IsMilitary || !target.IsMilitary)
            return GameError.DataNotFound;

        if (source.Soldier <= 0 || target.Soldier <= 0)
            return GameError.DataNotFound;

        if (!CanMergeInCurrentState(source) || !CanMergeInCurrentState(target))
            return GameError.MovementError.CannotMoveToTile;

        if (!IsSameTileOrAdjacent(source.Location, target.Location))
            return GameError.TargetLocationNotAdjacent;

        return GameResult.Ok();
    }

    private static bool CanMergeInCurrentState(Unit unit)
        => unit.Status is not (UnitStatus.Chaos or UnitStatus.Standoff or UnitStatus.BeingSurround)
           && unit.Stance is not UnitStance.Attacking
           && unit.ActionTarget.UnitId <= 0;

    private static bool IsSameTileOrAdjacent(Point3 a, Point3 b)
        => a.IsSameTile(b) || a.IsAdjacent(b);
}
