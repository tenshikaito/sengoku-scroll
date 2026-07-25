using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Extensions;
using SengokuScroll.Domain.Rules;
using SengokuScroll.Strategy.Data.Models;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Rules;

/// <summary>Unit 入城/出城/解散规则。</summary>
public static class UnitStrongholdPresenceRules
{
    public static bool CanEnterStronghold(Unit unit, Stronghold stronghold, GameData gameData)
    {
        if (unit.InStronghold || unit.Soldier <= 0)
            return false;

        if (!unit.Location.IsSameTile(stronghold.Location))
            return false;

        if (unit.ForceId == stronghold.ForceId)
            return true;

        if (!gameData.Forces.TryGetValue(unit.ForceId, out var unitForce)
            || !gameData.Forces.TryGetValue(stronghold.ForceId, out var holderForce))
        {
            return false;
        }

        return DiplomacyRules.IsAlly(unitForce, holderForce).IsSuccess;
    }

    public static bool CanExitStronghold(Unit unit, Stronghold stronghold)
        => unit.InStronghold
           && unit.LocationStrongholdId == stronghold.Id
           && unit.Soldier > 0;

    public static bool CanOrganizationalDisband(Unit unit, GameData gameData)
    {
        if (unit.HomeStrongholdId <= 0)
            return false;

        if (!unit.InStronghold || unit.LocationStrongholdId != unit.HomeStrongholdId)
            return false;

        return gameData.Strongholds.TryGetValue(unit.HomeStrongholdId, out var home)
               && home.ForceId == unit.ForceId;
    }

    public static bool IsOwnerMapDefenderOnTile(Unit unit, Stronghold stronghold)
        => unit.ForceId == stronghold.ForceId
           && !unit.InStronghold
           && unit.IsMilitary
           && unit.Soldier > 0
           && unit.Location.IsSameTile(stronghold.Location);
}
