using SengokuScroll.Common.Types;
using SengokuScroll.Domain;
using SengokuScroll.Domain.Actions;
using SengokuScroll.Domain.Entities;
using SengokuScroll.Domain.Entities.Types;
using SengokuScroll.Strategy.Constants;
using static SengokuScroll.Domain.Entities.Unit;

namespace SengokuScroll.Strategy.Helpers;

/// <summary>以 Merchant Unit 替代 SupplyConvoy 的贸易队（迁移骨架）。</summary>
public static class TradeConvoyUnitFactory
{
    public static Unit? TryCreateMerchantTradeUnit(
        GameWorld world,
        Stronghold origin,
        Stronghold destination,
        int cargoFoodGo)
    {
        if (cargoFoodGo <= 0)
            return null;

        if (origin.ForceActor.Food < cargoFoodGo)
            return null;

        origin.ForceActor.Food -= cargoFoodGo;

        var gameData = world.GameData;
        var unitId = gameData.Units.Keys.Where(id => id > 0).DefaultIfEmpty(100).Max() + 1;
        var unit = new Unit
        {
            Id = unitId,
            Name = $"{origin.Name}→{destination.Name}贸易队",
            ForceId = origin.ForceId,
            Location = origin.Location,
            Soldier = Math.Min(200, Math.Max(50, cargoFoodGo / 500)),
            Food = cargoFoodGo,
            Money = Math.Min(origin.ForceActor.Money / 20, cargoFoodGo / 2),
            Morale = 70,
            Training = 50,
            Movement = 6,
            Ap = 6,
            IsMilitary = true,
            Kind = UnitKind.Merchant,
            InStronghold = false,
            HomeStrongholdId = origin.Id,
            LocationStrongholdId = 0,
            Directive = UnitDirective.Move,
            TradePolicy = UnitTradePolicy.None,
            ActionTarget = new UnitActionTarget
            {
                ForceId = origin.ForceId,
                StrongholdId = destination.Id,
                RoutePoints = new Queue<Point2>()
            },
            SubUnitIds = []
        };

        gameData.Units[unitId] = unit;
        MapLocationActions.RegisterUnit(world, unit);
        return unit;
    }
}
