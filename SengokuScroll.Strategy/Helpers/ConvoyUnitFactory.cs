using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>创建并登记地图运输 Unit（Convoy/Migrant/Merchant 在途）。</summary>
public static class ConvoyUnitFactory
{
    public static Unit CreateTransportUnit(
        GameWorld world,
        string name,
        int forceId,
        int leaderId,
        Point3 location,
        int originStrongholdId,
        int targetUnitId,
        int targetStrongholdId,
        int foodCargo,
        int moneyCargo,
        int cargoPopulation,
        TransportPurpose purpose,
        Queue<Point2> routePoints,
        UnitKind? kindOverride = null)
    {
        var gameData = world.GameData;
        var unitId = NextUnitId(gameData);
        var kind = kindOverride ?? ResolveKind(purpose);
        var porterCount = purpose == TransportPurpose.Migrant
            ? LogisticsConstants.DefaultPorterCount
            : LogisticsConstants.DefaultPorterCount;
        var escortCount = purpose == TransportPurpose.Migrant
            ? 0
            : LogisticsConstants.DefaultEscortSoldierCount;

        var unit = new Unit
        {
            Id = unitId,
            Name = name,
            ForceId = forceId,
            LeaderId = leaderId,
            Location = location,
            Soldier = porterCount + escortCount,
            Food = foodCargo,
            Money = moneyCargo,
            Morale = (byte)(purpose == TransportPurpose.Migrant ? 60 : 70),
            Training = 50,
            Movement = LogisticsConstants.ConvoyDailyAp,
            Ap = 0,
            IsReadyToMove = false,
            IsMilitary = false,
            Kind = kind,
            InStronghold = false,
            HomeStrongholdId = originStrongholdId,
            LocationStrongholdId = 0,
            Directive = UnitDirective.Move,
            Status = UnitStatus.Moving,
            TransportPurpose = purpose,
            TransportOriginStrongholdId = originStrongholdId,
            TransportTargetUnitId = targetUnitId,
            TransportTargetStrongholdId = targetStrongholdId,
            CargoPopulation = cargoPopulation,
            PorterCount = porterCount,
            EscortSoldierCount = escortCount,
            ActionTarget = new UnitActionTarget
            {
                ForceId = forceId,
                StrongholdId = targetStrongholdId,
                UnitId = targetUnitId,
                RoutePoints = routePoints
            },
            SubUnitIds = []
        };

        gameData.Units[unitId] = unit;
        MapLocationActions.RegisterUnit(world, unit);
        return unit;
    }

    private static UnitKind ResolveKind(TransportPurpose purpose)
        => purpose switch
        {
            TransportPurpose.Trade => UnitKind.Merchant,
            TransportPurpose.Migrant => UnitKind.Migrant,
            _ => UnitKind.Convoy
        };

    private static int NextUnitId(GameData gameData)
        => gameData.Units.Keys.Where(id => id > 0).DefaultIfEmpty(100).Max() + 1;
}
