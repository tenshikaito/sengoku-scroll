using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>部队分兵校验：拆出子编制并在邻格生成新部队。</summary>
public static class UnitSplitRules
{
    public const int MinSoldiersPerSide = 100;

    public static GameResult ValidateSplit(
        Unit parent,
        IReadOnlyList<int> subUnitIds,
        Point3 spawnLocation,
        GameData gameData)
    {
        if (subUnitIds.Count == 0)
            return GameError.DataNotFound;

        if (!parent.IsMilitary || parent.Soldier <= 0)
            return GameError.UnitError.UnitNotFound;

        if (parent.Status is UnitStatus.Chaos or UnitStatus.Standoff or UnitStatus.BeingSurround)
            return GameError.MovementError.CannotMoveToTile;

        if (!parent.Location.IsAdjacent(spawnLocation))
            return GameError.TargetLocationNotAdjacent;

        if (gameData.Units.Values.Any(u =>
                u.IsMilitary && u.Soldier > 0 && u.Location.IsSameTile(spawnLocation)))
            return GameError.MovementError.UnitAlreadyExistsInTile;

        var splitSoldiers = 0;
        foreach (var subId in subUnitIds)
        {
            if (!parent.SubUnitIds.Contains(subId))
                return GameError.DataNotFound;

            if (!gameData.SubUnits.TryGetValue(subId, out var sub) || sub.Soldier <= 0)
                return GameError.DataNotFound;

            splitSoldiers += sub.Soldier;
        }

        if (splitSoldiers < MinSoldiersPerSide)
            return GameError.DataNotFound;

        var remain = parent.Soldier - splitSoldiers;
        if (remain < MinSoldiersPerSide)
            return GameError.DataNotFound;

        return GameResult.Ok();
    }
}
