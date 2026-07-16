using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Helpers;

namespace SengokuScroll.Strategy.Actions;

/// <summary>运输队被截获时的缴获入库（M4-d 掠夺收入）。</summary>
public static class PlunderEconomyActions
{
    /// <summary>将运输队剩余载货奖给截获方势力最近据点府库。</summary>
    public static void AwardConvoyCargoToForce(
        SupplyConvoy convoy,
        int plunderForceId,
        GameData gameData)
    {
        if (convoy.CargoFoodGo <= 0 && convoy.CargoMoney <= 0)
            return;

        var depot = FindNearestStronghold(plunderForceId, convoy.Location, gameData);
        if (depot is null)
            return;

        depot.ForceActor.Food += convoy.CargoFoodGo;
        depot.ForceActor.Money += convoy.CargoMoney;

        if (gameData.Forces.TryGetValue(plunderForceId, out var force))
            ForceEconomyActions.SyncForceTreasuryFromStrongholds(force, gameData);
    }

    private static Stronghold? FindNearestStronghold(
        int forceId,
        Common.Types.Point3 location,
        GameData gameData)
    {
        Stronghold? best = null;
        var bestDist = int.MaxValue;

        // 业务：同势力据点中曼哈顿距离最近者作为缴获入库点
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
