using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Actions;

/// <summary>运输 Unit 被截获时的缴获入库（M4-d 掠夺收入）。</summary>
public static class PlunderEconomyActions
{
    /// <summary>将运输 Unit 剩余载货奖给截获方势力最近据点府库。</summary>
    public static void AwardConvoyCargoToForce(
        Unit transport,
        int plunderForceId,
        GameData gameData)
    {
        if (transport.Food <= 0 && transport.Money <= 0)
            return;

        var depot = FindNearestStronghold(plunderForceId, transport.Location, gameData);
        if (depot is null)
            return;

        depot.ForceActor.Food += transport.Food;
        depot.ForceActor.Money += transport.Money;

        if (gameData.Forces.TryGetValue(plunderForceId, out var force))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
    }

    private static Stronghold? FindNearestStronghold(
        int forceId,
        Point3 location,
        GameData gameData)
    {
        Stronghold? best = null;
        var bestDist = int.MaxValue;

        foreach (var sh in gameData.Strongholds.Values)
        {
            if (sh.ForceId != forceId)
                continue;

            var dist = Math.Abs(sh.Location.X - location.X) + Math.Abs(sh.Location.Y - location.Y);
            if (dist >= bestDist)
                continue;

            bestDist = dist;
            best = sh;
        }

        return best;
    }
}
