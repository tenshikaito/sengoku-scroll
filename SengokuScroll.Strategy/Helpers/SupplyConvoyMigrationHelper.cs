using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Domain.World;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>将旧 <see cref="SupplyConvoy"/> 条目迁移为运输 Unit（剧本加载时一次性）。</summary>
public static class SupplyConvoyMigrationHelper
{
    public static void MigrateToUnits(GameWorld world)
    {
        var gameData = world.GameData;
        if (gameData.SupplyConvoys.Count == 0)
            return;

        foreach (var convoy in gameData.SupplyConvoys.Values.ToList())
        {
            if (gameData.Units.ContainsKey(convoy.Id))
                continue;

            var unit = ConvertConvoy(convoy);
            gameData.Units[unit.Id] = unit;
            MapLocationActions.RegisterUnit(world, unit);
        }

        gameData.SupplyConvoys.Clear();
    }

    private static Unit ConvertConvoy(SupplyConvoy convoy)
    {
        var route = new Queue<Point2>();
        foreach (var point in convoy.RoutePoints)
            route.Enqueue(new Point2(point.X, point.Y));

        var status = convoy.Status switch
        {
            SupplyConvoyStatus.Arrived => UnitStatus.Waiting,
            SupplyConvoyStatus.Destroyed => UnitStatus.Chaos,
            _ => UnitStatus.Moving
        };

        return new Unit
        {
            Id = convoy.Id,
            Name = convoy.Name,
            ForceId = convoy.ForceId,
            LeaderId = convoy.LeaderId,
            Location = convoy.Location,
            Soldier = convoy.PorterCount + convoy.EscortSoldierCount,
            Food = convoy.CargoFoodGo,
            Money = convoy.CargoMoney,
            Morale = 70,
            Training = 50,
            Movement = convoy.Movement > 0 ? convoy.Movement : LogisticsConstants.ConvoyDailyAp,
            Ap = convoy.Ap,
            IsMilitary = false,
            Kind = convoy.Purpose switch
            {
                TransportPurpose.Trade => UnitKind.Merchant,
                TransportPurpose.Migrant => UnitKind.Migrant,
                _ => UnitKind.Convoy
            },
            InStronghold = false,
            HomeStrongholdId = convoy.OriginStrongholdId,
            LocationStrongholdId = 0,
            Directive = UnitDirective.Move,
            Status = status,
            TransportPurpose = convoy.Purpose,
            TransportOriginStrongholdId = convoy.OriginStrongholdId,
            TransportTargetUnitId = convoy.TargetUnitId,
            TransportTargetStrongholdId = convoy.TargetStrongholdId,
            CargoPopulation = convoy.CargoPopulation,
            PorterCount = convoy.PorterCount,
            EscortSoldierCount = convoy.EscortSoldierCount,
            IsReturningToOrigin = convoy.IsReturningToOrigin,
            IsDeceived = convoy.IsDeceived,
            DeceivedHoldDaysRemaining = convoy.DeceivedHoldDaysRemaining,
            ActionTarget = new UnitActionTarget
            {
                ForceId = convoy.ForceId,
                StrongholdId = convoy.TargetStrongholdId,
                UnitId = convoy.TargetUnitId,
                RoutePoints = route
            },
            SubUnitIds = []
        };
    }
}
